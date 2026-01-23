using System.Collections.ObjectModel;
using System.Windows.Input;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;
using Microsoft.Maui.Controls;

namespace I_AM.Pages.Main;

public class AddOrEditQuestionViewModel : BindableObject
{
    private readonly IFirestoreService _firestoreService;
    private readonly IAuthenticationStateService _authStateService;
    
    private Question _question = new();
    private string _selectedQuestionType = "closed";
    private bool _isClosedQuestionVisible = true;
    private ObservableCollection<QuestionOption> _options = new();

    public Question Question
    {
        get => _question;
        set
        {
            _question = value;
            OnPropertyChanged();
        }
    }

    public string SelectedQuestionType
    {
        get => _selectedQuestionType;
        set
        {
            _selectedQuestionType = value;
            OnPropertyChanged();
            IsClosedQuestionVisible = value == "closed";
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

    public List<string> QuestionTypes => new() { "closed", "open" };

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
        SelectedQuestionType = "closed";
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
    }

    private void OnRemoveOption(QuestionOption option)
    {
        if (option != null && Options.Contains(option))
        {
            Options.Remove(option);
        }
    }

    public void SetQuestion(Question question)
    {
        Question = question;
        SelectedQuestionType = question.Type;
        Options = new ObservableCollection<QuestionOption>(question.Options ?? new List<QuestionOption>());
    }

    public void UpdateOptionsFromUI()
    {
        Question.Options = Options.ToList();
    }
}