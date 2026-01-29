using System.Net.Http.Json;
using System.Text.Json;

namespace TasksWeb.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;
    private string? _token;

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public string? Token => _token;

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
                    _token = result.Token;
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
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
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
