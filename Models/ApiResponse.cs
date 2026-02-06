using System;
using Newtonsoft.Json;

namespace XTS_CSharp_MiniClient.Models
{
    // Generic API response wrapper for XTS Market Data API
    public class ApiResponse<T>
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("result")]
        public T? Result { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        public bool IsSuccess => Type?.Equals("success", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
