using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace I_AM.Pages.Main;

public partial class AddOrEditQuestionPage : ContentPage
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    private string _caregiverId = string.Empty;
    private string _careTakerId = string.Empty;
    private string _mode = "add";
    private string? _questionId;
    private AddOrEditQuestionViewModel _viewModel = new();

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

            _caregiverId = authState.UserId;

            // Get query parameters
            var queryDict = this.GetNavigationParameters();
            if (queryDict.ContainsKey("mode"))
                _mode = queryDict["mode"].ToString() ?? "add";

            if (queryDict.ContainsKey("caretakerId"))
                _careTakerId = queryDict["caretakerId"].ToString() ?? string.Empty;

            if (queryDict.ContainsKey("questionId"))
                _questionId = queryDict["questionId"].ToString();

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

            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Brak tokenu uwierzytelniaj¹cego", "OK");
                return;
            }

            _viewModel.UpdateOptionsFromUI();
            _viewModel.Question.Type = _viewModel.SelectedQuestionType;
            _viewModel.Question.UpdatedAt = DateTime.UtcNow;

            bool success;
            if (_mode == "add")
            {
                _viewModel.Question.CaretakerId = _careTakerId;
                _viewModel.Question.CaregiverId = _caregiverId;
                success = await _firestoreService.SaveQuestionAsync(_careTakerId, _caregiverId, _viewModel.Question, authState.IdToken);
            }
            else
            {
                success = await _firestoreService.UpdateQuestionAsync(_careTakerId, _caregiverId, _viewModel.Question, authState.IdToken);
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
            await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private IDictionary<string, object> GetNavigationParameters()
    {
        if (Shell.Current?.CurrentItem?.CurrentItem is ShellSection section &&
            section.CurrentItem is ShellContent content &&
            content.BindingContext is IDictionary<string, object> parameters)
        {
            return parameters;
        }
        return new Dictionary<string, object>();
    }

    private void OnQuestionTypeChanged(object sender, EventArgs e)
    {
        // Tutaj mo¿na dodaæ logikê obs³ugi zmiany typu pytania
    }
}