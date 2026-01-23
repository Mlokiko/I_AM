using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.Main;

public partial class SurveyResultsPage : ContentPage
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    private List<TestSession> _testSessions;
    private string _caretakerId = string.Empty;
    private string _caregiverId = string.Empty;

    public SurveyResultsPage()
    {
        InitializeComponent();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
        _authStateService = ServiceHelper.GetService<IAuthenticationStateService>();
        _testSessions = new List<TestSession>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTestSessionsAsync();
    }

    private async Task LoadTestSessionsAsync()
    {
        try
        {
            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.UserId) || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Nie jesteœ zalogowany", "OK");
                return;
            }

            _caretakerId = authState.UserId;

            var caregivers = await _firestoreService.GetCaregiversAsync(authState.UserId, authState.IdToken);
            if (caregivers.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            _caregiverId = caregivers.First().UserId;

            _testSessions = await _firestoreService.GetTestSessionsAsync(_caretakerId, authState.IdToken);

            if (_testSessions.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            ResultsCollectionView.ItemsSource = _testSessions;
            EmptyLabel.IsVisible = false;
            EmptySubLabel.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ wyników: {ex.Message}", "OK");
        }
    }

    private void ShowEmptyState()
    {
        EmptyLabel.IsVisible = true;
        EmptySubLabel.IsVisible = true;
        ResultsCollectionView.ItemsSource = null;
    }

    private async void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TestSession session)
        {
            await Shell.Current.GoToAsync($"surveydetails?sessionId={session.Id}");
        }
    }
}