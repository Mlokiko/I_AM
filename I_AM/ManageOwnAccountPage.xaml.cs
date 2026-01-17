using I_AM.Services;

namespace I_AM;

public partial class ManageOwnAccountPage : ContentPage
{
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;

    public ManageOwnAccountPage()
    {
        InitializeComponent();
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserProfileAsync();
    }

    private async Task LoadUserProfileAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;

            // Pobierz bie¿¹ce dane u¿ytkownika
            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();
            var email = await _authService.GetCurrentEmailAsync();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                ShowError("B³¹d: Nie mo¿na za³adowaæ danych u¿ytkownika");
                return;
            }

            // Pobierz profil z Firestore
            var profile = await _firestoreService.GetUserProfileAsync(userId, idToken);

            if (profile != null)
            {
                // Wyœwietl dane
                EmailLabel.Text = profile.Email ?? email ?? "N/A";
                FirstNameLabel.Text = profile.FirstName ?? "N/A";
                LastNameLabel.Text = profile.LastName ?? "N/A";
                AgeLabel.Text = profile.Age > 0 ? profile.Age.ToString() : "N/A";
                SexLabel.Text = profile.Sex ?? "N/A";
                PhoneNumberLabel.Text = profile.PhoneNumber ?? "N/A";
                CreatedAtLabel.Text = profile.CreatedAt != DateTime.MinValue 
                    ? profile.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                    : "N/A";
                UserTypeLabel.Text = profile.IsCaregiver ? "Opiekun" : "Podopieczny";
            }
            else
            {
                // Profil nie znaleziony, wyœwietl tylko email
                EmailLabel.Text = email ?? "N/A";
                FirstNameLabel.Text = "N/A";
                LastNameLabel.Text = "N/A";
                AgeLabel.Text = "N/A";
                SexLabel.Text = "N/A";
                PhoneNumberLabel.Text = "N/A";
                CreatedAtLabel.Text = "N/A";
                UserTypeLabel.Text = "N/A";
            }
        }
        catch (Exception ex)
        {
            ShowError($"B³¹d podczas ³adowania profilu: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Info", "Funkcja edycji profilu bêdzie dostêpna wkrótce", "OK");
    }

    private async void OnDeleteAccountClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert(
            "PotwierdŸ",
            "Czy na pewno chcesz usun¹æ swoje konto? Ta akcja jest nieodwracalna.",
            "Tak",
            "Nie"
        );

        if (result)
        {
            try
            {
                DeleteAccountButton.IsEnabled = false;

                var userId = await _authService.GetCurrentUserIdAsync();
                var idToken = await _authService.GetCurrentIdTokenAsync();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
                {
                    await DisplayAlert("B³¹d", "Brak wymaganych danych autentykacji", "OK");
                    return;
                }

                // Poka¿ loading indicator
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                // 1. Usuñ profil z Firestore
                System.Diagnostics.Debug.WriteLine($"Usuwanie profilu z Firestore dla userId: {userId}");
                var firestoreSuccess = await _firestoreService.DeleteUserProfileAsync(userId, idToken);
                System.Diagnostics.Debug.WriteLine($"? Firestore delete: {firestoreSuccess}");
                
                if (!firestoreSuccess)
                {
                    // Jeœli usuwanie z Firestore siê nie powiedzie, pytaj czy mimo to usun¹æ konto
                    var continueDelete = await DisplayAlert(
                        "Ostrze¿enie",
                        "Nie uda³o siê usun¹æ profilu z bazy danych. Czy mimo to chcesz usun¹æ konto?",
                        "Tak",
                        "Nie"
                    );

                    if (!continueDelete)
                    {
                        return;
                    }
                }

                // 2. Usuñ konto z Firebase Authentication
                System.Diagnostics.Debug.WriteLine("Usuwanie konta z Firebase Authentication");
                var authSuccess = await _authService.DeleteAccountAsync();
                System.Diagnostics.Debug.WriteLine($"? Auth delete: {authSuccess}");
                
                if (!authSuccess)
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ konta z Firebase Authentication", "OK");
                    return;
                }

                // 3. Wyloguj u¿ytkownika (wyczyœæ lokalny stan)
                System.Diagnostics.Debug.WriteLine("Wylogowywanie u¿ytkownika");
                await _authService.LogoutAsync();
                
                await DisplayAlert("Sukces", "Konto i wszystkie powi¹zane dane zosta³y permanentnie usuniête", "OK");
                
                // 4. Nawiguj do LandingPage
                await Shell.Current.GoToAsync($"//{nameof(LandingPage)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Exception podczas usuwania konta: {ex}");
                await DisplayAlert("B³¹d", $"B³¹d usuwania konta: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                DeleteAccountButton.IsEnabled = true;
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
