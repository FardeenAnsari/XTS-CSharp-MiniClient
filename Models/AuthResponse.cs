using Newtonsoft.Json;

namespace XTS_CSharp_MiniClient.Models
{
    // Authentication response structure from login API
    // Response format: { "type": "success", "result": { "token": "...", "userID": "..." } }
    public class AuthResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("result")]
        public AuthResult? Result { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class AuthResult
    {
        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        [JsonProperty("userID")]
        public string UserID { get; set; } = string.Empty;

        [JsonProperty("isInvestorClient")]
        public bool IsInvestorClient { get; set; }
    }
}
