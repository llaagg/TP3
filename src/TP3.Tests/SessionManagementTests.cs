using Castle.Core.Logging;
using FakeItEasy;
using TP3.Agent.Logic.Agent;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

public class SessionManagementTests
{

    [Fact]
    public async Task AttachCannotReuseTag()
    {
        var tag = Guid.NewGuid().ToString("N").Substring(0, 8);

        var sut = new NetworkSessions();
        var fakeNetworkPipe = A.Fake<INetworkPipe>();
        A.CallTo(() => fakeNetworkPipe.AgentID).Returns("agent1_like_tcp");

        
        sut.AddSession(fakeNetworkPipe);
        sut.AttachTagToPointer(tag, TP3Helpers.SetupNode("root", NodeType.Directory), fakeNetworkPipe);
        
        // act
        Assert.Throws<InvalidOperationException>(() => sut.AttachTagToPointer(tag, TP3Helpers.SetupNode("root", NodeType.Directory), fakeNetworkPipe));
    }

    [Fact]
    public void AttachCanReuseTagAfterClunk()
    {
        var tag = Guid.NewGuid().ToString("N").Substring(0, 8);
        var sut = new NetworkSessions();
        var fakeNetworkPipe = A.Fake<INetworkPipe>();
        A.CallTo(() => fakeNetworkPipe.AgentID).Returns("agent1_like_tcp");
        
        sut.AddSession(fakeNetworkPipe);
        sut.AttachTagToPointer(tag, TP3Helpers.SetupNode("root", NodeType.Directory), fakeNetworkPipe);
        sut.CloseSession(fakeNetworkPipe, tag);
        Assert.Empty(sut.Connections[fakeNetworkPipe.TP3Transport.TransportTag + ":" + fakeNetworkPipe.AgentID].Pointers);

        // act
        sut.AttachTagToPointer(tag, TP3Helpers.SetupNode("root", NodeType.Directory), fakeNetworkPipe);
        Assert.Single(sut.Connections[fakeNetworkPipe.TP3Transport.TransportTag + ":" + fakeNetworkPipe.AgentID].Pointers);
    }

}