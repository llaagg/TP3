using System.Diagnostics;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;


namespace TP3.Tests.Intergration
{
    public class FlowTest
    {
        public static TP3Message lastMessageSent = null!;

        private static TP3Message AttachMessage(string tag)
        {
            return new TP3Message
            {
                Tag = tag,
                AttachRequest = new TP3AttachRequest(),
            };
        }

        private static TP3Message OpenMessage(string tag)
        {
            return new TP3Message
            {
                Tag = tag,
                OpenRequest = new TP3OpenRequest(),
            };
        }

        private static TP3Message ClunkMessage(string tag)
        {
            return new TP3Message
            {
                Tag = tag,
                ClunkRequest = new TP3ClunkRequest(),
            };
        }

        private static TP3Message WalkMessage(string tag, string newTag, params string[] segments)
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

        private static TP3Message ReadMessage(string tag, ulong offset, uint maxBytes)
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
        public async Task AgentHost_StartsAndStopsSuccessfully()
        {
            var fakeservice = A.Fake<IService>();
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

            A.CallTo(() => fakeservice.Id).Returns("svc1");
            A.CallTo(() => fakeservice.Name).Returns("service1");
            A.CallTo(() => fakeservice.NodeType).Returns(NodeType.Directory);
            A.CallTo(() => fakeservice.Children).Returns(new List<INode> { stateNode });
            A.CallTo(() => fakeservice.Get()).Returns(Task.FromResult<ITP3DataStream?>(null));
            A.CallTo(() => fakeservice.Init(A<IAgent>.Ignored)).Returns(Task.CompletedTask);

            var fakeNetwrokTransport = A.Fake<INetworkTransport>();

            var transport = new TP3Transport(A.Fake<ILogger>(), fakeNetwrokTransport);

            var pipe = A.Fake<INetworkPipe>();
            A.CallTo(() => pipe.AgentID).Returns("agent1");
            A.CallTo(() => pipe.TP3Transport).Returns(transport);


            A.CallTo(() => fakeNetwrokTransport.Send(pipe, A<TP3Message>.Ignored))
                .Invokes((a) =>
                {
                    lastMessageSent = a.GetArgument<TP3Message>(1) ?? null!;
                    Trace.TraceInformation("Fake network transport send called with {0}", lastMessageSent);
                })
                .Returns(Task.CompletedTask);


            var sut = new TP3.Agent.Logic.Agent.Agent(A.Fake<ILogger>(),
                new List<IService> { fakeservice }.ToArray());

            await sut.Init();

            transport.NewUserNetworkConnection(fakeNetwrokTransport, pipe);

            await sut.Handle(pipe, AttachMessage("tag1"));                                      /// Tattach (tag)
            Assert.Equal(TP3Message.PayloadOneofCase.AttachResponse, lastMessageSent.PayloadCase);                                           ///                   Rattach
            var lastAttachResponse = lastMessageSent.AttachResponse;                               ///                   Rattach
            Assert.NotNull(lastAttachResponse);
            Assert.Equal("tag1", lastMessageSent.Tag);                                                ///                   tag
            Assert.NotNull(lastAttachResponse.Info);
            Assert.NotNull(lastAttachResponse.Info.Id);                                           ///                   quid
            // var pointer = sut.NetworkSessions.GetPointer(pipe, "tag1");
            // Assert.NotNull(pointer);
            // Assert.NotNull(pointer!.Node);
            // Assert.Null(pointer!.Data);

            await sut.Handle(pipe, WalkMessage("tag1", "tag2"));
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, lastMessageSent.PayloadCase);
            
            await sut.Handle(pipe, OpenMessage("tag2"));
            var lastOpenResponse = lastMessageSent.OpenResponse;
            var nodeType = lastOpenResponse!.Info.NodeType;
            Assert.Equal(NodeType.Directory, nodeType);

            List<TP3StatPayload> stats = TReadOnADirectory(pipe, sut, "tag2");
            Assert.NotEmpty(stats);
            Assert.Equal(NodeType.Directory, stats[0].Info.NodeType);

            await sut.Handle(pipe, ClunkMessage("tag2"));
            var lastClunkResponse = lastMessageSent.ClunkResponse;
            Assert.NotNull(lastClunkResponse);

            List<string> path = new List<string> { stats[0].Name };

            await sut.Handle(pipe, WalkMessage("tag1", "tag2", path.ToArray()));
            var lastWalkResponse2 = lastMessageSent.WalkResponse;
            Assert.NotNull(lastWalkResponse2);

            await sut.Handle(pipe, OpenMessage("tag2"));
            Assert.Equal(NodeType.Directory, nodeType);

            List<TP3StatPayload> stats2 = TReadOnADirectory(pipe, sut, "tag2");
            Assert.NotEmpty(stats2);
            Assert.Equal(NodeType.Directory, stats2[0].Info.NodeType);
            var firstChildName = stats2[0].Name;     
            Assert.Equal("state", firstChildName, ignoreCase: true); // we have state as in all services
  
            await sut.Handle(pipe, ClunkMessage("tag2"));

            path.Add(firstChildName);

            await sut.Handle(pipe, WalkMessage("tag1", "tag2", path.ToArray()));
            Assert.NotNull(lastMessageSent.WalkResponse);

            await sut.Handle(pipe, OpenMessage("tag2"));
            Assert.Equal(NodeType.Directory, nodeType);

            await sut.Handle(pipe, ClunkMessage("tag2"));

            await sut.Handle(pipe, WalkMessage("tag1", "tag2", path[0], path[1], "README.md"));
            var lastWalkResponse4 = lastMessageSent.WalkResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, lastMessageSent.PayloadCase);
            Assert.NotNull(lastWalkResponse4);
            Assert.Equal(3, lastWalkResponse4!.Infos!.Count);
            Assert.Equal(NodeType.File, lastWalkResponse4.Infos![2].NodeType);

            await sut.Handle(pipe, OpenMessage("tag2"));
            var lastOpenResponse4 = lastMessageSent.OpenResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.OpenResponse, lastMessageSent.PayloadCase);
            Assert.Equal(NodeType.File, lastOpenResponse4!.Info.NodeType);
            
            await sut.Handle(pipe, ReadMessage("tag2", 0, 1000));
            var lastReadResponse = lastMessageSent.ReadResponse;
            Assert.NotNull(lastReadResponse);
            var data = lastReadResponse!.Data;
            Assert.NotNull(data);
            Assert.NotEmpty(data);
        }

        private List<TP3StatPayload> TReadOnADirectory(INetworkPipe pipe, Agent.Logic.Agent.Agent sut, string tag)
        {
            IEnumerable<TP3ReadResponse> data = GetData(sut, pipe, tag);
            TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(data);

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            return TP3StatPayloadExtensions.Deserilize(ms).ToList();
        }

        private IEnumerable<TP3ReadResponse> GetData(Agent.Logic.Agent.Agent sut, INetworkPipe pipe, string tag)
        {
            var offset = 0UL;
            var maxbytes = 10000U;
            while (true)
            {
                var readRequest = ReadMessage(tag, offset, maxbytes);
                sut.Handle(pipe, readRequest).Wait();
                var lastReadResponse = lastMessageSent.ReadResponse;
                var count = lastReadResponse?.Data?.Length ?? 0;
                yield return lastReadResponse;


                if (count == 0 || count < maxbytes)
                {
                    break;
                }
                offset += (ulong)count;
            }
        }

    }

}
