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
        var nodes = A.Fake<INode>();
        A.CallTo(() => nodes.Name).Returns("root");
        A.CallTo(() => nodes.NodeType).Returns(NodeType.Directory);
        A.CallTo(() => nodes.Children).Returns(new List<INode>
        {
            A.Fake<INode>(),
            A.Fake<INode>()
        });
        
        var fakeservice = A.Fake<IService>();
        A.CallTo(() => fakeservice.State).Returns(nodes);


        var fakeNetwrokTransport = A.Fake<INetworkTransport>();
        A.CallTo(() => fakeNetwrokTransport.Send(A<TP3AttachRequest>.Ignored))
            .Returns(Task.CompletedTask);
        
        var transport = new TP3Transport(A.Fake<ILogger>(), fakeNetwrokTransport);

        var sut = new AgentHost(A.Fake<ILogger>(), 
            new List<IService> { fakeservice }.ToArray(), 
            new List<ITP3Transport> { transport }.ToArray());
        
        await sut.Init();
        
        #warning TODO: auth
        // auth
        // Tauth(afid, uname, aname)
        //     → Rauth(qid_auth)

        // Tattach(fid, afid, uname, aname)
        //     → Rattach(qid_root)

        // 1. Client attaches to the server
        //    Tattach(fid=root)
        //    -> Rattach(qid_root)
        
        // when message is incoming from network trasnport, router is asked to handle it.
        // we can use that to prtend we are some user and send a message to the agent host, and see if it is routed correctly.
        await sut.router.Route(transport, new TP3AttachRequest(tag: "root"));

        // let's check what is incoming to the agent host, and see if it is routed correctly.
        A.CallTo(() => fakeNetwrokTransport.Send(A<TP3AttachResponse>.That.Matches(m => m.Tag == "root")))
            .MustHaveHappenedOnceExactly();

        // 2. Client navigates to a path
        //    Twalk(fid=root, newfid=fileFid, ["usr", "bin"])
        //    -> Rwalk([qid_usr, qid_bin])

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


}