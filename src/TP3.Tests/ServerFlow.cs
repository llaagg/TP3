public class ServerFlow
{
    [Fact]
    public async Task Server_CanListAnotherServer()
    {
        // connect from serv a to serv b
        // list services on serv b
    }

    [Fact]
    public async Task Server_EstablishesConnection()
    {
        //allow 2 or more servers to connect to each other
        //and maintatin a session

        // multiple attach commands to the same server

        // if server establisches a connection to another server, it should be able to send messages to that server and receive messages from that server
        // it will use service for that purpose
        // and then this connection will be aproxy to a tree on the other server
    }
}