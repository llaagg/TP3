using Castle.Core.Logging;
using FakeItEasy;
using TP3.Agent.Logic.Agent;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Tests.Protocol
{
    public class CommandTests
    {
        [Fact]
        public async Task RunsCommands()
        {
            var cmd = new TestCommand();

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
    }

    public class TestCommand : BaseControlCommandNode
    {
        protected override Task HandleArgsCommand(string[] args, StreamWriter output)
        {
            return output.WriteLineAsync($"ok args: {string.Join(", ", args)}");
        }

        protected override async Task HandleStreamCommand(Stream input, Stream output, StreamWriter control)
        {
            await control.WriteLineAsync("ok stream started");

            // Demo behavior: pass bytes through. Derived commands can replace this with transforms.
            await input.CopyToAsync(output);
            await output.FlushAsync();
        }
    }
}
