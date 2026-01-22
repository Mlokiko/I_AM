using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.Main;

public partial class EditCareTakerQuestionsPage : ContentPage
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    private string _caregiverId = string.Empty;
    private List<UserProfile> _careTakerProfiles = new();
    private List<Question> _questions = new();

    public EditCareTakerQuestionsPage()
    {
        InitializeComponent();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
        _authStateService = ServiceHelper.GetService<IAuthenticationStateService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCareTakersAsync();
    }

    private async Task LoadCareTakersAsync()
    {
        try
        {
            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.UserId) || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Nie jesteœ zalogowany", "OK");
                return;
            }

            _caregiverId = authState.UserId;
            var idToken = authState.IdToken;

            // Get all care takers for this caregiver
            var userProfile = await _firestoreService.GetUserProfileAsync(_caregiverId, idToken);
            if (userProfile != null && userProfile.CaretakersID.Count > 0)
            {
                _careTakerProfiles.Clear();
                
                // Load full profiles for each caretaker
                foreach (var careTakerId in userProfile.CaretakersID)
                {
                    try
                    {
                        var careTakerProfile = await _firestoreService.GetUserProfileAsync(careTakerId, idToken);
                        if (careTakerProfile != null)
                        {
                            _careTakerProfiles.Add(careTakerProfile);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EditCareTakerQuestionsPage] Error loading caretaker profile: {ex.Message}");
                    }
                }

                // Bind the full profiles to the picker, displaying the name
                CareTakerPicker.ItemsSource = _careTakerProfiles;
                CareTakerPicker.ItemDisplayBinding = new Binding("FirstName");
            }
            else
            {
                await DisplayAlert("Informacja", "Nie masz przypisanych podopiecznych", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ podopiecznych: {ex.Message}", "OK");
        }
    }

    private async void OnCareTakerSelected(object sender, EventArgs e)
    {
        if (CareTakerPicker.SelectedItem is UserProfile selectedCareTaker)
        {
            try
            {
                var authState = await _authStateService.LoadAuthenticationStateAsync();
                if (authState == null || string.IsNullOrEmpty(authState.IdToken))
                {
                    await DisplayAlert("B³¹d", "Brak tokenu uwierzytelniaj¹cego", "OK");
                    return;
                }

                _questions = await _firestoreService.GetCaregiverQuestionsAsync(selectedCareTaker.Email, _caregiverId, authState.IdToken);
                QuestionsCollectionView.ItemsSource = _questions.Select(q => new QuestionViewModel(q)).ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ pytañ: {ex.Message}", "OK");
            }
        }
    }

    private async void OnAddQuestionClicked(object sender, EventArgs e)
    {
        if (CareTakerPicker.SelectedItem is not UserProfile selectedCareTaker)
        {
            await DisplayAlert("Ostrze¿enie", "Wybierz podopiecznego najpierw", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"addoreditquestion?mode=add&caretakerId={selectedCareTaker.Email}&caregiverId={_caregiverId}");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Helper class for view model binding
    private class QuestionViewModel
    {
        private readonly Question _question;

        public QuestionViewModel(Question question)
        {
            _question = question;
        }

        public string Id => _question.Id;
        public string Text => _question.Text;
        public string Description => _question.Description;
        public string Type => _question.Type;
        public List<QuestionOption> Options => _question.Options;
        public bool IsClosedQuestion => _question.Type == "closed";
        public bool IsOpenQuestion => _question.Type == "open";
    }
}