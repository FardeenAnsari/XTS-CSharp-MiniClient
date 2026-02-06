using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using XTS_CSharp_MiniClient.Models;

namespace XTS_CSharp_MiniClient.Services
{
    // Assignment Requirement: Download OHLC data for equity (top 5 NIFTY 50 constituents)
    // Implementation: Uses NSECM (cash market) exchange segment
    // Hardcoded instrument IDs for top 5 stocks to simplify demo
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _rootUrl;
        private string? _token;

        // Top 5 NIFTY 50 constituents by market cap with XTS instrument IDs
        private readonly Dictionary<string, int> _equityInstruments = new()
        {
            { "RELIANCE", 2885 },      // Reliance Industries
            { "TCS", 11536 },          // Tata Consultancy Services
            { "HDFCBANK", 1333 },      // HDFC Bank
            { "INFY", 1594 },          // Infosys
            { "ICICIBANK", 4963 }      // ICICI Bank
        };

        public MarketDataService(string rootUrl)
        {
            _rootUrl = rootUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(rootUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public void SetToken(string token)
        {
            _token = token;
        }

        // Core method: Fetch OHLC data from XTS API
        // compressionValue: 60=1min, 180=3min, 300=5min, etc.
        public async Task<List<OhlcCandle>?> GetOhlcAsync(
            string exchangeSegment,
            int exchangeInstrumentID,
            string startTime,
            string endTime,
            int compressionValue = 60) // 60 = 1 minute
        {
            if (string.IsNullOrEmpty(_token))
            {
                Console.WriteLine("Error: Not authenticated. Call SetToken() first.");
                return null;
            }

            try
            {
                // Python: params = { 'exchangeSegment': ..., 'exchangeInstrumentID': ..., ... }
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["exchangeSegment"] = exchangeSegment;
                queryParams["exchangeInstrumentID"] = exchangeInstrumentID.ToString();
                queryParams["startTime"] = startTime;
                queryParams["endTime"] = endTime;
                queryParams["compressionValue"] = compressionValue.ToString();

                var url = $"/apimarketdata/instruments/ohlc?{queryParams}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                // Python: headers.update({'Authorization': self.token})
                request.Headers.Add("Authorization", _token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"OHLC request failed: {response.StatusCode}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var ohlcResponse = JsonConvert.DeserializeObject<OhlcResponse>(responseBody);

                if (ohlcResponse?.Type == "success" && ohlcResponse.Result != null)
                {
                    // API returns pipe-delimited string: "timestamp|open|high|low|close|volume|oi|,..."
                    string? dataStr = ohlcResponse.Result.DataResponse ?? ohlcResponse.Result.ListQuotes;
                    return ParseOhlcString(dataStr);
                }
                else
                {
                    Console.WriteLine($"OHLC failed: {ohlcResponse?.Description}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OHLC exception: {ex.Message}");
                return null;
            }
        }

        private List<OhlcCandle> ParseOhlcString(string? data)
        {
            var candles = new List<OhlcCandle>();
            if (string.IsNullOrWhiteSpace(data)) return candles;

            // Split by comma to get individual candle strings
            var candleStrings = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var candleStr in candleStrings)
            {
                // Split by pipe: timestamp|open|high|low|close|volume|openInterest|
                var parts = candleStr.Split('|');
                if (parts.Length >= 7)
                {
                    try
                    {
                        candles.Add(new OhlcCandle
                        {
                            Timestamp = long.Parse(parts[0]),
                            Open = decimal.Parse(parts[1]),
                            High = decimal.Parse(parts[2]),
                            Low = decimal.Parse(parts[3]),
                            Close = decimal.Parse(parts[4]),
                            Volume = long.Parse(parts[5]),
                            OpenInterest = long.Parse(parts[6])
                        });
                    }
                    catch { /* Skip malformed candles */ }
                }
            }
            return candles;
        }

        // Assignment Task: Download OHLC for top 5 NIFTY 50 stocks
        // Iterates through equity list and saves each to CSV
        public async Task DownloadTop5NiftyOhlcAsync(string startTime, string endTime)
        {
            Console.WriteLine("\n=== Task 2: Downloading OHLC for Top 5 NIFTY 50 Stocks ===\n");

            foreach (var equity in _equityInstruments)
            {
                Console.WriteLine($"Fetching OHLC for {equity.Key} (ID: {equity.Value})...");

                var candles = await GetOhlcAsync(
                    exchangeSegment: "NSECM",  // Cash Market
                    exchangeInstrumentID: equity.Value,
                    startTime: startTime,
                    endTime: endTime,
                    compressionValue: 60  // 1 minute candles
                );

                if (candles != null && candles.Count > 0)
                {
                    Console.WriteLine($"  [OK] Received {candles.Count} candles");
                    Console.WriteLine($"       First: {candles[0]}");
                    Console.WriteLine($"       Last:  {candles[^1]}");
                    
                    // Save to CSV file
                    SaveToCsv(candles, $"OHLC_Equity_{equity.Key}.csv");
                    Console.WriteLine($"       Saved to: Data/OHLC_Equity_{equity.Key}.csv");
                }
                else
                {
                    Console.WriteLine($"  [WARN] No data received");
                }

                Console.WriteLine();
                
                await Task.Delay(500);
            }
        }

        // Export OHLC data to CSV for offline analysis
        private void SaveToCsv(List<OhlcCandle> candles, string filename)
        {
            try
            {
                // Create Data folder if it doesn't exist
                Directory.CreateDirectory("Data");
                
                var csv = new StringBuilder();
                csv.AppendLine("DateTime,Open,High,Low,Close,Volume,OpenInterest");
                
                foreach (var candle in candles)
                {
                    csv.AppendLine($"{candle.GetDateTime():yyyy-MM-dd HH:mm:ss},{candle.Open},{candle.High},{candle.Low},{candle.Close},{candle.Volume},{candle.OpenInterest}");
                }
                
                File.WriteAllText(Path.Combine("Data", filename), csv.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Failed to save CSV: {ex.Message}");
            }
        }
    }
}
