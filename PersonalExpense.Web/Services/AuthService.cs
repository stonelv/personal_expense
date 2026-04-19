using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PersonalExpense.Web.DTOs;

namespace PersonalExpense.Web.Services;

public interface IAuthService
{
    bool IsAuthenticated { get; }
    string? Token { get; }
    Task<bool> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string email, string password, string userName);
    Task LogoutAsync();
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string Password, string UserName);
public record AuthResponse(string Token, string UserName, string Email);

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _tokenKey = "auth_token";
    private readonly string _userNameKey = "user_name";
    private readonly string _emailKey = "user_email";

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public string? Token { get; private set; }
    public string? UserName { get; private set; }
    public string? Email { get; private set; }

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var request = new LoginRequest(email, password);
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null)
            {
                Token = result.Token;
                UserName = result.UserName;
                Email = result.Email;

                await SecureStorage.SetAsync(_tokenKey, result.Token);
                await SecureStorage.SetAsync(_userNameKey, result.UserName);
                await SecureStorage.SetAsync(_emailKey, result.Email);

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

                return true;
            }
        }

        return false;
    }

    public async Task<bool> RegisterAsync(string email, string password, string userName)
    {
        var request = new RegisterRequest(email, password, userName);
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        return response.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        Token = null;
        UserName = null;
        Email = null;

        SecureStorage.Remove(_tokenKey);
        SecureStorage.Remove(_userNameKey);
        SecureStorage.Remove(_emailKey);

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        var token = await SecureStorage.GetAsync(_tokenKey);
        var userName = await SecureStorage.GetAsync(_userNameKey);
        var email = await SecureStorage.GetAsync(_emailKey);

        if (!string.IsNullOrEmpty(token))
        {
            Token = token;
            UserName = userName;
            Email = email;

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return true;
        }

        return false;
    }
}

public static class SecureStorage
{
    private static readonly Dictionary<string, string> _storage = new();

    public static Task SetAsync(string key, string value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public static Task<string?> GetAsync(string key)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult<string?>(value);
    }

    public static void Remove(string key)
    {
        _storage.Remove(key);
    }
}
