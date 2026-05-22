using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ScreenTimeMonitor.Service.Services
{
    /// <summary>
    /// Manages inter-process communication between the Service and UI via Named Pipes.
    /// </summary>
    public class IPCService : IIPCService, IDisposable
    {
        private readonly ILogger<IPCService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundProcessMonitorService _backgroundProcessMonitor;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenTask;
        private bool _isRunning;
        private int _connectedClients;
        private readonly string _pipeName;

        public IPCService(ILogger<IPCService> logger, IConfiguration configuration, IBackgroundProcessMonitorService backgroundProcessMonitor)
        {
            _logger = logger;
            _configuration = configuration;
            _backgroundProcessMonitor = backgroundProcessMonitor;
            _pipeName = configuration.GetValue<string>("UISettings:ServicePipeName") ?? "ScreenTimeMonitor.Pipe";
            _connectedClients = 0;
        }

        /// <summary>
        /// Starts the IPC server (listening for UI connections).
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                _logger.LogWarning("IPC service is already running");
                return;
            }

            try
            {
                _logger.LogInformation($"Starting IPC service on pipe: {_pipeName}...");

                _cancellationTokenSource = new CancellationTokenSource();
                _listenTask = ListenForClientsAsync(_cancellationTokenSource.Token);

                _isRunning = true;
                _logger.LogInformation("IPC service started successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start IPC service");
                throw;
            }
        }

        /// <summary>
        /// Stops the IPC server.
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping IPC service...");

                _cancellationTokenSource?.Cancel();

                if (_listenTask != null)
                {
                    try
                    {
                        await _listenTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                    }
                }

                _pipeServer?.Close();
                _pipeServer?.Dispose();

                _isRunning = false;
                _logger.LogInformation("IPC service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping IPC service");
                throw;
            }
        }

        /// <summary>
        /// Sends data to connected UI clients.
        /// </summary>
        public async Task BroadcastDataAsync(string data)
        {
            try
            {
                if (_pipeServer == null || !_pipeServer.IsConnected)
                {
                    _logger.LogDebug("No connected clients to broadcast to");
                    return;
                }

                var buffer = Encoding.UTF8.GetBytes(data);
                
                try
                {
                    await _pipeServer.WriteAsync(buffer, 0, buffer.Length);
                    await _pipeServer.FlushAsync();
                    _logger.LogDebug($"Broadcast {buffer.Length} bytes to UI");
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Client disconnected during broadcast");
                    _connectedClients = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting data");
            }
        }

        /// <summary>
        /// Gets the number of connected clients.
        /// </summary>
        public int GetConnectedClientCount()
        {
            return _connectedClients;
        }

        /// <summary>
        /// Background task that listens for client connections.
        /// </summary>
        private async Task ListenForClientsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Create a new pipe server for each client (sequentially)
                        using var server = new NamedPipeServerStream(
                            _pipeName,
                            PipeDirection.InOut,
                            1,
                            PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous
                        );

                        _pipeServer = server;
                        _logger.LogInformation("Waiting for IPC client connection...");

                        // Wait for client connection with cancellation support
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        linkedCts.Token.Register(() => server.Close());

                        await server.WaitForConnectionAsync(linkedCts.Token);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        _connectedClients = 1;
                        _logger.LogInformation("IPC client connected");

                        // Handle client communication, then dispose before next loop
                        await HandleClientAsync(server, cancellationToken);
                        _connectedClients = 0;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error accepting IPC connection: {ex.GetType().Name} - {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            _logger.LogError(ex.InnerException, "Inner exception details");
                        }
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in IPC listen loop");
            }
            finally
            {
                _pipeServer?.Close();
                _connectedClients = 0;
            }
        }

        /// <summary>
        /// Handles communication with a connected client.
        /// </summary>
        private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[4096];

                while (!cancellationToken.IsCancellationRequested && pipeServer.IsConnected)
                {
                    try
                    {
                        int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        
                        if (bytesRead == 0)
                        {
                            break; // Client closed connection
                        }

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        _logger.LogDebug($"Received from client: {message}");

                        // Handle commands
                        if (message.StartsWith("PING"))
                        {
                            var response = "PONG\n";
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length);
                            await pipeServer.FlushAsync();
                        }
                        else if (message.StartsWith("UI_CONNECTED"))
                        {
                            // UI connected - reset session tracking so Live Activity shows session-relative times
                            _backgroundProcessMonitor.ResetSessionTracking();
                            var response = "OK\n";
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length);
                            await pipeServer.FlushAsync();
                            _logger.LogInformation("UI connected - session tracking reset for Live Activity");
                        }
                        else if (message.StartsWith("GET_RUNNING_APPS"))
                        {
                            // Get currently running apps from background process monitor, aggregated by app name
                            var allRunningApps = _backgroundProcessMonitor.GetAllRunningApps();
                            
                            // Format: APP_NAME|DURATION_MS|APP_NAME|DURATION_MS|...
                            var appsList = new List<string>();
                            foreach (var (appName, durationMs) in allRunningApps)
                            {
                                appsList.Add(appName);
                                appsList.Add(durationMs.ToString());
                            }
                            
                            var response = string.Join("|", appsList);
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length);
                            await pipeServer.FlushAsync();
                            _logger.LogDebug($"Sent {appsList.Count / 2} unique running apps to UI");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in client handler");
                        break;
                    }
                }
            }
            finally
            {
                _connectedClients = 0;
                _logger.LogInformation("IPC client disconnected");
            }
        }

        public void Dispose()
        {
            _pipeServer?.Dispose();
            _cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
