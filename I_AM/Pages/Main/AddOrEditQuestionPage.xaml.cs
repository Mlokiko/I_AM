using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace I_AM.Pages.Main;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(CareTakerId), "caretakerId")]
[QueryProperty(nameof(QuestionId), "questionId")]
public partial class AddOrEditQuestionPage : ContentPage
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    private string _caregiverId = string.Empty;
    private string _careTakerId = string.Empty;
    private string _mode = "add";
    private string? _questionId;
    private AddOrEditQuestionViewModel _viewModel = new();

    // Query Properties
    public string Mode
    {
        get => _mode;
        set 
        {
            _mode = value ?? "add";
            System.Diagnostics.Debug.WriteLine($"[QueryProperty] Mode set to: {_mode}");
        }
    }

    public string CareTakerId
    {
        get => _careTakerId;
        set 
        {
            _careTakerId = value ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"[QueryProperty] CareTakerId set to: {_careTakerId}");
        }
    }

    public string QuestionId
    {
        get => _questionId ?? string.Empty;
        set 
        {
            _questionId = value ?? null;
            System.Diagnostics.Debug.WriteLine($"[QueryProperty] QuestionId set to: {_questionId}");
        }
    }

    public AddOrEditQuestionPage()
    {
        InitializeComponent();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
        _authStateService = ServiceHelper.GetService<IAuthenticationStateService>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.UserId) || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Nie jesteœ zalogowany", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[InitializeAsync] CareTaker ID: {_careTakerId}");
            System.Diagnostics.Debug.WriteLine($"[InitializeAsync] Mode: {_mode}");

            if (_mode == "edit" && !string.IsNullOrEmpty(_questionId))
            {
                await LoadQuestionAsync(authState.IdToken);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê zainicjalizowaæ: {ex.Message}", "OK");
        }
    }

    private async Task LoadQuestionAsync(string idToken)
    {
        try
        {
            var question = await _firestoreService.GetQuestionAsync(_questionId!, idToken);
            if (question != null)
            {
                _viewModel.SetQuestion(question);
                _careTakerId = question.CaretakerId;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ pytania: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Question.Text))
            {
                await DisplayAlert("B³¹d", "Tekst pytania nie mo¿e byæ pusty", "OK");
                return;
            }

            if (_viewModel.Question == null)
            {
                await DisplayAlert("B³¹d", "Brak danych pytania", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_careTakerId))
            {
                await DisplayAlert("B³¹d", "Brak ID podopiecznego", "OK");
                return;
            }

            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Brak tokenu uwierzytelniaj¹cego", "OK");
                return;
            }

            _viewModel.UpdateOptionsFromUI();
            _viewModel.Question.Type = _viewModel.SelectedQuestionType;
            _viewModel.Question.UpdatedAt = DateTime.UtcNow;

            // DEBUG LOGS
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Question ID: {_viewModel.Question.Id}");
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Question Text: {_viewModel.Question.Text}");
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] CareTaker ID: {_careTakerId}");
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Mode: {_mode}");
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Options count: {_viewModel.Question.Options?.Count ?? 0}");

            bool success;
            if (_mode == "add")
            {
                _viewModel.Question.CaretakerId = _careTakerId;
                System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Saving NEW question...");
                success = await _firestoreService.SaveQuestionAsync(
                    _careTakerId,
                    _viewModel.Question,
                    authState.IdToken);
                System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] SaveQuestionAsync result: {success}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Updating existing question...");
                success = await _firestoreService.UpdateQuestionAsync(
                    _careTakerId,
                    _viewModel.Question,
                    authState.IdToken);
                System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] UpdateQuestionAsync result: {success}");
            }

            if (success)
            {
                await DisplayAlert("Sukces", "Pytanie zosta³o zapisane", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê zapisaæ pytania", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] Exception: {ex.Message}\n{ex.StackTrace}");
            await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditCareTakerQuestionsPage));
    }

    protected override bool OnBackButtonPressed()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync(nameof(EditCareTakerQuestionsPage));
        });
        return true;
    }

    private void OnQuestionTypeChanged(object sender, EventArgs e)
    {
        if (sender is Picker picker)
        {
            _viewModel.SelectedQuestionType = picker.SelectedItem?.ToString() ?? "closed";
        }
    }
}