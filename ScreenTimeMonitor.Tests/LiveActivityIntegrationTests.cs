using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using ScreenTimeMonitor.UI.Services;
using Xunit;

namespace ScreenTimeMonitor.Tests
{
    public class LiveActivityIntegrationTests
    {
        [Fact(Timeout = 10000)]
        public async Task IPCClient_Receives_Broadcasts_From_SimulatedServer()
        {
            var pipeName = "LiveActivityTestPipe_" + Guid.NewGuid().ToString("N");
            var received = new ConcurrentBag<string>();
            var cts = new System.Threading.CancellationTokenSource();

            // Server: accept the connection then broadcast messages periodically
            var serverTask = Task.Run(async () =>
            {
                // Use InOut to match the client's InOut direction
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cts.Token);

                for (int i = 0; i < 5; i++)
                {
                    var msg = $"EVENT:{DateTime.UtcNow:O}:count={i}\n";
                    var buffer = Encoding.UTF8.GetBytes(msg);
                    try
                    {
                        await server.WriteAsync(buffer, 0, buffer.Length, cts.Token);
                        await server.FlushAsync(cts.Token);
                    }
                    catch
                    {
                        break; // client disconnected
                    }
                    await Task.Delay(200, cts.Token);
                }

                // give client time to read
                await Task.Delay(200, cts.Token);
            }, cts.Token);

            // Client
            var client = new IPCClient(pipeName);
            client.OnMessageReceived += (s) => received.Add(s);

            try
            {
                // Give the server a tiny moment to initialize to avoid race
                await Task.Delay(50);
                await client.ConnectAsync(3000);

                // Wait until we receive at least one message or timeout
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 5000 && received.Count < 3)
                {
                    await Task.Delay(100);
                }

                await client.DisconnectAsync();

                // Ensure server task completes
                cts.CancelAfter(500);
                await serverTask;

                Assert.True(received.Count >= 1, "Expected to receive at least one broadcast message from simulated server");
            }
            finally
            {
                try { await client.DisconnectAsync(); } catch { }
                cts.Cancel();
            }
        }
    }
}
