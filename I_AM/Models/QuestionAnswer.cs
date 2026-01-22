using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Answer submitted by caretaker to a question
/// </summary>
public class QuestionAnswer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("caretakerId")]
    public string CaretakerId { get; set; } = string.Empty;

    [JsonPropertyName("caregiverId")]
    public string CaregiverId { get; set; } = string.Empty;

    [JsonPropertyName("selectedOption")]
    public string SelectedOption { get; set; } = string.Empty; // Text of selected option

    [JsonPropertyName("selectedOptionPoints")]
    public decimal SelectedOptionPoints { get; set; }

    [JsonPropertyName("openAnswer")]
    public string OpenAnswer { get; set; } = string.Empty; // For open questions

    [JsonPropertyName("answeredAt")]
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}