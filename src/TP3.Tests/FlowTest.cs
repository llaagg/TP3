using System.Diagnostics;
using System.Text;
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
            // build some fake tree strucutre like in files sytsme
            INode nodes = TP3Helpers.MockFileSystem();

            var fakeservice = A.Fake<IService>();
            A.CallTo(() => fakeservice.State).Returns(nodes);

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


            var sut = new AgentHost(A.Fake<ILogger>(),
                new List<IService> { fakeservice }.ToArray(),
                new List<ITP3Transport> { transport }.ToArray());

            await sut.Init();


#warning TODO: auth

            // after opening TCP or IPC connection,
            // session is added
            transport.NewUserNetworkConnection(fakeNetwrokTransport, pipe);

            // 1. auth
            // Tauth(afid, uname, aname)
            //     → Rauth(qid_auth)

            // Tattach(fid, afid, uname, aname)
            //     → Rattach(qid_root)

            // 2. Client attaches to the server
            //   
            //    -> 
            // when message is incoming from network trasnport, router is asked to handle it.
            // we can use that to prtend we are some user and send a message to the agent host, and see if it is routed correctly.
            await sut.router.Route(pipe, AttachMessage("tag1"));                                      /// Tattach (tag)
            Assert.Equal(TP3Message.PayloadOneofCase.AttachResponse, lastMessageSent.PayloadCase);                                           ///                   Rattach
            var lastAttachResponse = lastMessageSent.AttachResponse;                               ///                   Rattach
            Assert.NotNull(lastAttachResponse);
            Assert.Equal("tag1", lastMessageSent.Tag);                                                ///                   tag
            Assert.NotNull(lastAttachResponse.Info);
            Assert.NotNull(lastAttachResponse.Info.Id);                                           ///                   quid
            var pointer = sut.NetworkSessions.GetPointer(pipe, "tag1");
            Assert.NotNull(pointer);
            Assert.NotNull(pointer!.Node);
            Assert.Null(pointer!.Data);

            // let' do walk with new fid, so we cn rerefenrce root fid later again
            await sut.router.Route(pipe, WalkMessage("tag1", "tag2"));
            var lastWalkResponse = lastMessageSent.WalkResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, lastMessageSent.PayloadCase);
            
            // 3. opens the object
            //    Topen(fid, mode)
            //  -> Ropen(qid, iounit)
            await sut.router.Route(pipe, OpenMessage("tag2"));
            var lastOpenResponse = lastMessageSent.OpenResponse;
            // there will be stream assigned to the pointer
            //Assert.NotNull(pointer!.Data);
            var iounit = lastOpenResponse!.Iounit;
            var nodeType = lastOpenResponse!.Info.NodeType;
            Assert.Equal(NodeType.Directory, nodeType);

            // 4.  lists folder (trunk)
            List<TP3StatPayload> stats = TReadOnADirectory(pipe, sut, "tag2");
            Assert.NotEmpty(stats);
            Assert.Equal(NodeType.Directory, stats[0].Info.NodeType);

            // 5. we close the file
            await sut.router.Route(pipe, ClunkMessage("tag2"));
            var lastClunkResponse = lastMessageSent.ClunkResponse;
            Assert.NotNull(lastClunkResponse);

            List<string> path = new List<string>();
            path.Add(stats[0].Name); 

            // 6. Client navigates to a path
            //    Twalk(fid=root, newfid=fileFid, ["usr", "bin"])
            //    -> Rwalk([qid_usr, qid_bin])
            await sut.router.Route(pipe, WalkMessage("tag1", "tag2", path.ToArray()));
            var lastWalkResponse2 = lastMessageSent.WalkResponse;
            Assert.NotNull(lastWalkResponse2);

            // 7 we did walk let's open read
            await sut.router.Route(pipe, OpenMessage("tag2"));
            var lastOpenResponse2 = lastMessageSent.OpenResponse;
            Assert.Equal(NodeType.Directory, nodeType);

            // 7 list current folder
            List<TP3StatPayload> stats2 = TReadOnADirectory(pipe, sut, "tag2");
            Assert.NotEmpty(stats2);
            Assert.Equal(NodeType.Directory, stats2[0].Info.NodeType);
            var firstChildName = stats2[0].Name;     
            Assert.Equal("state", firstChildName, ignoreCase: true); // we have state as in all services
  

            // 8. let's go close
            await sut.router.Route(pipe, ClunkMessage("tag2"));

            path.Add(firstChildName);
                        
            // 8 let's walk
            await sut.router.Route(pipe, WalkMessage("tag1", "tag2", path.ToArray()));
            var lastWalkResponse3 = lastMessageSent.WalkResponse;
            Assert.NotNull(lastWalkResponse3);

            // 9 let's open
            await sut.router.Route(pipe, OpenMessage("tag2"));
            var lastOpenResponse3 = lastMessageSent.OpenResponse;
            Assert.Equal(NodeType.Directory, nodeType);

            // 10 list current folder
            List<TP3StatPayload> stats3 = TReadOnADirectory(pipe, sut, "tag2"   );
            Assert.NotEmpty(stats3);
            Assert.Equal(NodeType.Directory, stats3[0].Info.NodeType);  

            // 11. let's go close
            await sut.router.Route(pipe, ClunkMessage("tag2"));

            // 12. let's walk to Reamde.md
            await sut.router.Route(pipe, WalkMessage("tag1", "tag2", path[0], path[1], "README.md"));
            var lastWalkResponse4 = lastMessageSent.WalkResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, lastMessageSent.PayloadCase);
            Assert.NotNull(lastWalkResponse4);
            Assert.Equal(3, lastWalkResponse4!.Infos!.Count);
            Assert.Equal(NodeType.File, lastWalkResponse4.Infos![2].NodeType);

            // 13. let open the file
            await sut.router.Route(pipe, OpenMessage("tag2"));
            var lastOpenResponse4 = lastMessageSent.OpenResponse;
            Assert.Equal(TP3Message.PayloadOneofCase.OpenResponse, lastMessageSent.PayloadCase);
            Assert.Equal(NodeType.File, lastOpenResponse4!.Info.NodeType);
            

            // 14. let's read the file
            await sut.router.Route(pipe, ReadMessage("tag2", 0, 1000));
            var lastReadResponse = lastMessageSent.ReadResponse;
            Assert.NotNull(lastReadResponse);
            var data = lastReadResponse!.Data;
            Assert.NotNull(data);
            Assert.NotEmpty(data);

            // | Operation      | Input          | Output         |
            // | -------------- | -------------- | -------------- |
            // | Attach         | `fid`          | `Qid`          |
            // | Walk           | `fid + path`   | `Qid[]`        |
            // | Open           | `fid`          | `Qid`          |
            // | Read directory | `fid + offset` | `Stat[]`       |
            // | Read file      | `fid + offset` | `bytes`        |
            // | Clunk          | `fid`          | acknowledgment |
        }

        private List<TP3StatPayload> TReadOnADirectory(INetworkPipe pipe, AgentHost sut, string tag)
        {
            IEnumerable<TP3ReadResponse> data = GetData(sut, pipe, tag);
            TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(data);

            // diagnostic: read all data and deserialize to TP3StatPayload
            StreamReader reader = new StreamReader(stream);
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;           

            // what do we hwve there...
            string json = new StreamReader(ms).ReadToEnd();
            ms.Position = 0;

            var stats = TP3StatPayloadExtensions.Deserilize(ms).ToList();
            return stats;
        }

        private IEnumerable<TP3ReadResponse> GetData(AgentHost sut, INetworkPipe pipe, string tag)
        {
            var offset = 0UL;
            var maxbytes = 10000U;
            while (true)
            {
                var readRequest = ReadMessage(tag, offset, maxbytes);
                sut.router.Route(pipe, readRequest).Wait();
                var lastReadResponse = lastMessageSent.ReadResponse;
                var count = lastReadResponse?.Data?.Length ?? 0;
                yield return lastReadResponse;


                if (count == 0 || count < maxbytes)
                {
                    break;
                }
                offset += (ulong)count;
            }
            yield break;
        }

    }

}
