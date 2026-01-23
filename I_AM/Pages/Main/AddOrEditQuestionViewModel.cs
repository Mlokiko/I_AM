using System.Collections.ObjectModel;
using System.Windows.Input;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Helpers;
using I_AM.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace I_AM.Pages.Main;

public class AddOrEditQuestionViewModel : BindableObject
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    
    private Question _question = new();
    private string _selectedQuestionTypeDisplay = "Zamkniête";
    private bool _isClosedQuestionVisible = true;
    private ObservableCollection<QuestionOption> _options = new();
    private Dictionary<QuestionOption, string> _optionErrors = new();

    public Question Question
    {
        get => _question;
        set
        {
            _question = value;
            OnPropertyChanged();
        }
    }

    public string SelectedQuestionTypeDisplay
    {
        get => _selectedQuestionTypeDisplay;
        set
        {
            _selectedQuestionTypeDisplay = value;
            OnPropertyChanged();
            IsClosedQuestionVisible = value == "Zamkniête";
        }
    }

    public bool IsClosedQuestionVisible
    {
        get => _isClosedQuestionVisible;
        set
        {
            _isClosedQuestionVisible = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<QuestionOption> Options
    {
        get => _options;
        set
        {
            _options = value;
            OnPropertyChanged();
        }
    }

    public List<string> QuestionTypes => new() { "Zamkniête", "Otwarte" };

    public ICommand AddOptionCommand { get; }
    public ICommand RemoveOptionCommand { get; }

    public AddOrEditQuestionViewModel()
    {
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
        _authStateService = ServiceHelper.GetService<IAuthenticationStateService>();

        AddOptionCommand = new Command(OnAddOption);
        RemoveOptionCommand = new Command<QuestionOption>(OnRemoveOption);

        InitializeQuestion();
    }

    private void InitializeQuestion()
    {
        Question = new Question
        {
            Id = Guid.NewGuid().ToString(),
            Type = "closed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Options = new List<QuestionOption>()
        };
        Options = new ObservableCollection<QuestionOption>();
        SelectedQuestionTypeDisplay = "Zamkniête";
    }

    private void OnAddOption()
    {
        var newOption = new QuestionOption
        {
            Text = string.Empty,
            Points = 0,
            Order = Options.Count
        };
        Options.Add(newOption);
        _optionErrors[newOption] = string.Empty;
    }

    private void OnRemoveOption(QuestionOption option)
    {
        if (option != null && Options.Contains(option))
        {
            Options.Remove(option);
            _optionErrors.Remove(option);
        }
    }

    public void SetQuestion(Question question)
    {
        Question = question;
        SelectedQuestionTypeDisplay = question.Type == "closed" ? "Zamkniête" : "Otwarte";
        Options = new ObservableCollection<QuestionOption>(question.Options ?? new List<QuestionOption>());
        
        // Zainicjalizuj s³ownik b³êdów
        _optionErrors.Clear();
        foreach (var option in Options)
        {
            _optionErrors[option] = string.Empty;
        }
    }

    /// <summary>
    /// Validates and converts options points to proper format
    /// Accepts formats: x, x.x, x.xx, x,x, x,xx
    /// Returns false only if format contains invalid characters
    /// </summary>
    public bool ValidateAndConvertOptions(out string errorMessage)
    {
        errorMessage = string.Empty;
        _optionErrors.Clear();

        if (SelectedQuestionTypeDisplay == "Otwarte")
        {
            return true;
        }

        // Walidacja dla pytañ zamkniêtych
        foreach (var option in Options)
        {
            if (string.IsNullOrWhiteSpace(option.Text))
            {
                _optionErrors[option] = "Tekst opcji nie mo¿e byæ pusty";
                errorMessage = "Wszystkie opcje musz¹ mieæ tekst";
                return false;
            }

            // Konwersja i walidacja punktacji
            var pointsString = option.Points.ToString();
            if (!FirestoreValueExtractor.TryConvertPoints(pointsString, out var convertedPoints, out var displayFormat))
            {
                _optionErrors[option] = "Format punktacji musi byæ liczb¹ (x, x.xx, x,xx)";
                errorMessage = "Format punktacji zawiera niedozwolone znaki. U¿yj formatu: x, x.x, x.xx, x,x, x,xx";
                return false;
            }

            // Przeupdatnij wartoœæ na skonwertowan¹
            option.Points = convertedPoints;

            if (convertedPoints < 0)
            {
                _optionErrors[option] = "Punkty nie mog¹ byæ ujemne";
                errorMessage = "Punkty nie mog¹ byæ ujemne";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Legacy method - kept for compatibility, now calls ValidateAndConvertOptions
    /// </summary>
    public bool ValidateOptions(out string errorMessage)
    {
        return ValidateAndConvertOptions(out errorMessage);
    }

    public void UpdateOptionsFromUI()
    {
        Question.Options = Options.ToList();
        Question.Type = SelectedQuestionTypeDisplay == "Zamkniête" ? "closed" : "open";
    }

    public Dictionary<QuestionOption, string> GetOptionErrors()
    {
        return _optionErrors;
    }
}