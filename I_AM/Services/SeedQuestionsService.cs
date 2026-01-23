using I_AM.Models;
using I_AM.Services.Interfaces;

namespace I_AM.Services;

/// <summary>
/// Service to initialize default questions for caretakers
/// </summary>
public class SeedQuestionsService
{
    private readonly IFirestoreService _firestoreService;

    public SeedQuestionsService(IFirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
    }

    /// <summary>
    /// Gets the list of default questions
    /// </summary>
    public static List<Question> GetDefaultQuestions(string caretakerId, string caregiverId)
    {
        return new List<Question>
        {
            new Question
            {
                Id = Guid.NewGuid().ToString(),
                CaretakerId = caretakerId,
                Text = "Jak siÍ dzisiaj czujesz?",
                Description = "OceÒ swoje samopoczucie na dzisiejszy dzieÒ",
                Type = "closed",
                Order = 1,
                IsActive = true,
                Options = new List<QuestionOption>
                {
                    new() { Text = "Bardzo dobrze", Points = 1.00m, Order = 1 },
                    new() { Text = "Dobrze", Points = 0.75m, Order = 2 },
                    new() { Text = "årednio", Points = 0.50m, Order = 3 },
                    new() { Text = "S≥abo", Points = 0.25m, Order = 4 },
                    new() { Text = "Bardzo üle", Points = 0.00m, Order = 5 }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Question
            {
                Id = Guid.NewGuid().ToString(),
                CaretakerId = caretakerId,
                Text = "Jak spa≥eú/aú wczoraj?",
                Description = "OceÒ jakoúÊ swojego snu",
                Type = "closed",
                Order = 2,
                IsActive = true,
                Options = new List<QuestionOption>
                {
                    new() { Text = "Doskonale", Points = 1.00m, Order = 1 },
                    new() { Text = "Dobrze", Points = 0.75m, Order = 2 },
                    new() { Text = "Niez≥e", Points = 0.50m, Order = 3 },
                    new() { Text = "èle", Points = 0.25m, Order = 4 },
                    new() { Text = "Bardzo üle", Points = 0.00m, Order = 5 }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Question
            {
                Id = Guid.NewGuid().ToString(),
                CaretakerId = caretakerId,
                Text = "Czy mia≥eú/aú bÛl dzisiaj?",
                Description = "OceÒ poziom bÛlu, jeúli by≥",
                Type = "closed",
                Order = 3,
                IsActive = true,
                Options = new List<QuestionOption>
                {
                    new() { Text = "Brak bÛlu", Points = 1.00m, Order = 1 },
                    new() { Text = "Lekki bÛl", Points = 0.75m, Order = 2 },
                    new() { Text = "Umiarkowany bÛl", Points = 0.50m, Order = 3 },
                    new() { Text = "Silny bÛl", Points = 0.25m, Order = 4 },
                    new() { Text = "Bardzo silny bÛl", Points = 0.00m, Order = 5 }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Question
            {
                Id = Guid.NewGuid().ToString(),
                CaretakerId = caretakerId,
                Text = "Czy bra≥eú/aú wszystkie leki na czas?",
                Description = "Potwierdzenie zaøycia lekÛw",
                Type = "closed",
                Order = 4,
                IsActive = true,
                Options = new List<QuestionOption>
                {
                    new() { Text = "Tak, wszystkie", Points = 1.00m, Order = 1 },
                    new() { Text = "Tak, prawie wszystkie", Points = 0.75m, Order = 2 },
                    new() { Text = "CzÍúÊ z nich", Points = 0.50m, Order = 3 },
                    new() { Text = "Zapomnia≥em/am", Points = 0.25m, Order = 4 },
                    new() { Text = "Nie bra≥em/am", Points = 0.00m, Order = 5 }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Question
            {
                Id = Guid.NewGuid().ToString(),
                CaretakerId = caretakerId,
                Text = "Czy mia≥eú/aú apetyt dzisiaj?",
                Description = "OceÒ swÛj apetyt",
                Type = "closed",
                Order = 5,
                IsActive = true,
                Options = new List<QuestionOption>
                {
                    new() { Text = "Doskona≥y", Points = 1.00m, Order = 1 },
                    new() { Text = "Dobry", Points = 0.75m, Order = 2 },
                    new() { Text = "PrzeciÍtny", Points = 0.50m, Order = 3 },
                    new() { Text = "S≥aby", Points = 0.25m, Order = 4 },
                    new() { Text = "Brak apetytu", Points = 0.00m, Order = 5 }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Initializes default questions for a caretaker
    /// </summary>
    public async Task<bool> InitializeDefaultQuestionsAsync(
        string caretakerId,
        string caregiverId,
        string idToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(caretakerId) || string.IsNullOrWhiteSpace(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[SeedQuestionsService] Missing caretakerId or idToken");
                return false;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[SeedQuestionsService] Initializing questions for caretaker {caretakerId}");

            // Get default questions
            var defaultQuestions = GetDefaultQuestions(caretakerId, caregiverId);

            System.Diagnostics.Debug.WriteLine(
                $"[SeedQuestionsService] Creating careTaker questions document with {defaultQuestions.Count} default questions");

            // ===== NOWY KOD: Tworzy nowy dokument z pytaniami =====
            var success = await _firestoreService.CreateCareTakerQuestionsAsync(
                caretakerId,
                defaultQuestions,
                idToken);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SeedQuestionsService] Successfully created careTaker questions document for caretaker {caretakerId}");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SeedQuestionsService] Failed to create careTaker questions document for caretaker {caretakerId}");
                return false;
            }
            // ===== KONIEC NOWEGO KODU =====
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SeedQuestionsService] Exception: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
}