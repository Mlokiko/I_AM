using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Questions collection dla podopiecznego
/// </summary>
public class CareTakerQuestions
{
    [JsonPropertyName("caretakerId")]
    public string CaretakerId { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<Question> Questions { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}