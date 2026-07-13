using System.Diagnostics;
using FakeItEasy;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;


namespace TP3.Tests.Integration
{
    public class FlowTest
    {
        private static TP3Message LastMessageSent = null!;

        public static TP3Message AttachMessage(string tag)
        {
            return new TP3Message
            {
                Tag = tag,
                AttachRequest = new TP3AttachRequest(),
            };
        }

        public static TP3Message OpenMessage(string tag)
        {
            return new TP3Message
            {
                Tag = tag,
                OpenRequest = new TP3OpenRequest(),
            };
        }

        public static TP3Message ClunkMessage(string tag)
        {
            return new TP3Message
            {
                Tag = tag,
                ClunkRequest = new TP3ClunkRequest(),
            };
        }

        public static TP3Message WalkMessage(string tag, string newTag, params string[] segments)
        {
            var request = new TP3WalkRequest();
            request.NewTag = newTag;
            request.Path.Add(segments);

            return new TP3Message
            {
                Tag = tag,
                WalkRequest = request,
            };
        }

        public static TP3Message ReadMessage(string tag, ulong offset, uint maxBytes)
        {
            return new TP3Message
            {
                Tag = tag,
                ReadRequest = new TP3ReadRequest
                {
                    Offset = offset,
                    MaxBytes = maxBytes,
                },
            };
        }

        

        [Fact]
        public async Task GoldenPath_CompleteEndToEnd()
        {
            (INetworkPipe fakeNetworkPipe, Agent.Logic.Agent.Agent sut) = await InitilizeTP3(new List<INode> { StateNode() }, async (message) =>
            {
                Trace.TraceInformation("Message sent: {0}", message);
              
                LastMessageSent = message;
            });

            // * ATTACH
            await sut.Handle(fakeNetworkPipe, AttachMessage("tag1"));
            Assert.Equal(TP3Message.PayloadOneofCase.AttachResponse, LastMessageSent.PayloadCase);
            var lastAttachResponse = LastMessageSent.AttachResponse;
            Assert.NotNull(lastAttachResponse);
            Assert.Equal("tag1", LastMessageSent.Tag);
            Assert.NotNull(lastAttachResponse.Info);
            Assert.NotNull(lastAttachResponse.Info.Id);

            // * WALK
            await sut.Handle(fakeNetworkPipe, WalkMessage("tag1", "tag2"));
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, LastMessageSent.PayloadCase);

            // * OPEN
            await sut.Handle(fakeNetworkPipe, OpenMessage("tag2"));
            var lastOpenResponse = LastMessageSent.OpenResponse;
            var nodeType = lastOpenResponse!.Info.NodeType;
            Assert.Equal(NodeType.Directory, nodeType);

            // * READ on directory (multiple)
            List<TP3StatPayload> stats = TReadOnADirectory(fakeNetworkPipe, sut, "tag2");
            Assert.NotEmpty(stats);
            Assert.Equal(NodeType.Directory, stats[0].Info.NodeType);

            // * CLUNK
            await sut.Handle(fakeNetworkPipe, ClunkMessage("tag2"));
            var lastClunkResponse = LastMessageSent.ClunkResponse;
            Assert.NotNull(lastClunkResponse);

            List<string> path = new List<string> { stats[0].Name };

            // * WALK to child
            await sut.Handle(fakeNetworkPipe, WalkMessage("tag1", "tag2", path.ToArray()));
            var lastWalkResponse2 = LastMessageSent.WalkResponse;
            Assert.NotNull(lastWalkResponse2);

            // * OPEN child
            await sut.Handle(fakeNetworkPipe, OpenMessage("tag2"));
            Assert.Equal(NodeType.Directory, nodeType);

            // * READ on child (multiple)
            List<TP3StatPayload> stats2 = TReadOnADirectory(fakeNetworkPipe, sut, "tag2");
            Assert.NotEmpty(stats2);
            Assert.Equal(NodeType.Directory, stats2[0].Info.NodeType);
            var firstChildName = stats2[0].Name;
            Assert.Equal("service1", firstChildName, ignoreCase: true); // we have state as in all services

            // * CLUNK child
            await sut.Handle(fakeNetworkPipe, ClunkMessage("tag2"));

            path.Add(firstChildName);

            // * WALK to grandchild
            await sut.Handle(fakeNetworkPipe, WalkMessage("tag1", "tag2", path.ToArray()));
            Assert.NotNull(LastMessageSent.WalkResponse);

            // * OPEN grandchild
            await sut.Handle(fakeNetworkPipe, OpenMessage("tag2"));
            Assert.Equal(NodeType.Directory, nodeType);

            // * READ on grandchild (multiple)
            await sut.Handle(fakeNetworkPipe, ClunkMessage("tag2"));

            // * WALK to file
            await sut.Handle(fakeNetworkPipe, WalkMessage("tag1", "tag2", path[0], path[1], "state","README.md"));
            var lastWalkResponse4 = LastMessageSent.WalkResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, LastMessageSent.PayloadCase);
            Assert.NotNull(lastWalkResponse4);
            Assert.Equal(4, lastWalkResponse4!.Infos!.Count);
            Assert.Equal(NodeType.File, lastWalkResponse4.Infos![3].NodeType);

