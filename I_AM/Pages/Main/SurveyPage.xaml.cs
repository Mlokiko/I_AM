using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.Main;

public partial class SurveyPage : ContentPage
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    private List<Question> _questions;
    private Dictionary<string, QuestionAnswer> _answers;
    private string _caretakerId = string.Empty;
    private string _caregiverId = string.Empty;

    public SurveyPage()
    {
        InitializeComponent();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
        _authStateService = ServiceHelper.GetService<IAuthenticationStateService>();
        _questions = new List<Question>();
        _answers = new Dictionary<string, QuestionAnswer>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadQuestionsAsync();
    }

    private async Task LoadQuestionsAsync()
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
            var idToken = authState.IdToken;

            // Get the first caregiver (for now - can be changed to select one)
            var caregivers = await _firestoreService.GetCaregiversAsync(_caretakerId, idToken);
            if (caregivers.Count == 0)
            {
                await DisplayAlert("Brak opiekunów", "Nie masz przypisanych opiekunów", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            _caregiverId = caregivers.First().UserId;

            _questions = await _firestoreService.GetCaregiverQuestionsAsync(_caretakerId, idToken);

            if (_questions.Count == 0)
            {
                await DisplayAlert("Brak pytañ", "Opiekun nie przygotowa³ jeszcze pytañ dla Ciebie", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            UpdateProgressLabel();
            QuestionsCollectionView.ItemsSource = _questions.Select(q => new QuestionViewModel(q)).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê za³adowaæ pytañ: {ex.Message}", "OK");
        }
    }

    private void UpdateProgressLabel()
    {
        var answered = _answers.Count;
        var total = _questions.Count;
        ProgressLabel.Text = $"Pytanie {answered} z {total}";
    }

    private async void OnSelectOptionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is QuestionOption option)
        {
            // Find the current question
            var currentQuestion = _questions.FirstOrDefault();
            if (currentQuestion != null)
            {
                var answer = new QuestionAnswer
                {
                    QuestionId = currentQuestion.Id,
                    SelectedOption = option.Text,
                    SelectedOptionPoints = option.Points,
                    AnsweredAt = DateTime.UtcNow
                };

                _answers[currentQuestion.Id] = answer;
                UpdateProgressLabel();
            }
        }
    }

    private async void OnSubmitAnswersClicked(object sender, EventArgs e)
    {
        try
        {
            if (_answers.Count != _questions.Count)
            {
                await DisplayAlert("Niekompletne", "Proszê odpowiedzieæ na wszystkie pytania", "OK");
                return;
            }

            var authState = await _authStateService.LoadAuthenticationStateAsync();
            if (authState == null || string.IsNullOrEmpty(authState.IdToken))
            {
                await DisplayAlert("B³¹d", "Brak tokenu uwierzytelniaj¹cego", "OK");
                return;
            }
            var idToken = authState.IdToken;

            var totalPoints = _answers.Values.Sum(a => a.SelectedOptionPoints);
            var maxPoints = _questions.Sum(q => q.Options.Max(o => o.Points));
            var percentageScore = (totalPoints / maxPoints) * 100;

            var session = new TestSession
            {
                CaretakerId = _caretakerId,
                TotalPoints = totalPoints,
                MaxPoints = maxPoints,
                PercentageScore = percentageScore,
                CompletedAt = DateTime.UtcNow,
                Answers = _answers.Values.ToList()
            };

            foreach (var answer in _answers.Values)
            {
                await _firestoreService.SaveAnswerAsync(_caretakerId, answer, idToken);
            }

            await _firestoreService.SaveTestSessionAsync(_caretakerId, session, idToken);

            await DisplayAlert("Sukces", "Twoje odpowiedzi zosta³y zapisane", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Nie uda³o siê wys³aæ odpowiedzi: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
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