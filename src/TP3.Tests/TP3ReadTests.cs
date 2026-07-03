using System.Text;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;


namespace TP3.Tests.Protocol
{
    public class TP3ReadResponseTests
    {
        [Fact]
        public async Task TP3ReadResponseDataStream_ReadsDataCorrectly()
        {
            // Arrange
            var listOfResponses = new List<TP3ReadResponse>
        {
            new TP3ReadResponse { Data = new byte[] { (byte)'1', (byte)'2', (byte)'3' } },
            new TP3ReadResponse { Data = new byte[] { (byte)'4', (byte)'5', (byte)'6' } },
            new TP3ReadResponse { Data = new byte[] { (byte)'7', (byte)'8', (byte)'9' } }
        };

            // Act
            TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(listOfResponses);
            var sr = new StreamReader(stream);
            var data = sr.ReadToEnd();

            // Assert
            Assert.Equal("123456789", data);
        }


    }
}