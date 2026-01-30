using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace FilesWeb.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TokenSessionKey = "AuthToken";

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public string? Token => _httpContextAccessor.HttpContext?.Session.GetString(TokenSessionKey);

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new
            {
                username,
                password
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    _httpContextAccessor.HttpContext?.Session.SetString(TokenSessionKey, result.Token);
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);
                    return true;
                }
            }

            _logger.LogWarning("Login failed: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public void Logout()
    {
        _httpContextAccessor.HttpContext?.Session.Remove(TokenSessionKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
