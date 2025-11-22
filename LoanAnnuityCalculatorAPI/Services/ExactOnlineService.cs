using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class ExactOnlineService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly string _baseAuthUrl = "https://start.exactonline.nl/api/oauth2/auth";
        private readonly string _tokenUrl = "https://start.exactonline.nl/api/oauth2/token";
        private readonly string _apiBaseUrl = "https://start.exactonline.nl/api/v1";

        public ExactOnlineService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
            _clientId = _config["ExactOnline:ClientId"];
            _clientSecret = _config["ExactOnline:ClientSecret"];
            _redirectUri = _config["ExactOnline:RedirectUri"];
        }

        public string GetAuthorizationUrl()
        {
            return $"{_baseAuthUrl}?client_id={_clientId}&redirect_uri={_redirectUri}&response_type=code";
        }

        public async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            var values = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _redirectUri },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            };
            var content = new FormUrlEncodedContent(values);
            var response = await _httpClient.PostAsync(_tokenUrl, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);
            return obj["access_token"]?.ToString();
        }

        public async Task<HttpResponseMessage> PostInvoiceAsync(string accessToken, JObject invoiceData)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/{{division}}/salesinvoices")
            {
                Content = new StringContent(invoiceData.ToString(), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await _httpClient.SendAsync(request);
        }
    }
}
