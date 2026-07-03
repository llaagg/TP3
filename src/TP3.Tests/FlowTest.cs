using System.Diagnostics;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

public class FlowTest
{
    public static TP3Message lastMessageSent = null!;


    [Fact]
    public async Task AgentHost_StartsAndStopsSuccessfully()
    {
        // build some fake tree strucutre like in files sytsme
        INode nodes = MockFileSystem();

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

        string tag = "root-tag";

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
        //    Tattach(fid=root)
        //    -> Rattach(qid_root)
        // when message is incoming from network trasnport, router is asked to handle it.
        // we can use that to prtend we are some user and send a message to the agent host, and see if it is routed correctly.
        await sut.router.Route(pipe, new TP3AttachRequest(tag: tag));
        var lastAttachResponse = lastMessageSent as TP3AttachResponse;
        Assert.NotNull(lastAttachResponse);
        Assert.Equal(tag, lastAttachResponse!.Tag);
        Assert.NotNull(lastAttachResponse.Info);
        Assert.NotNull(lastAttachResponse.Info.Id);
#warning TODO: what if tag it's taken?

        // 2. lets walk to root
        await sut.router.Route(pipe, new TP3WalkRequest()
        {
            Tag = tag,
            Path = new List<string> { }
        });
        var lastWalkResponse = lastMessageSent as TP3WalkResponse;
        Assert.NotNull(lastWalkResponse);
        Assert.Equal(tag, lastWalkResponse!.Tag);
        // after the walk there should be a pointer setup for this user
        var pointer = transport.NetwokSessions.GetPointer(pipe, tag);
        Assert.NotNull(pointer);
        Assert.Null(pointer!.Data);

        // 3. Client opens the object
        //    Topen(fid, mode)
        //  -> Ropen(qid, iounit)
        await sut.router.Route(pipe, new TP3OpenRequest(tag: tag));
        var lastOpenResponse = lastMessageSent as TP3OpenResponse;
        // there will be stream assigned to the pointer
        Assert.NotNull(pointer!.Data);
        var iounit = lastOpenResponse!.Iounit;
        var nodeType = lastOpenResponse!.Info.NodeType;
        Assert.Equal(NodeType.Directory, nodeType);

        // 4. Client lists folder (trunk)
        
        //var data = TP3StatPayload.(() => GetData(sut, pipe, tag));
        // await sut.router.Route(pipe, new TP3ReadRequest(tag: tag, offset: offset, maxBytes: maxbytes));
        // var lastReadResponse = lastMessageSent as TP3ReadResponse;
        // Assert.NotNull(lastReadResponse);
        // Assert.Equal(tag, lastReadResponse!.Tag);
        // // we know it is a directory, so the data should be a JSON array of Stat objects
        // Assert.NotNull(lastReadResponse.Data);
        // var data = lastReadResponse.Data;
        // var count = data.Length;
        // offset += (ulong)count;
        // var stats = 
        //     TP3StatPayload.DataAsFolders(new []{lastReadResponse}, maxbytes == count).ToList();
        // if(count == 0 || count < maxbytes)
        // {
        //     break;
        // }

        // 5. Client navigates to a path
        //    Twalk(fid=root, newfid=fileFid, ["usr", "bin"])
        //    -> Rwalk([qid_usr, qid_bin])
        await sut.router.Route(pipe, new TP3WalkRequest()
        {
            Tag = tag,
            Path = new List<string> { }
        });

        // matches tag nad has quid
        Assert.Same(tag, (lastMessageSent as TP3WalkResponse)!.Tag);
        Assert.NotEmpty((lastMessageSent as TP3WalkResponse)!.Infos);

        var quids = (lastMessageSent as TP3WalkResponse)!.Infos;

        // 3. Client opens the object
        //    Topen(fileFid, OREAD)
        //    -> Ropen(qid, iounit)

        // 4. If it is a DIRECTORY
        //    loop:
        //        Tread(fileFid, offset, count)
        //        -> Rread([Stat][Stat][Stat]...)
        //        offset += bytesReturned
        //    until Rread returns 0 bytes (EOF)

        // 5. If it is a FILE
        //    loop:
        //        Tread(fileFid, offset, count)
        //        -> Rread(file bytes)
        //        offset += bytesReturned
        //    until Rread returns 0 bytes (EOF)

        // 6. Release the handle
        //    Tclunk(fileFid)
        //    -> Rclunk

        // | Operation      | Input          | Output         |
        // | -------------- | -------------- | -------------- |
        // | Attach         | `fid`          | `Qid`          |
        // | Walk           | `fid + path`   | `Qid[]`        |
        // | Open           | `fid`          | `Qid`          |
        // | Read directory | `fid + offset` | `Stat[]`       |
        // | Read file      | `fid + offset` | `bytes`        |
        // | Clunk          | `fid`          | acknowledgment |


    }

    private IEnumerable<TP3ReadResponse> GetData(AgentHost sut, INetworkPipe pipe, string tag)
    {
        var offset = 0UL;
        var maxbytes = 10000U;
        while (true)
        {
            var readRequest = new TP3ReadRequest(tag: tag, offset: offset, maxBytes: maxbytes);
            sut.router.Route(pipe, readRequest).Wait();
            var lastReadResponse = lastMessageSent as TP3ReadResponse;
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

    private static INode MockFileSystem()
    {
        var root = SetupNode("root", NodeType.Directory, new List<INode>()
        {
            SetupNode("FILESYSTEM", NodeType.Directory, new List<INode>()
            {
                SetupNode("bin", NodeType.Directory, new List<INode>()
                {
                    SetupNode("ls", NodeType.File),
                    SetupNode("cat", NodeType.File),
                }),
                SetupNode("usr", NodeType.Directory, new List<INode>()
                {
                    SetupNode("local", NodeType.Directory, new List<INode>()
                    {
                        SetupNode("bin", NodeType.Directory, new List<INode>()
                        {
                            SetupNode("myapp", NodeType.File),
                        }),
                    }),
                }),
            }),
            SetupNode("README.md", NodeType.File),
        });
        return root;
    }

    private static INode SetupNode(string name, NodeType nodeType = NodeType.Directory, List<INode>? children = null)
    {
        var node = A.Fake<INode>();
        A.CallTo(() => node.Name).Returns(name);
        A.CallTo(() => node.NodeType).Returns(nodeType);
        if (children != null)
        {
            A.CallTo(() => node.Children).Returns(children);
        }
        return node;
    }
}