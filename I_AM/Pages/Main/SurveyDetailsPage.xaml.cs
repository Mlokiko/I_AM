using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.Main;

[QueryProperty(nameof(SessionId), "sessionId")]
public partial class SurveyDetailsPage : ContentPage
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    private string _sessionId = string.Empty;

    public SurveyDetailsPage()
    {
        InitializeComponent();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
        _authStateService = ServiceHelper.GetService<IAuthenticationStateService>();
    }

    public string SessionId
    {
        get => _sessionId;
        set
        {
            _sessionId = value;
            _ = LoadSessionDetailsAsync();
        }
    }

    private async Task LoadSessionDetailsAsync()
    {
        try
        {
            // For now, this is a placeholder - you'll need to implement full session retrieval with answers
            await DisplayAlert("Info", "Szczegó³y s¹ ³adowane...", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ szczegó³ów: {ex.Message}", "OK");
        }
    }
}