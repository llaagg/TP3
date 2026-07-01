using System.Text;
using System.Text.Json;
using TP3.Agent.Logic.Agent;
using TP3.Messages;
using TP3.Service.FileSystem;

public class FileSystemServiceReadTests
{
    [Fact]
    public async Task WalkThenReadDirectory_StreamsJsonEntries_AndEndsWithEof()
    {
        var service = new FileSystemService();
        var walker = new PathWalker(() => new[] { service });

        var walkResponse = await walker.WalkAsync(new TP3WalkRequest
        {
            Args = new List<string>()
        });

        Assert.Null(walkResponse.Error);
        Assert.Equal(NodeType.Directory, walkResponse.NodeType);
        Assert.False(string.IsNullOrWhiteSpace(walkResponse.Qid));

        var qid = walkResponse.Qid!;
        long offset = 0;
        var sawEof = false;

        for (var i = 0; i < 512; i++)
        {
            var readResponse = await walker.ReadAsync(new TP3ReadRequest
            {
                Qid = qid,
                Offset = offset,
                MaxBytes = 4096
            });

            Assert.Null(readResponse.Error);
            Assert.Equal(TP3Command.READ, readResponse.Command);
            Assert.Equal(qid, readResponse.Qid);

            var payload = Encoding.UTF8.GetString(readResponse.Data ?? Array.Empty<byte>());
            if (payload == "EOF")
            {
                Assert.True(readResponse.IsFinalChunk);
                sawEof = true;
                break;
            }

            Assert.False(readResponse.IsFinalChunk);
            Assert.NotEmpty(payload);

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.True(root.TryGetProperty("Name", out var nameElement));
            Assert.True(root.TryGetProperty("Type", out var typeElement));
            Assert.Equal(JsonValueKind.String, nameElement.ValueKind);
            Assert.Equal(JsonValueKind.String, typeElement.ValueKind);

            Assert.True(readResponse.Offset > offset, "Offset must move forward for non-EOF entries.");
            offset = readResponse.Offset;
        }

        Assert.True(sawEof, "Directory stream did not terminate with EOF.");
    }
}