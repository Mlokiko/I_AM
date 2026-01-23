using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Survey session with all answers and aggregated results
/// </summary>
public class TestSession
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("caretakerId")]
    public string CaretakerId { get; set; } = string.Empty;

    [JsonPropertyName("totalPoints")]
    public decimal TotalPoints { get; set; }

    [JsonPropertyName("maxPoints")]
    public decimal MaxPoints { get; set; }

    [JsonPropertyName("percentageScore")]
    public decimal PercentageScore { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("answers")]
    public List<QuestionAnswer> Answers { get; set; } = new();
}