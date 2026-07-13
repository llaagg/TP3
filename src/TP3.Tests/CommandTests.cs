using Castle.Core.Logging;
using FakeItEasy;
using TP3.Agent.Logic.Agent;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using System.Text;
using TP3.Tests.Integration;

namespace TP3.Tests.Protocol
{
    public class CommandTests
    {
        [Fact]
        public async Task RunsCommands()
        {
            var cmd = new EchoCommand();

            var readMessage = new TP3Message
            {
                Tag = "test",
            };
            readMessage.ReadRequest = new TP3ReadRequest
            {
                Offset = 0,
                MaxBytes = 1000
            };

            // let's use reader logic
            MessageHandler handler = new MessageHandler(A.Fake<IAgent>(), A.Fake<IRouter>());
            await handler.Handle(A.Fake<INetworkPipe>(), readMessage);
        }

        [Fact]
        public async Task Command_Get_Stream_GluesWriteToReadOutput()
        {
            string sent = "Hello from Get()";
            var cmd = new EchoCommand();
            var dataStream = await cmd.Get();
            Assert.NotNull(dataStream);

            await dataStream!.Open();
            var request = Encoding.UTF8.GetBytes($"{sent}");
            await dataStream.Write(0, request);

            var responseBytes = await dataStream.Read(0, 4096);
            var response = Encoding.UTF8.GetString(responseBytes);

            Assert.Equal($"Echo: {sent}", response);
        }

        [Fact]
        public async Task Command_Get_ParamsArgsHelper_ParsesInputAndWritesOutput()
        {
            var cmd = new ArgsEchoCommand();
            var dataStream = await cmd.Get();
            Assert.NotNull(dataStream);

            await dataStream!.Open();
            var request = Encoding.UTF8.GetBytes("hello   my   friend");
            await dataStream.Write(0, request);

            var responseBytes = await dataStream.Read(0, 4096);
            var response = Encoding.UTF8.GetString(responseBytes);

            Assert.Equal("Args: hello|my|friend", response);
        }

        [Fact]
        public async Task Command_IntegrationTest()
        {
            var command = new EchoCommand();

            var targetMemoryStream = new MemoryStream();

            TP3Message lastMessageSent = null!;

            var (fakeNetworkPipe, sut) = await FlowTest.InitilizeTP3(
                new List<INode> { command }, 
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
            List<string> path = new List<string> { "services", "service1", "echo-command"};
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
            Assert.Equal(NodeType.Command, nodeType);

            // * WRITE
            //// aka send args
            await sut.Handle(fakeNetworkPipe, FlowTest.WriteMessage("tag2", "hello my friend"));
            var lastWriteResponse = lastMessageSent.WriteResponse;
            Assert.Equal(NodeType.Command, nodeType);
            Assert.Equal(15UL, lastWriteResponse!.Count);
            //// akaa get response aka READ
            await sut.Handle(fakeNetworkPipe, FlowTest.ReadMessage("tag2", 0, 1000));
            var lastReadResponse = lastMessageSent.ReadResponse;
            Assert.Equal("Echo: hello my friend", lastReadResponse!.Data.ToStringUtf8());


            // CLUNK
            await sut.Handle(fakeNetworkPipe, FlowTest.ClunkMessage("tag2"));
            Assert.Equal(TP3Message.PayloadOneofCase.ClunkResponse, lastMessageSent.PayloadCase);
        }
   }

    public class ArgsEchoCommand : BaseControlParamsArgsCommand
    {
        protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
        {
            using var writer = new StreamWriter(output, leaveOpen: true)
            {
                AutoFlush = true
            };

            await writer.WriteAsync($"Args: {string.Join("|", args ?? Array.Empty<string>())}");
        }
    }

    public class EchoCommand : BaseControlCommand
    {
        protected override async Task HandleStreamCommand(Stream input, Stream output)
        {
            using var writer = new StreamWriter(output, leaveOpen: true)
            {
                AutoFlush = true
            };
            writer.Write("Echo: ");

            // Demo behavior: pass bytes through. Derived commands can replace this with transforms.
            await input.CopyToAsync(output);
            await output.FlushAsync();
        }
    }
}
