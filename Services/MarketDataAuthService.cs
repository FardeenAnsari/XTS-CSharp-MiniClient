using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XTS_CSharp_MiniClient.Models;

namespace XTS_CSharp_MiniClient.Services
{
    // Assignment Requirement: Market data login
    // Python Reference: xts_connect_async.py - marketdata_login() method
    // 
    // Handles authentication with XTS Market Data API
    // Returns JWT token used for all subsequent API requests
    public class MarketDataAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _source;
        private readonly string _rootUrl;

        public string? Token { get; private set; }
        public string? UserID { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public MarketDataAuthService(string apiKey, string secretKey, string source, string rootUrl)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;
            _source = source;
            _rootUrl = rootUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(rootUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Login to XTS Market Data API
        // Python package: marketdata_login() - POST /apimarketdata/auth/login
        public async Task<bool> LoginAsync()
        {
            try
            {
                // Python: params = { "appKey": self.apiKey, "secretKey": self.secretKey, "source": self.source }
                var loginPayload = new
                {
                    appKey = _apiKey,
                    secretKey = _secretKey,
                    source = _source
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(loginPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                // Python route: "market.login": "/apimarketdata/auth/login"
                var response = await _httpClient.PostAsync("/apimarketdata/auth/login", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Login failed with status: {response.StatusCode}");
                    return false;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseBody);

                if (authResponse?.Type == "success" && authResponse.Result != null)
                {
                    // Python: self._set_common_variables(response['result']['token'], response['result']['userID'], False)
                    Token = authResponse.Result.Token;
                    UserID = authResponse.Result.UserID;
                    
                    Console.WriteLine("[OK] Market Data Login Successful");
                    Console.WriteLine($"     Token: {Token[..20]}...");
                    Console.WriteLine($"     UserID: {UserID}");
                    
                    return true;
                }
                else
                {
                    Console.WriteLine($"Login failed: {authResponse?.Description}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login exception: {ex.Message}");
                return false;
            }
        }

        // Logout and invalidate token
        // Python package: marketdata_logout() - DELETE /apimarketdata/auth/logout
        public async Task<bool> LogoutAsync()
        {
            if (!IsAuthenticated) return true;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, "/apimarketdata/auth/logout");
                request.Headers.Add("Authorization", Token);

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("[OK] Market Data Logout Successful");
                    Token = null;
                    UserID = null;
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout exception: {ex.Message}");
                return false;
            }
        }
    }
}
