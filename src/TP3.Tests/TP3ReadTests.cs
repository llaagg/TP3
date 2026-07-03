using System.Text;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

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
            Encoding.UTF8.GetString(new TP3StatPayload(child).Serialize())));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task TP3ReadResponseDataStream_DeserilizedFolderInfo()
    {
        // Arrange
        var files = new [] { 
            new TP3StatPayload(TP3Helpers.SetupNode("file1.txt", NodeType.File)), 
            new TP3StatPayload(TP3Helpers.SetupNode("file2.txt", NodeType.File)),
            new TP3StatPayload(TP3Helpers.SetupNode("file3.txt", NodeType.File))
        };
        
        var serilizeddata = files.SelectMany(f => f.Serialize()).ToArray();

        var listOfResponses = new List<TP3ReadResponse>
        {
            new TP3ReadResponse { Data = serilizeddata },
        };

        // Act
        TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(listOfResponses);
        var sr = new StreamReader(stream);
        var data = sr.ReadToEnd();
        var stringData = Encoding.UTF8.GetString(serilizeddata);
        
        // Assert
        Assert.Equal("123456789", data);
    }
}