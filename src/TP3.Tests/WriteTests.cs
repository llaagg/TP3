using System.Text;
using FakeItEasy;
using Google.Protobuf;
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
            var stream = A.Fake<ITP3DataStream>();

            var listOfResponses = new List<TP3WriteRequest>
            {
                new TP3WriteRequest { Data = ByteString.CopyFrom(new byte[] { (byte)'1', (byte)'2', (byte)'3' }), Offset = 0 },
                new TP3WriteRequest { Data = ByteString.CopyFrom(new byte[] { (byte)'4', (byte)'5', (byte)'6' }), Offset = 3 },
                new TP3WriteRequest { Data = ByteString.CopyFrom(new byte[] { (byte)'7', (byte)'8', (byte)'9' }), Offset = 6 }
            };

            // Act            
            TP3WriteRequestDataStreamWriter writer = new TP3WriteRequestDataStreamWriter(stream);
            foreach (var response in listOfResponses)
            {
                var written = await writer.Write(response);
            }

            // Assert
            //Assert.Equal("123456789", data);
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
    }
}
