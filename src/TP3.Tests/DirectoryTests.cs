using System.Diagnostics;
using System.Text;
using FakeItEasy;
using TP3.Agent.Logic.Agent;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Tests.Protocol
{

    public class DirectoryTests
    {

        [Fact]
        public async Task Nodes_IntoResponse2()
        {
            // ARRANGE
            var rootNode = TP3Helpers.SetupNode("root", NodeType.Directory, new List<INode>
            {
                TP3Helpers.SetupNode("file1.txt", NodeType.File),
                TP3Helpers.SetupNode("file2.txt", NodeType.File),
                TP3Helpers.SetupNode("file3.txt", NodeType.File)
            });
            var directoryStreamData = new DirectoryStreamData(rootNode);
            await directoryStreamData.Open(); // creates a stream that can be serilized and sent

            TP3Message lastMessageSent = null!;
            var fakeRouter = A.Fake<IRouter>();
            A.CallTo(() => fakeRouter.Respond(A<IAgent>.Ignored, A<TP3Message>.Ignored, A<INetworkPipe>.Ignored))
                .Invokes((IAgent agent, TP3Message message, INetworkPipe targetTransport) =>
                {
                    lastMessageSent = message;
                    Trace.TraceInformation("Fake router respond called with {0}", lastMessageSent);
                })
                .Returns(Task.CompletedTask);

            var fakeTp3Trasnport = A.Fake<ITP3Transport>();
            A.CallTo(() => fakeTp3Trasnport.GetData(A<INetworkPipe>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult<ITP3DataStream>(directoryStreamData));

            var fakeNetworkPie = A.Fake<INetworkPipe>();
            A.CallTo(() => fakeNetworkPie.TP3Transport).Returns(fakeTp3Trasnport);


            var agent = new TP3.Agent.Logic.Agent.Agent(fakeRouter, A.Fake<Microsoft.Extensions.Logging.ILogger>());

            string tag = "root-tag";

            // ACT
            await agent.ReadDataAndSend(new TP3ReadRequest(tag), fakeNetworkPie);

            // ASSERT
            Assert.NotNull(lastMessageSent);
            Assert.IsType<TP3ReadResponse>(lastMessageSent);
            var readResponse = (TP3ReadResponse)lastMessageSent;
            Assert.NotEmpty(readResponse.Data!);

            var dataStream = new TP3ReadResponseDataStream(new List<TP3ReadResponse> { readResponse });
            // diagnostics 
            // var sr = new StreamReader(dataStream);
            // var data = await sr.ReadToEndAsync();

            var statPayload = TP3StatPayloadExtensions.Deserilize(dataStream).ToList();
            Assert.Equal(rootNode.Children!.Count(), statPayload.Count());
        }


        [Fact]
        public async Task Nodes_IntoResponse()
        {
            var rootNode = TP3Helpers.SetupNode("root", NodeType.Directory, new List<INode>
        {
            TP3Helpers.SetupNode("file1.txt", NodeType.File),
            TP3Helpers.SetupNode("file2.txt", NodeType.File),
            TP3Helpers.SetupNode("file3.txt", NodeType.File)
        });
            var directoryStreamData = new DirectoryStreamData(rootNode);

            await directoryStreamData.Open();

            var actualBytes = await directoryStreamData.Read(0, 0);
            var actual = Encoding.UTF8.GetString(actualBytes);

            var expected = string.Concat(rootNode.Children!.Select(child =>
                Encoding.UTF8.GetString(new TP3StatPayload(child).Serialize().ToArray())));

            Assert.Equal(expected, actual);
        }
    }
}