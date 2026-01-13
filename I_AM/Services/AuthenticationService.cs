using System.Text.Json;
using System.Text.Json.Serialization;

namespace I_AM.Services;

public interface IAuthenticationService
{
    Task<AuthResult> RegisterAsync(string email, string password);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<bool> LogoutAsync();
    Task<string?> GetCurrentUserIdAsync();
    Task<string?> GetCurrentIdTokenAsync();
    Task<string?> GetCurrentEmailAsync();
    Task InitializeAsync();
    bool IsUserAuthenticated { get; }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string? IdToken { get; set; }
}

public class AuthenticationService : IAuthenticationService
{
    private string? _currentUserId;
    private string? _idToken;
    private string? _currentEmail;
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationStateService _stateService;

    public bool IsUserAuthenticated => !string.IsNullOrEmpty(_currentUserId);

    public AuthenticationService()
    {
        _httpClient = new HttpClient();
        _stateService = ServiceHelper.GetService<IAuthenticationStateService>();
    }

    /// <summary>
    /// Inicjalizuje serwis - ³aduje zapisany stan autentykacji
    /// </summary>
    public async Task InitializeAsync()
    {
        var state = await _stateService.LoadAuthenticationStateAsync();
        if (state != null)
        {
            _currentUserId = state.UserId;
            _idToken = state.IdToken;
            _currentEmail = state.Email;
            System.Diagnostics.Debug.WriteLine($"? Autentykacja przywrócona dla: {_currentEmail}");
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult 
                { 
                    Success = false, 
                    Message = "Email i has³o nie mog¹ byæ puste" 
                };
            }

            if (password.Length < 6)
            {
                return new AuthResult 
                { 
                    Success = false, 
                    Message = "Has³o musi mieæ co najmniej 6 znaków" 
                };
            }

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseConfig.WebApiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("localId", out var localId) && 
                    root.TryGetProperty("idToken", out var idToken))
                {
                    _currentUserId = localId.GetString();
                    _idToken = idToken.GetString();
                    _currentEmail = email;

                    // Zapisz stan autentykacji
                    await _stateService.SaveAuthenticationStateAsync(_currentUserId, _idToken, email);

                    return new AuthResult
                    {
                        Success = true,
                        Message = "Rejestracja powiod³a siê",
                        UserId = _currentUserId,
                        IdToken = _idToken
                    };
                }
            }

            // Parsuj b³¹d Firebase
            var errorMessage = "B³¹d rejestracji";
            try
            {
                var errorJson = JsonDocument.Parse(responseBody);
                if (errorJson.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("message", out var message))
                {
                    errorMessage = message.GetString() ?? "B³¹d rejestracji";
                }
            }
            catch { }

            return new AuthResult
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = $"B³¹d rejestracji: {ex.Message}"
            };
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult 
                { 
                    Success = false, 
                    Message = "Email i has³o nie mog¹ byæ puste" 
                };
            }

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseConfig.WebApiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("localId", out var localId) && 
                    root.TryGetProperty("idToken", out var idToken))
                {
                    _currentUserId = localId.GetString();
                    _idToken = idToken.GetString();
                    _currentEmail = email;

                    // Zapisz stan autentykacji
                    await _stateService.SaveAuthenticationStateAsync(_currentUserId, _idToken, email);

                    return new AuthResult
                    {
                        Success = true,
                        Message = "Logowanie powiod³o siê",
                        UserId = _currentUserId,
                        IdToken = _idToken
                    };
                }
            }

            // Parsuj b³¹d Firebase
            var errorMessage = "B³¹d logowania";
            try
            {
                var errorJson = JsonDocument.Parse(responseBody);
                if (errorJson.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("message", out var message))
                {
                    var errorMsg = message.GetString() ?? "";
                    errorMessage = errorMsg switch
                    {
                        "INVALID_LOGIN_CREDENTIALS" => "Niepoprawny email lub has³o",
                        "USER_DISABLED" => "Konto zosta³o wy³¹czone",
                        _ => errorMsg
                    };
                }
            }
            catch { }

            return new AuthResult
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = $"B³¹d logowania: {ex.Message}"
            };
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            _currentUserId = null;
            _idToken = null;
            _currentEmail = null;
            
            // Wyczyœæ stan autentykacji
            await _stateService.ClearAuthenticationStateAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        return await Task.FromResult(_currentUserId);
    }

    public async Task<string?> GetCurrentIdTokenAsync()
    {
        return await Task.FromResult(_idToken);
    }

    public async Task<string?> GetCurrentEmailAsync()
    {
        return await Task.FromResult(_currentEmail);
    }
}