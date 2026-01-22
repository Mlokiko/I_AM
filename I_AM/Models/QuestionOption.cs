using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Single answer option for a closed question
/// </summary>
public class QuestionOption
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    public decimal Points { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}