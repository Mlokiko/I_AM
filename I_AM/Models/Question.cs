using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Survey question for caretaker assessment
/// </summary>
public class Question
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("caretakerId")]
    public string CaretakerId { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "closed"; // closed, open

    [JsonPropertyName("options")]
    public List<QuestionOption> Options { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("order")]
    public int Order { get; set; }
}