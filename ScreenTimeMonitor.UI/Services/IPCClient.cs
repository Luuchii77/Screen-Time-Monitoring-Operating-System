using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenTimeMonitor.UI.Services
{
    public class IPCClient : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _client;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        public event Action<string>? OnMessageReceived;
        public bool IsConnected => _client != null && _client.IsConnected;

        public IPCClient(string pipeName = "ScreenTimeMonitor.Pipe")
        {
            _pipeName = pipeName;
        }

        public async Task ConnectAsync(int timeoutMs = 5000)
        {
            if (IsConnected) return;

            _client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            _cts = new CancellationTokenSource();

            var connectTask = _client.ConnectAsync(timeoutMs, _cts.Token);
            try
            {
                await connectTask;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Timed out connecting to the service pipe");
            }

            if (!_client.IsConnected)
            {
                throw new InvalidOperationException("Failed to connect to service pipe");
            }

            _listenTask = Task.Run(() => ListenLoop(_cts.Token));
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_listenTask != null)
                {
                    await _listenTask.ConfigureAwait(false);
                }
            }
            catch { }
            finally
            {
                _client?.Dispose();
                _client = null;
                _cts?.Dispose();
                _cts = null;
                _listenTask = null;
            }
        }

        public async Task SendAsync(string message)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to IPC server");
            var buffer = Encoding.UTF8.GetBytes(message);
            await _client!.WriteAsync(buffer, 0, buffer.Length);
            await _client.FlushAsync();
        }

        public async Task<string?> SendPingAsync(int timeoutMs = 2000)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to IPC server");

            // Send PING\n
            var message = "PING\n";
            // Use the message event to receive the reply to avoid concurrent reads
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(string msg)
            {
                // Resolve with the first message received
                tcs.TrySetResult(msg);
            }

            try
            {
                OnMessageReceived += Handler;
                await SendAsync(message);

                using var cts = new CancellationTokenSource(timeoutMs);
                using (cts.Token.Register(() => tcs.TrySetResult(null)))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                OnMessageReceived -= Handler;
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            if (_client == null) return;
            var buffer = new byte[4096];

            try
            {
                while (!token.IsCancellationRequested && _client.IsConnected)
                {
                    int read = 0;
                    try
                    {
                        read = await _client.ReadAsync(buffer, 0, buffer.Length, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (read == 0) break;

                    var msg = Encoding.UTF8.GetString(buffer, 0, read);
                    OnMessageReceived?.Invoke(msg);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }
    }
}
