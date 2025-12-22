using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class ExactOnlineService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly LoanDbContext _context;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly string _baseAuthUrl = "https://start.exactonline.nl/api/oauth2/auth";
        private readonly string _tokenUrl = "https://start.exactonline.nl/api/oauth2/token";
        private readonly string _apiBaseUrl = "https://start.exactonline.nl/api/v1";

        public ExactOnlineService(IConfiguration config, HttpClient httpClient, LoanDbContext context)
        {
            _config = config;
            _httpClient = httpClient;
            _context = context;
            _clientId = _config["ExactOnline:ClientId"];
            _clientSecret = _config["ExactOnline:ClientSecret"];
            _redirectUri = _config["ExactOnline:RedirectUri"];
        }

        public string GetAuthorizationUrl()
        {
            return $"{_baseAuthUrl}?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code";
        }

        public async Task<ExactOnlineToken> ExchangeCodeForTokenAsync(string code, string tenantId)
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
            var tokenData = JObject.Parse(json);
            
            var accessToken = tokenData["access_token"]?.ToString();
            var refreshToken = tokenData["refresh_token"]?.ToString();
            var expiresIn = tokenData["expires_in"]?.ToObject<int>() ?? 600;
            
            // Get the division from the current user endpoint
            var division = await GetCurrentDivisionAsync(accessToken);
            
            // Deactivate any existing tokens for this tenant
            var existingTokens = await _context.ExactOnlineTokens
                .Where(t => t.TenantId == tenantId && t.IsActive)
                .ToListAsync();
            
            foreach (var token in existingTokens)
            {
                token.IsActive = false;
            }
            
            // Create new token entry
            var newToken = new ExactOnlineToken
            {
                TenantId = tenantId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                Division = division,
                IsActive = true
            };
            
            _context.ExactOnlineTokens.Add(newToken);
            await _context.SaveChangesAsync();
            
            return newToken;
        }

        public async Task<string> GetValidAccessTokenAsync(string tenantId)
        {
            var token = await _context.ExactOnlineTokens
                .Where(t => t.TenantId == tenantId && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (token == null)
            {
                throw new InvalidOperationException("No Exact Online token found. Please authorize first.");
            }
            
            // If token is expired, refresh it
            if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            {
                await RefreshTokenAsync(token);
            }
            
            return token.AccessToken;
        }

        private async Task RefreshTokenAsync(ExactOnlineToken token)
        {
            var values = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", token.RefreshToken },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            };
            
            var content = new FormUrlEncodedContent(values);
            var response = await _httpClient.PostAsync(_tokenUrl, content);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JObject.Parse(json);
            
            token.AccessToken = tokenData["access_token"]?.ToString();
            token.RefreshToken = tokenData["refresh_token"]?.ToString();
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData["expires_in"]?.ToObject<int>() ?? 600);
            token.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
        }

        private async Task<int> GetCurrentDivisionAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/current/Me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);
            
            return data["d"]?["results"]?[0]?["CurrentDivision"]?.ToObject<int>() ?? 0;
        }

        public async Task<HttpResponseMessage> PostInvoiceAsync(string tenantId, JObject invoiceData)
        {
            var token = await _context.ExactOnlineTokens
                .Where(t => t.TenantId == tenantId && t.IsActive)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (token == null)
            {
                throw new InvalidOperationException("No Exact Online token found. Please authorize first.");
            }
            
            var accessToken = await GetValidAccessTokenAsync(tenantId);
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/{token.Division}/salesinvoice/SalesInvoices")
            {
                Content = new StringContent(invoiceData.ToString(), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return await _httpClient.SendAsync(request);
        }
    }
}
