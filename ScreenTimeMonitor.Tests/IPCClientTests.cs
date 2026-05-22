using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using ScreenTimeMonitor.UI.Services;
using Xunit;

namespace ScreenTimeMonitor.Tests
{
    public class IPCClientTests
    {
        [Fact]
        public async Task IPCClient_PingPong_Works()
        {
            var pipeName = "Test.ScreenTimeMonitor.Pipe." + Guid.NewGuid().ToString("N");

            // Start a lightweight server that will accept one connection and respond to PING
            var serverTask = Task.Run(async () =>
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync();

                var buffer = new byte[1024];
                int read = await server.ReadAsync(buffer, 0, buffer.Length);
                var msg = Encoding.UTF8.GetString(buffer, 0, read);

                    if (msg.StartsWith("PING"))
                    {
                        var resp = Encoding.UTF8.GetBytes("PONG\n");
                        await server.WriteAsync(resp, 0, resp.Length);
                        await server.FlushAsync();
                    }

                    // allow client to read
                    await Task.Delay(200);
            });

            // Create client
            var client = new IPCClient(pipeName);

            // Small delay to avoid race with server initialization
                await Task.Delay(150);

            // Connect and SendPing
                await client.ConnectAsync(5000);
                var response = await client.SendPingAsync(5000);

            // Cleanup
            await client.DisconnectAsync();

            // Ensure server task completes
            await serverTask;

            Assert.NotNull(response);
            Assert.Contains("PONG", response);
        }
    }
}
