using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using XTS_CSharp_MiniClient.Models;

namespace XTS_CSharp_MiniClient.Services
{
    // Assignment Requirement: Download monthly (near_month) F&O 1-min data
    // Implementation Notes:
    // - Dynamically fetches current F&O instrument IDs from master data API
    // - Filters for FUTIDX (NIFTY) and FUTSTK (HDFCBANK) series
    // - Selects near-month contract (nearest expiry) for each underlying
    // - Downloads 1-minute OHLC data using compressionValue=60
    public class FnoDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _rootUrl;
        private string? _token;
        
        // Stores discovered near-month F&O contracts: Key = "Symbol-FUT-ExpiryDate", Value = InstrumentID
        private Dictionary<string, int> _fnoInstruments = new();

        public FnoDataService(string rootUrl)
        {
            _rootUrl = rootUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(rootUrl),
                Timeout = TimeSpan.FromSeconds(60)  // Extended timeout for master data API responses
            };
        }

        public void SetToken(string token)
        {
            _token = token;
        }

        // Fetches OHLC data for F&O instruments from NSEFO exchange segment
        // compressionValue=60 gives 1-minute candles as per assignment requirement
        public async Task<List<OhlcCandle>?> GetFnoOhlcAsync(
            int exchangeInstrumentID,
            string startTime,
            string endTime)
        {
            if (string.IsNullOrEmpty(_token))
            {
                Console.WriteLine("Error: Not authenticated. Call SetToken() first.");
                return null;
            }

            try
            {
                // Build request parameters - compressionValue=60 gives 1-minute candles as per assignment
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["exchangeSegment"] = "NSEFO";  // F&O segment
                queryParams["exchangeInstrumentID"] = exchangeInstrumentID.ToString();
                queryParams["startTime"] = startTime;  // Format: "Feb 03 2026 091500"
                queryParams["endTime"] = endTime;
                queryParams["compressionValue"] = "60";  // 1 minute

                var url = $"/apimarketdata/instruments/ohlc?{queryParams}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", _token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"F&O OHLC request failed: {response.StatusCode}");
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var ohlcResponse = JsonConvert.DeserializeObject<OhlcResponse>(responseBody);

                if (ohlcResponse?.Type == "success" && ohlcResponse.Result != null)
                {
                    // XTS API returns pipe-delimited string format (not JSON array)
                    // Format: "timestamp|open|high|low|close|volume|openInterest|,..."
                    string? dataStr = ohlcResponse.Result.DataResponse ?? ohlcResponse.Result.ListQuotes;
                    return ParseOhlcString(dataStr);
                }
                else
                {
                    // More detailed error logging for F&O debugging
                    Console.WriteLine($"  [WARN] API Response Type: {ohlcResponse?.Type}");
                    Console.WriteLine($"  [WARN] Description: {ohlcResponse?.Description}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Exception: {ex.Message}");
                return null;
            }
        }

        // Parses XTS API's proprietary pipe-delimited data format into structured objects
        private List<OhlcCandle> ParseOhlcString(string? data)
        {
            var candles = new List<OhlcCandle>();
            if (string.IsNullOrWhiteSpace(data)) return candles;

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

        // Assignment Requirement: Download monthly (near_month) F&O price 1-min data
        // Underlying: HDFCBANK, NIFTY
        // Approach: Fetch master data first, then identify near-month contracts
        public async Task DownloadNearMonthFnoOhlcAsync(string startTime, string endTime)
        {
            Console.WriteLine("\n=== Task 3: Downloading Near-Month F&O 1-Min Data ===\n");
            Console.WriteLine("Underlying: HDFCBANK, NIFTY");
            Console.WriteLine("Expiry: Near Month (February 2026)");
            Console.WriteLine("Interval: 1 minute\n");

            // Fetch real instrument IDs from master data
            Console.WriteLine("Fetching F&O master data to find near-month contracts...");
            await FetchNearMonthInstruments();
            
            if (_fnoInstruments.Count == 0)
            {
                Console.WriteLine("[WARN] No near-month contracts found. Skipping F&O data download.\n");
                return;
            }
            
            Console.WriteLine($"[OK] Found {_fnoInstruments.Count} near-month contract(s)\n");

            foreach (var contract in _fnoInstruments)
            {
                Console.WriteLine($"Fetching OHLC for {contract.Key} (ID: {contract.Value})...");

                var candles = await GetFnoOhlcAsync(
                    exchangeInstrumentID: contract.Value,
                    startTime: startTime,
                    endTime: endTime
                );

                if (candles != null && candles.Count > 0)
                {
                    Console.WriteLine($"  [OK] Received {candles.Count} 1-min candles");
                    Console.WriteLine($"       First: {candles[0]}");
                    Console.WriteLine($"       Last:  {candles[^1]}");
                    
                    // Display Open Interest for F&O
                    if (candles[^1].OpenInterest > 0)
                    {
                        Console.WriteLine($"       Open Interest: {candles[^1].OpenInterest:N0}");
                    }
                    
                    // Save to CSV file
                    SaveToCsv(candles, $"OHLC_FNO_{contract.Key}.csv");
                    Console.WriteLine($"       Saved to: Data/OHLC_FNO_{contract.Key}.csv");
                }
                else
                {
                    Console.WriteLine($"  [WARN] No data received");
                }

                Console.WriteLine();
                
                // Avoid rate limiting
                await Task.Delay(500);
            }
        }

        /// <summary>
        /// Save F&O OHLC candles to CSV file for analysis
        /// </summary>
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

        // Fetch instrument master data from XTS API
        // Required because F&O instrument IDs change with each expiry cycle
        private async Task FetchNearMonthInstruments()
        {
            try
            {
                var url = "/apimarketdata/instruments/master";
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", _token);
                request.Content = new StringContent("{\"exchangeSegmentList\":[\"NSEFO\"]}", 
                    Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  [WARN] Master data fetch failed: {response.StatusCode}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var masterResponse = JsonConvert.DeserializeObject<ApiResponse<dynamic>>(json);
                
                if (masterResponse?.IsSuccess != true || masterResponse.Result == null)
                {
                    Console.WriteLine($"  [WARN] Master data response invalid");
                    return;
                }

                // Parse master data to find near-month futures
                string? resultStr = masterResponse.Result?.ToString();
                if (!string.IsNullOrWhiteSpace(resultStr))
                {
                    ParseNearMonthFutures(resultStr!);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Exception fetching master data: {ex.Message}");
            }
        }

        // Parse master data to extract near-month futures
        // Near-month = contract with nearest expiry among active contracts
        // Filters: FUTIDX (index futures) for NIFTY, FUTSTK (stock futures) for HDFCBANK
        private void ParseNearMonthFutures(string masterData)
        {
            if (string.IsNullOrWhiteSpace(masterData)) return;

            // Save raw data for debugging (first 50 lines + all FUTIDX lines)
            try
            {
                var allLines = masterData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var debugLines = allLines.Take(50).ToList();
                
                // Also add all FUTIDX lines for debugging
                var futidxLines = allLines.Where(l => l.Contains("FUTIDX")).Take(20);
                if (futidxLines.Any())
                {
                    debugLines.Add("\n=== FUTIDX Examples ===");
                    debugLines.AddRange(futidxLines);
                }
                
                Directory.CreateDirectory("Data");
                File.WriteAllLines(Path.Combine("Data", "FNO_Master_Debug.txt"), debugLines);
                Console.WriteLine("  Debug: Saved first 50 lines + FUTIDX samples to Data/FNO_Master_Debug.txt");
            }
            catch { }

            var lines = masterData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var futuresLines = new List<(int id, string name, string description, long expiry)>();
            var currentDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Console.WriteLine($"  Parsing {lines.Length} lines from master data...");
            Console.WriteLine($"  Current timestamp: {currentDate}");
            
            // Sample first few lines to understand format
            int sampleCount = 0;
            int hdfc = 0, nifty = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split('|');
                
                // Debug early exit for first 5 lines
                if (sampleCount < 5)
                {
                    Console.WriteLine($"  Line {sampleCount}: {parts.Length} parts, Series[5]='{(parts.Length > 5 ? parts[5] : "N/A")}', Underlying[15]='{(parts.Length > 15 ? parts[15] : "N/A")}'");
                    sampleCount++;
                }
                
                if (parts.Length < 10) continue;

                try
                {
                    // Correct column mapping based on actual data:
                    // [1]: ExchangeInstrumentID
                    // [2]: InstrumentType (1=regular future, 2=option, 4=spread)
                    // [3]: Symbol name (contract symbol)
                    // [5]: Series (OPTSTK, FUTIDX, FUTSTK)
                    // [15]: Underlying symbol (may be empty for spread contracts)
                    // [16]: Expiry datetime string
                    
                    string instrumentType = parts.Length > 2 ? parts[2].Trim() : "";
                    int instrumentId = int.Parse(parts[1].Trim());
                    string symbol = parts.Length > 3 ? parts[3].Trim() : "";
                    string series = parts.Length > 5 ? parts[5].Trim() : "";
                    string underlying = parts.Length > 15 ? parts[15].Trim() : "";
                    string expiryStr = parts.Length > 16 ? parts[16].Trim() : "";
                    string description = parts.Length > 4 ? parts[4].Trim() : "";
                    
                    // Use symbol for matching if underlying is empty
                    string matchName = string.IsNullOrWhiteSpace(underlying) ? symbol : underlying;
                    
                    // Count occurrences of our target symbols/underlyings
                    if (matchName.Contains("HDFCBANK")) hdfc++;
                    if (matchName.Contains("NIFTY") || matchName.Contains("Nifty 50")) nifty++;
                    
                    // Check if it's a regular future (not spread)
                    // InstrumentType 1 = regular future, 2 = option, 4 = spread future
                    // Match: HDFCBANK (exact) or NIFTY/Nifty 50
                    bool isHdfcFuture = instrumentType == "1" &&
                                        (series == "FUTIDX" || series == "FUTSTK") &&
                                        (symbol == "HDFCBANK" || matchName == "HDFCBANK");
                    bool isNiftyFuture = instrumentType == "1" &&
                                         (series == "FUTIDX") &&
                                         (symbol == "NIFTY" || matchName == "Nifty 50");
                    
                    if (isHdfcFuture || isNiftyFuture)
                    {
                        // Parse expiry datetime (format: 2026-02-24T14:30:00)
                        DateTime expiryDate;
                        if (!DateTime.TryParse(expiryStr, out expiryDate))
                        {
                            continue; // Skip if can't parse expiry
                        }
                        
                        long expiryTimestamp = new DateTimeOffset(expiryDate).ToUnixTimeSeconds();
                        
                        // Determine display name
                        string displayUnderlying = isHdfcFuture ? "HDFCBANK" : "NIFTY";
                        
                        Console.WriteLine($"    Found: {displayUnderlying} {series} expiry={expiryDate:yyyy-MM-dd} ID={instrumentId}");

                        // Only include future expiries (allow some buffer for today's contracts)
                        var currentDay = DateTime.UtcNow.Date;
                        var expiryDay = expiryDate.Date;
                        
                        if (expiryDay >= currentDay)
                        {
                            futuresLines.Add((instrumentId, displayUnderlying, description, expiryTimestamp));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip malformed lines silently (too many to log)
                }
            }

            Console.WriteLine($"  Total HDFCBANK mentions: {hdfc}, NIFTY mentions: {nifty}");
            Console.WriteLine($"  Found {futuresLines.Count} active futures contracts");

            // Group by underlying and find nearest expiry for each
            var nearMonthContracts = futuresLines
                .GroupBy(x => x.name)
                .Select(g => g.OrderBy(x => x.expiry).First())
                .ToList();

            // Populate the instruments dictionary
            _fnoInstruments.Clear();
            foreach (var contract in nearMonthContracts)
            {
                var expiryDate = DateTimeOffset.FromUnixTimeSeconds(contract.expiry).ToString("yyyy-MM-dd");
                var key = $"{contract.name}-FUT-{expiryDate}";
                _fnoInstruments[key] = contract.id;
                Console.WriteLine($"  [OK] {contract.name}: {contract.description} (ID: {contract.id}, Expiry: {expiryDate})");
            }
        }
    }
}
