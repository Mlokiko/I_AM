namespace I_AM.Services;

/// <summary>
/// Serwis zarz¹dzaj¹cy stanem autentykacji i persystencj¹ danych logowania
/// </summary>
public interface IAuthenticationStateService
{
    Task SaveAuthenticationStateAsync(string userId, string idToken, string email);
    Task<AuthenticationState?> LoadAuthenticationStateAsync();
    Task ClearAuthenticationStateAsync();
    bool HasValidAuthenticationState { get; }
}

public class AuthenticationState
{
    public string UserId { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AuthenticationStateService : IAuthenticationStateService
{
    private const string UserIdKey = "auth_user_id";
    private const string IdTokenKey = "auth_id_token";
    private const string EmailKey = "auth_email";

    private AuthenticationState? _cachedState;

    public bool HasValidAuthenticationState => _cachedState != null;

    public async Task SaveAuthenticationStateAsync(string userId, string idToken, string email)
    {
        try
        {
            // Zapisz do Secure Storage
            await SecureStorage.SetAsync(UserIdKey, userId);
            await SecureStorage.SetAsync(IdTokenKey, idToken);
            await SecureStorage.SetAsync(EmailKey, email);

            // Zaktualizuj cache
            _cachedState = new AuthenticationState
            {
                UserId = userId,
                IdToken = idToken,
                Email = email
            };

            System.Diagnostics.Debug.WriteLine("? Stan autentykacji zapisany");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? B³¹d zapisywania stanu autentykacji: {ex.Message}");
        }
    }

    public async Task<AuthenticationState?> LoadAuthenticationStateAsync()
    {
        try
        {
            // Jeœli mamy cache, zwróæ go
            if (_cachedState != null)
            {
                return _cachedState;
            }

            // Spróbuj za³adowaæ z Secure Storage
            var userId = await SecureStorage.GetAsync(UserIdKey);
            var idToken = await SecureStorage.GetAsync(IdTokenKey);
            var email = await SecureStorage.GetAsync(EmailKey);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            _cachedState = new AuthenticationState
            {
                UserId = userId,
                IdToken = idToken,
                Email = email ?? string.Empty
            };

            System.Diagnostics.Debug.WriteLine($"? Stan autentykacji za³adowany dla: {email}");
            return _cachedState;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? B³¹d ³adowania stanu autentykacji: {ex.Message}");
            return null;
        }
    }

    public async Task ClearAuthenticationStateAsync()
    {
        try
        {
            SecureStorage.Remove(UserIdKey);
            SecureStorage.Remove(IdTokenKey);
            SecureStorage.Remove(EmailKey);

            _cachedState = null;

            System.Diagnostics.Debug.WriteLine("? Stan autentykacji wyczyszczony");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? B³¹d czyszczenia stanu autentykacji: {ex.Message}");
        }
    }
}
