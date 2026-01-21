using I_AM.Services;
using I_AM.Pages.CareTaker;

namespace I_AM.Pages.Authentication;

public partial class LoginPage : ContentPage
{
    private readonly IAuthenticationService _authService;

    public LoginPage()
    {
        InitializeComponent();
        _authService = ServiceHelper.GetService<IAuthenticationService>();
    }

    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("B³¹d", "Wype³nij wszystkie pola", "OK");
            return;
        }

        var result = await _authService.LoginAsync(email, password);
        
        if (result.Success)
        {
            await DisplayAlert("Sukces", result.Message, "OK");
            
            // Get the current AppShell and navigate based on user role
            if (Shell.Current is AppShell appShell)
            {
                await appShell.NavigateToAppropriatePageAsync();
            }
        }
        else
        {
            await DisplayAlert("B³¹d", result.Message, "OK");
        }
    }

    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}