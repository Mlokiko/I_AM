using I_AM.Services;

namespace I_AM;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;
    public List<string> SexOptions { get; set; }

    public RegisterPage()
    {
        InitializeComponent();
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();

        SexOptions = new List<string>
        {
            "Mê¿czyzna",
            "Kobieta",
            "Inne",
            "Nie chcê podawaæ"
        };
        this.BindingContext = this;
    }

    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        if (!(sender is Button button)) return;
        
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var firstName = FirstNameEntry.Text?.Trim() ?? string.Empty;
        var lastName = LastNameEntry.Text?.Trim() ?? string.Empty;
        var ageText = AgeEntry.Text?.Trim() ?? string.Empty;
        var sex = SexPicker.SelectedItem?.ToString() ?? string.Empty;

        // Walidacja
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("B³¹d", "Email i has³o s¹ wymagane", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            await DisplayAlert("B³¹d", "Imiê i nazwisko s¹ wymagane", "OK");
            return;
        }

        if (!int.TryParse(ageText, out var age) || age < 13)
        {
            await DisplayAlert("B³¹d", "Podaj prawid³owy wiek (minimum 13 lat)", "OK");
            return;
        }

        button.IsEnabled = false;

        var result = await _authService.RegisterAsync(email, password);
        
        if (result.Success)
        {
            // Zapisz profil u¿ytkownika do Firestore
            var userProfile = new UserProfile
            {
                FirstName = firstName,
                LastName = lastName,
                Age = age,
                Sex = sex,
                Email = email
            };

            var saveSuccess = await _firestoreService.SaveUserProfileAsync(
                result.UserId!,
                userProfile,
                result.IdToken!
            );

            if (saveSuccess)
            {
                await DisplayAlert("Sukces", "Rejestracja powiod³a siê! Twój profil zosta³ zapisany.", "OK");
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            else
            {
                await DisplayAlert("Ostrze¿enie", "Rejestracja powiod³a siê, ale nie uda³o siê zapisaæ profilu. Spróbuj póŸniej.", "OK");
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
        }
        else
        {
            await DisplayAlert("B³¹d", result.Message, "OK");
        }

        button.IsEnabled = true;
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}