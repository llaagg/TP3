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
        public async Task Command_StreamMode_CanReadAndWriteOnSameDuplexStream()
        {
            string sent = "Hello command";
            var cmd = new EchoCommand();
            var duplex = new InMemoryDuplexStream($"{sent}");

            await cmd.Command(duplex);

            var output = duplex.GetWrittenText();
            Assert.Equal($"Echo: {sent}", output);
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
            List<string> path = new List<string> { "service1", nameof(EchoCommand) };
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
            //// aka send args
            await sut.Handle(fakeNetworkPipe, FlowTest.WriteMessage("tag2", "hello my friend"));
            var lastWriteResponse = lastMessageSent.WriteResponse;
            Assert.Equal(NodeType.File, nodeType);
            Assert.Equal(15UL, lastWriteResponse!.Count);
            //// akaa get response aka READ
            await sut.Handle(fakeNetworkPipe, FlowTest.ReadMessage("tag2", 0, 1000));
            var lastReadResponse = lastMessageSent.ReadResponse;
            Assert.Equal("Echo: hello my friend", lastReadResponse!.Data.ToStringUtf8());

        }

        private sealed class InMemoryDuplexStream : Stream
        {
            private readonly byte[] inputBytes;
            private readonly MemoryStream output = new MemoryStream();
            private int readPosition;
            private bool headerServed;
            private readonly int headerEndIndex;

            public InMemoryDuplexStream(string inputText)
            {
                inputBytes = Encoding.UTF8.GetBytes(inputText);
                var newline = Array.IndexOf(inputBytes, (byte)'\n');
                headerEndIndex = newline >= 0 ? newline + 1 : inputBytes.Length;
            }

            public string GetWrittenText()
            {
                return Encoding.UTF8.GetString(output.ToArray());
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                output.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (readPosition >= inputBytes.Length)
                {
                    return 0;
                }

                int remaining = inputBytes.Length - readPosition;
                int bytesToRead;

                if (!headerServed)
                {
                    var headerRemaining = headerEndIndex - readPosition;
                    bytesToRead = Math.Min(count, Math.Max(0, headerRemaining));
                    headerServed = true;
                }
                else
                {
                    bytesToRead = Math.Min(count, remaining);
                }

                Array.Copy(inputBytes, readPosition, buffer, offset, bytesToRead);
                readPosition += bytesToRead;
                return bytesToRead;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return Task.FromResult(Read(buffer, offset, count));
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                output.Write(buffer, offset, count);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return output.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
        }
    }

    public class EchoCommand : BaseControlCommand
    {
        protected override async Task HandleStreamCommand(Stream input, Stream output, StreamWriter control)
        {
            control.Write("Echo: ");

            // Demo behavior: pass bytes through. Derived commands can replace this with transforms.
            await input.CopyToAsync(output);
            await output.FlushAsync();
        }
    }
}
