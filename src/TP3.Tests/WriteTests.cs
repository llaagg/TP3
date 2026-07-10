using System.Text;
using Google.Protobuf;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Tests.Integration;


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
            var targetMemoryStream = new MemoryStream();
            var node = new StreamNode(targetMemoryStream, "TestNode", "wut");

            TP3Message lastMessageSent = null!;

            var (fakeNetworkPipe, sut) = await FlowTest.InitilizeTP3(
                new List<INode> { node }, 
                async (message) =>
                {
                    lastMessageSent = message;
                });

            // * ATTACH
            await sut.Handle(fakeNetworkPipe, FlowTest.AttachMessage("tag1"));
            Assert.Equal(TP3Message.PayloadOneofCase.AttachResponse, lastMessageSent.PayloadCase);
            var lastAttachResponse = lastMessageSent.AttachResponse;
            Assert.NotNull(lastAttachResponse);
            Assert.Equal("tag1", lastMessageSent.Tag);
            Assert.NotNull(lastAttachResponse.Info);
            Assert.NotNull(lastAttachResponse.Info.Id);

            // * WALK
            List<string> path = new List<string> { "service1", "TestNode" };
            var walkRequest = new TP3WalkRequest
            {
                NewTag = "tag2",
                Path = { path }
            };
            await sut.Handle(fakeNetworkPipe, new TP3Message { Tag = "tag1", WalkRequest = walkRequest });
            Assert.Equal(TP3Message.PayloadOneofCase.WalkResponse, lastMessageSent.PayloadCase);
            Assert.Equal(walkRequest.Path.Count, lastMessageSent.WalkResponse!.Infos.Count);

            // * OPEN
            await sut.Handle(fakeNetworkPipe, FlowTest.OpenMessage("tag2"));
            var lastOpenResponse = lastMessageSent.OpenResponse;
            var nodeType = lastOpenResponse!.Info.NodeType;
            Assert.Equal(NodeType.File, nodeType);

            // * WRITE
            string test = "Hello, World!";
            var writeMessage = new TP3Message
            {
                Tag = "tag2",
                WriteRequest = new TP3WriteRequest
                {
                    Offset = 0,
                    Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes(test))
                }
            };
            await sut.Handle(fakeNetworkPipe, writeMessage);
            Assert.Equal(TP3Message.PayloadOneofCase.WriteResponse, lastMessageSent.PayloadCase);
            var writeResponse = lastMessageSent.WriteResponse;
            Assert.Equal(test.Length, (int)writeResponse!.Count);
            Assert.NotNull(writeResponse);
            
        }

        [Fact]
        public async Task StreamNode_CanWrite()
        {
            var memoryStream = new MemoryStream();
            var StreamNode = new StreamNode(memoryStream);

            StreamNode streamNode = StreamNode;
            var dataStream = await streamNode.Get();
            dataStream!.Open();
            dataStream.Write(0, Encoding.UTF8.GetBytes("Hello, World!"));
            dataStream.Close();

            var result = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Equal("Hello, World!", result);
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