            // * OPEN file
            await sut.Handle(fakeNetworkPipe, OpenMessage("tag2"));
            var lastOpenResponse4 = LastMessageSent.OpenResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.OpenResponse, LastMessageSent.PayloadCase);
            Assert.Equal(NodeType.File, lastOpenResponse4!.Info.NodeType);

            // * READ on file (multiple)
            await sut.Handle(fakeNetworkPipe, ReadMessage("tag2", 0, 1000));
            var lastReadResponse = LastMessageSent.ReadResponse;
            Assert.NotNull(lastReadResponse);
            var data = lastReadResponse!.Data;
            Assert.NotNull(data);
            Assert.NotEmpty(data);
        }

        public static async Task<(INetworkPipe fakeNetworkPipe, Agent.Logic.Agent.Agent sut)> InitilizeTP3(List<INode> nodes, Func<TP3Message, Task>? onMessageSent = null)
        {
            var fakeservice = A.Fake<IService>();
            A.CallTo(() => fakeservice.Id).Returns("svc1");
            A.CallTo(() => fakeservice.Name).Returns("service1");
            A.CallTo(() => fakeservice.NodeType).Returns(NodeType.Directory);
            A.CallTo(() => fakeservice.Children).Returns(nodes);
            A.CallTo(() => fakeservice.Get()).Returns(Task.FromResult<ITP3DataStream?>(null));
            A.CallTo(() => fakeservice.Init(A<IAgent>.Ignored)).Returns(Task.CompletedTask);


            var fakeNetworkTransport = A.Fake<INetworkTransport>();
            ITP3Transport transport = null!;

            var fakeNetworkPipe = A.Fake<INetworkPipe>();
            A.CallTo(() => fakeNetworkPipe.AgentID).Returns("agent1");
            A.CallTo(() => fakeNetworkPipe.TP3Transport).ReturnsLazily<ITP3Transport>(() => transport);


            A.CallTo(() => fakeNetworkTransport.Send(fakeNetworkPipe, A<TP3Message>.Ignored))
                .Invokes(async (INetworkPipe pipe, TP3Message message) =>
                {
                    if (onMessageSent != null)
                    {
                        await onMessageSent(message);
                    }
                })
                .Returns(Task.CompletedTask);


            var sut = new TP3.Agent.Logic.Agent.Agent(A.Fake<ILogger>(),
                new List<IService> { fakeservice }.ToArray());


            await sut.Init();
            transport = await sut.AddTransport(fakeNetworkTransport);
            transport.NewUserNetworkConnection(fakeNetworkPipe);
            return (fakeNetworkPipe, sut);
        }

        private static INode StateNode()
        {
            var fileData = System.Text.Encoding.UTF8.GetBytes("sample-readme-content");
            var fileStream = A.Fake<ITP3DataStream>();
            A.CallTo(() => fileStream.Read(A<ulong>.Ignored, A<ulong>.Ignored))
                .ReturnsLazily((ulong offset, ulong maxBytes) =>
                {
                    if (offset >= (ulong)fileData.Length)
                    {
                        return Task.FromResult(Array.Empty<byte>());
                    }

                    var take = (int)Math.Min((ulong)fileData.Length - offset, maxBytes);
                    return Task.FromResult(fileData.Skip((int)offset).Take(take).ToArray());
                });

            var readmeNode = TP3Helpers.SetupNode("README.md", NodeType.File);
            A.CallTo(() => readmeNode.Get()).Returns(fileStream);

            var stateNode = TP3Helpers.SetupNode("state", NodeType.Directory, new List<INode> { readmeNode });
            return stateNode;
        }

        public static List<TP3StatPayload> TReadOnADirectory(INetworkPipe pipe, Agent.Logic.Agent.Agent sut, string tag)
        {
            IEnumerable<TP3ReadResponse> data = GetData(sut, pipe, tag);
            TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(data);

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            return TP3StatPayloadExtensions.Deserilize(ms).ToList();
        }

        private static IEnumerable<TP3ReadResponse> GetData(Agent.Logic.Agent.Agent sut, INetworkPipe pipe, string tag)
        {
            var offset = 0UL;
            var maxbytes = 10000U;
            while (true)
            {
                var readRequest = ReadMessage(tag, offset, maxbytes);
                sut.Handle(pipe, readRequest).Wait();
                var lastReadResponse = LastMessageSent.ReadResponse;
                var count = lastReadResponse?.Data?.Length ?? 0;
                yield return lastReadResponse;

                if (count == 0 || count < maxbytes)
                {
                    break;
                }
                offset += (ulong)count;
            }
        }

        internal static TP3Message WriteMessage(string tag, string data)
        {
            return new TP3Message
            {
                Tag = tag,
                WriteRequest = new TP3WriteRequest
                {
                    Data = ByteString.CopyFrom(System.Text.Encoding.UTF8.GetBytes(data))                    
                }
            };
        }
    }

}
