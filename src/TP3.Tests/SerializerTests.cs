using System.IO;
using System.Threading.Tasks;
using TP3.Agent.Logic.Protocol;
using TP3.Messages;
using Xunit;

namespace TP3.Tests;

public class SerializerTests
{
    [Fact]
    public async Task BinarySerializer_RoundTripsMessage()
    {
        var message = new TP3Message(TP3Command.ECHO, "target", "payload");
        var payload = TP3Serializer.SerializeBytes(message, TP3SerializationFormat.Binary);

        using var stream = new MemoryStream(payload);
        var roundTripped = await TP3Serializer.ReadMessageAsync(stream, default);

        Assert.Equal(message.Command, roundTripped.Command);
        Assert.Equal(message.Target, roundTripped.Target);
        Assert.Equal(message.Payload, roundTripped.Payload);
    }

    [Fact]
    public async Task JsonSerializer_RoundTripsMessage()
    {
        var message = new TP3Message(TP3Command.LIST, "node", "data");
        var payload = TP3Serializer.SerializeBytes(message, TP3SerializationFormat.Json);

        using var stream = new MemoryStream(payload);
        var roundTripped = await TP3Serializer.ReadMessageAsync(stream, default);

        Assert.Equal(message.Command, roundTripped.Command);
        Assert.Equal(message.Target, roundTripped.Target);
        Assert.Equal(message.Payload, roundTripped.Payload);
    }

    [Fact]
    public void DeserializeBytes_RecognizesBinaryHeader()
    {
        var message = new TP3Message(TP3Command.HELP, "foo", "bar");
        var payload = TP3Serializer.SerializeBytes(message, TP3SerializationFormat.Binary);
        var roundTripped = TP3Serializer.DeserializeBytes(payload);

        Assert.Equal(message.Command, roundTripped.Command);
        Assert.Equal(message.Target, roundTripped.Target);
        Assert.Equal(message.Payload, roundTripped.Payload);
    }

    [Fact]
    public void DeserializeBytes_RecognizesJsonHeader()
    {
        var message = new TP3Message(TP3Command.QUIT, "foo", "bar");
        var payload = TP3Serializer.SerializeBytes(message, TP3SerializationFormat.Json);
        var roundTripped = TP3Serializer.DeserializeBytes(payload);

        Assert.Equal(message.Command, roundTripped.Command);
        Assert.Equal(message.Target, roundTripped.Target);
        Assert.Equal(message.Payload, roundTripped.Payload);
    }
}
