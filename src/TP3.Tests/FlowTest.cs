using System.Diagnostics;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Interfaces;
using TP3.Messages;

public class FlowTest
{
    

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

        TP3Message lastMessageSent = null!;
        A.CallTo(() => fakeNetwrokTransport.Send(pipe, A<TP3Message>.Ignored))
            .Invokes((a) => 
            {
                lastMessageSent = a.GetArgument<TP3Message>(1) ?? null!;
                Trace.TraceInformation("Fake network transport send called with {0}", lastMessageSent);
            })   
            .Returns(Task.CompletedTask);
        // A.CallTo(() => fakeNetwrokTransport.Send(pipe, A<TP3AttachResponse>.Ignored))
        //     .Returns(Task.CompletedTask);

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
        #warning TODO: what if it's taken?

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
        
        // 3. Client opens the object
        //    Topen(fid, mode)
        //  -> Ropen(qid, iounit)
        await sut.router.Route(pipe, new TP3OpenRequest(tag: tag));
        var lastOpenResponse = lastMessageSent as TP3OpenResponse;


        // 2. Client lists folder (trunk)
        await sut.router.Route(pipe, new TP3ReadRequest(tag: tag));

        // 2. Client navigates to a path
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

    private static INode MockFileSystem()
    {
        var nodes = A.Fake<INode>();
        A.CallTo(() => nodes.Name).Returns("root");
        A.CallTo(() => nodes.NodeType).Returns(NodeType.Directory);
        A.CallTo(() => nodes.Children).Returns(new List<INode>
        {
            A.Fake<INode>(),
            A.Fake<INode>()
        });
        return nodes;
    }
}