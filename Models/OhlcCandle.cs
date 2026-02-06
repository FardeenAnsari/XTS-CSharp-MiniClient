using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace XTS_CSharp_MiniClient.Models
{
    // Represents a single OHLC candlestick bar with timestamp and trading data
    public class OhlcCandle
    {
        [JsonProperty("Timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("Open")]
        public decimal Open { get; set; }

        [JsonProperty("High")]
        public decimal High { get; set; }

        [JsonProperty("Low")]
        public decimal Low { get; set; }

        [JsonProperty("Close")]
        public decimal Close { get; set; }

        [JsonProperty("Volume")]
        public long Volume { get; set; }

        [JsonProperty("OpenInterest")]
        public long OpenInterest { get; set; }

        public DateTime GetDateTime()
        {
            // Convert Unix timestamp to DateTime
            return DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;
        }

        public override string ToString()
        {
            return $"{GetDateTime():yyyy-MM-dd HH:mm:ss} | O:{Open} H:{High} L:{Low} C:{Close} V:{Volume}";
        }
    }

    // Response structure for OHLC API requests
    public class OhlcResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("result")]
        public OhlcResult? Result { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class OhlcResult
    {
        // API returns pipe-delimited string: "timestamp|open|high|low|close|volume|oi|,..."
        [JsonProperty("dataReponse")]
        public string? DataResponse { get; set; }

        [JsonProperty("listQuotes")]
        public string? ListQuotes { get; set; }
    }
}
