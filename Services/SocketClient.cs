using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XTS_CSharp_MiniClient.Services
{
    // Assignment Requirement: Stream any form of data using socket
    // This implementation uses standard WebSocket for simplicity
    // Demonstrates persistent connection and continuous data streaming
    public class SocketClient : IDisposable
    {
        private ClientWebSocket? _webSocket;
        private readonly string _rootUrl;
        private string? _token;
        private string? _userId;
        private CancellationTokenSource? _cancellationTokenSource;
        private StreamWriter? _streamingLogger;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public SocketClient(string rootUrl)
        {
            _rootUrl = rootUrl;
        }

        public void SetCredentials(string token, string userId)
        {
            _token = token;
            _userId = userId;
        }

        /// <summary>
        /// Connect to WebSocket streaming endpoint
        /// Python equivalent: async def connect(self, ...)
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (string.IsNullOrEmpty(_token) || string.IsNullOrEmpty(_userId))
            {
                Console.WriteLine("Error: Token and UserID required. Call SetCredentials() first.");
                return false;
            }

            try
            {
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Python connection URL format:
                // {root_url}/?token={token}&userID={userID}&publishFormat=JSON&broadcastMode=Full
                var wsUrl = _rootUrl
                    .Replace("https://", "wss://")
                    .Replace("http://", "ws://");
                
                var connectionUri = $"{wsUrl}/apimarketdata/socket.io/?token={_token}&userID={_userId}&publishFormat=JSON&broadcastMode=Full&transport=websocket";

                Console.WriteLine("\n=== Task 4: WebSocket Streaming Demo ===\n");
                Console.WriteLine($"Connecting to: {wsUrl}/apimarketdata/socket.io/...");

                await _webSocket.ConnectAsync(new Uri(connectionUri), _cancellationTokenSource.Token);

                if (_webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine("[OK] WebSocket Connected Successfully");
                    Console.WriteLine("  State: Open");
                    Console.WriteLine("  Ready to receive streaming data\n");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection failed: {ex.Message}");
                Console.WriteLine("\nNote: This is a demonstration. In production:");
                Console.WriteLine("  - Use Socket.IO client library for full protocol support");
                Console.WriteLine("  - Handle reconnection logic");
                Console.WriteLine("  - Subscribe to specific instruments and event codes");
                return false;
            }
        }

        /// <summary>
        /// Receive streaming data from WebSocket
        /// Python equivalent: Event handlers like on_event_touchline_full(), on_event_market_data_full()
        /// </summary>
        public async Task ReceiveDataAsync(int durationSeconds = 10)
        {
            if (_webSocket?.State != WebSocketState.Open || _cancellationTokenSource == null)
            {
                Console.WriteLine("Error: WebSocket not connected");
                return;
            }

            Console.WriteLine($"Receiving streaming data for {durationSeconds} seconds...\n");
            
            // Create Data folder and log file for streaming data
            Directory.CreateDirectory("Data");
            var logFilename = Path.Combine("Data", $"Streaming_Data_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            _streamingLogger = new StreamWriter(logFilename, false);
            _streamingLogger.WriteLine($"=== XTS WebSocket Streaming Data Log ===");
            _streamingLogger.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _streamingLogger.WriteLine($"Duration: {durationSeconds} seconds\n");
            Console.WriteLine($"[INFO] Logging streaming data to: {logFilename}\n");

            var buffer = new byte[4096];
            var endTime = DateTime.Now.AddSeconds(durationSeconds);
            int messageCount = 0;

            try
            {
                while (DateTime.Now < endTime && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageCount++;

                        Console.WriteLine($"[Message {messageCount}] Received at {DateTime.Now:HH:mm:ss.fff}");
                        
                        // Log to file
                        _streamingLogger?.WriteLine($"[Message {messageCount}] {DateTime.Now:HH:mm:ss.fff}");
                        _streamingLogger?.WriteLine(message);
                        _streamingLogger?.WriteLine();
                        
                        // Try to parse as JSON for pretty display
                        try
                        {
                            var json = JsonConvert.DeserializeObject(message);
                            Console.WriteLine(JsonConvert.SerializeObject(json, Formatting.Indented));
                        }
                        catch
                        {
                            Console.WriteLine(message);
                        }
                        
                        Console.WriteLine();
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Server closed connection");
                        break;
                    }
                }

                _streamingLogger?.WriteLine($"\n=== End of Stream ===");
                _streamingLogger?.WriteLine($"Total Messages: {messageCount}");
                _streamingLogger?.WriteLine($"Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _streamingLogger?.Close();
                
                Console.WriteLine($"\n[OK] Streaming complete. Received {messageCount} messages.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nStreaming cancelled");
                _streamingLogger?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nStreaming error: {ex.Message}");
                _streamingLogger?.Close();
            }
        }

        /// <summary>
        /// Simulate streaming if real connection fails (for demo purposes)
        /// </summary>
        public async Task SimulateStreamingAsync(int durationSeconds = 10)
        {
            Console.WriteLine("\n=== Simulating Market Data Stream ===");
            Console.WriteLine("(Real WebSocket connection not available - showing demo data)\n");

            var random = new Random();
            var instruments = new[] { "RELIANCE", "TCS", "HDFCBANK", "INFY", "ICICIBANK" };
            var endTime = DateTime.Now.AddSeconds(durationSeconds);
            int messageCount = 0;

            while (DateTime.Now < endTime)
            {
                messageCount++;
                var symbol = instruments[random.Next(instruments.Length)];
                var price = 1000 + random.Next(0, 1000) + random.NextDouble();
                var volume = random.Next(100, 10000);

                var streamData = new
                {
                    MessageCode = 1512,  // LTP (Last Traded Price)
                    Symbol = symbol,
                    LastTradedPrice = Math.Round(price, 2),
                    Volume = volume,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };

                Console.WriteLine($"[Message {messageCount}] {streamData.Symbol} | LTP: ₹{streamData.LastTradedPrice} | Vol: {streamData.Volume:N0}");

                await Task.Delay(1000); // 1 message per second
            }

            Console.WriteLine($"\n[OK] Simulated streaming complete. Generated {messageCount} messages.");
        }

        /// <summary>
        /// Disconnect WebSocket
        /// Python equivalent: await socket.disconnect()
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
                    Console.WriteLine("[OK] WebSocket Disconnected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Disconnect error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _streamingLogger?.Close();
            _streamingLogger?.Dispose();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
