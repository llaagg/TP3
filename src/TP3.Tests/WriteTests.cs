using System.Text;
using FakeItEasy;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Agent;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;


namespace TP3.Tests.Protocol
{
    public class WriteTests
    {
        [Fact]
        public async Task TP3WriteRequestDataStream_ReadsDataCorrectly()
        {
            // Arrange
            var memeoryStream = new MemoryStream();
            var stream = new TP3Stream(memeoryStream);

            var listOfResponses = new List<TP3WriteRequest>
            {
                new TP3WriteRequest { Data = ByteString.CopyFrom(new byte[] { (byte)'1', (byte)'2', (byte)'3' }), Offset = 0 },
                new TP3WriteRequest { Data = ByteString.CopyFrom(new byte[] { (byte)'4', (byte)'5', (byte)'6' }), Offset = 3 },
                new TP3WriteRequest { Data = ByteString.CopyFrom(new byte[] { (byte)'7', (byte)'8', (byte)'9' }), Offset = 6 }
            };

            // Act            
            TP3WriteRequestDataStreamWriter writer = new TP3WriteRequestDataStreamWriter(stream);
            var total = 0UL;
            foreach (var response in listOfResponses)
            {
                var written = await writer.Write(response);
                total += written;
            }

            // Assert
            memeoryStream.Seek(0, SeekOrigin.Begin);
            Assert.Equal("123456789", Encoding.UTF8.GetString(memeoryStream.ToArray()));
            Assert.Equal(9UL, total);
        }

        [Fact]
        public async Task IsAbleToReadResponseFromNode()
        {
            var node = new StreamNode(() => "This will be a text string");
            var dataStream = await node.Get();

            var readData = await dataStream!.Read(0, 1000);
            var readString = Encoding.UTF8.GetString(readData);
            Assert.Equal("This will be a text string", readString);
        }

         [Fact]
        public async Task HandlesWriteRequests()
        {
            TP3Message lastMessageSent = null!;
            var fakeRouter = A.Fake<IRouter>();
            A.CallTo(() => fakeRouter.Route)
                .Invokes((INetworkPipe pipe, TP3Message message) =>
                {
                    lastMessageSent = message;
                });

            var messageHandler = new MessageHandler(A.Fake<IAgent>(), fakeRouter, A.Fake<ILogger>());

            var data = new MemoryStream();
            // put random 1mb of data 
            await FillUp(data);

            var writeRequestsProvider = new TP3WriteRequestsProvider(data, 1024 * 16);
            foreach (var request in writeRequestsProvider.GetWriteRequests())
            {
                Assert.NotNull(request.Data);
                Assert.True(request.Data.Length > 0);
                // create messages to send
                var message = new TP3Message
                {
                    Tag = "test-tag",
                    WriteRequest = request
                };
                await messageHandler.Handle(A.Fake<INetworkPipe>(), message);

                // assert that response is not error and is write response
                Assert.NotNull(lastMessageSent);
                Assert.Equal("test-tag", lastMessageSent.Tag);
                Assert.Equal(TP3Message.PayloadOneofCase.WriteResponse, lastMessageSent.PayloadCase);
            }

        }


        private static async Task FillUp(MemoryStream data, int sizeInBytes = 1024 * 1024)
        {
            // fill with some ascii characters
            var random = new Random();
            var buffer = new byte[sizeInBytes];
            for (int i = 0; i < sizeInBytes; i++)
            {
                buffer[i] = (byte)('a' + random.Next(0, 26));
            }
            await data.WriteAsync(buffer, 0, buffer.Length);
            data.Seek(0, SeekOrigin.Begin);
        }
    }
}
