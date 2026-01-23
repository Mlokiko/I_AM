using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;
using I_AM.Pages.CareGiver;

namespace I_AM.Pages.CareGiver;

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
                
                // ZMIANA: Jeœli by³ wybrany podopieczny, odœwie¿ jego pytania
                if (CareTakerPicker.SelectedItem is UserProfile previouslySelected)
                {
                    await RefreshSelectedCareTakerQuestionsAsync(authState.IdToken);
                }
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

                await LoadQuestionsForCareTakerAsync(selectedCareTaker, authState.IdToken);
            }
            catch (Exception ex)
            {
                await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ pytañ: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// ZMIANA: Nowa metoda do za³adowania pytañ dla wybranego podopiecznego
    /// </summary>
    private async Task LoadQuestionsForCareTakerAsync(UserProfile selectedCareTaker, string idToken)
    {
        var careTakerQuestions = await _firestoreService.GetCareTakerQuestionsAsync(
            selectedCareTaker.Id,
            idToken);

        // Diagnostyka
        System.Diagnostics.Debug.WriteLine($"[EditCareTakerQuestionsPage] CareTaker ID: {selectedCareTaker.Id}");
        System.Diagnostics.Debug.WriteLine($"[EditCareTakerQuestionsPage] CareTakerQuestions result: {(careTakerQuestions != null ? "Not null" : "Null")}");
        if (careTakerQuestions != null)
        {
            System.Diagnostics.Debug.WriteLine($"[EditCareTakerQuestionsPage] Questions count: {careTakerQuestions.Questions?.Count ?? 0}");
        }

        if (careTakerQuestions != null && careTakerQuestions.Questions?.Count > 0)
        {
            _questions = careTakerQuestions.Questions;
            // ZMIANA: Utwórz now¹ listê, aby wymusiæ odœwie¿enie CollectionView
            QuestionsCollectionView.ItemsSource = null;
            QuestionsCollectionView.ItemsSource = _questions;
        }
        else
        {
            _questions = new List<Question>();
            QuestionsCollectionView.ItemsSource = null;
            await DisplayAlert("Informacja", "Ten podopieczny nie ma jeszcze ¿adnych pytañ", "OK");
        }
    }

    /// <summary>
    /// ZMIANA: Nowa metoda do odœwie¿enia pytañ aktualnie wybranego podopiecznego
    /// </summary>
    private async Task RefreshSelectedCareTakerQuestionsAsync(string idToken)
    {
        if (CareTakerPicker.SelectedItem is UserProfile selectedCareTaker)
        {
            await LoadQuestionsForCareTakerAsync(selectedCareTaker, idToken);
        }
    }

    private async void OnAddQuestionClicked(object sender, EventArgs e)
    {
        if (CareTakerPicker.SelectedItem is not UserProfile selectedCareTaker)
        {
            await DisplayAlert("Ostrze¿enie", "Wybierz podopiecznego najpierw", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"addoreditquestion?mode=add&caretakerId={selectedCareTaker.Id}");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"///{nameof(CareGiverMainPage)}");
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync($"///{nameof(CareGiverMainPage)}");
        });
        return true;
    }

    private async Task DeleteQuestionAsync(string questionId)
    {
        try
        {
            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Brak tokenu uwierzytelniaj¹cego", "OK");
                return;
            }

            if (CareTakerPicker.SelectedItem is not UserProfile selectedCareTaker)
            {
                await DisplayAlert("B³¹d", "Brak wybranego podopiecznego", "OK");
                return;
            }

            var success = await _firestoreService.DeleteQuestionFromCaretakerAsync(
                selectedCareTaker.Id,
                questionId,
                authState.IdToken);

            if (success)
            {
                await DisplayAlert("Sukces", "Pytanie zosta³o usuniête", "OK");
                // ZMIANA: Odœwie¿ pytania zamiast pe³nego prze³adowania
                await RefreshSelectedCareTakerQuestionsAsync(authState.IdToken);
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ pytania", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
        }
    }

    private async void OnEditQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Question question)
        {
            // Nawiguj do edycji
            await Shell.Current.GoToAsync($"addoreditquestion?mode=edit&questionId={question.Id}&caretakerId={_careTakerProfiles.FirstOrDefault()?.Id}");
        }
    }

    private async void OnDeleteQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Question question)
        {
            var result = await DisplayAlert("Potwierdzenie", "Czy chcesz usun¹æ to pytanie?", "Tak", "Nie");
            if (result)
            {
                await DeleteQuestionAsync(question.Id);
            }
        }
    }
}