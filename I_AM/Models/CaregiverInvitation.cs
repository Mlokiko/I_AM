using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Represents an invitation from a caretaker to a caregiver
/// </summary>
public class CaregiverInvitation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fromUserId")]
    public string FromUserId { get; set; } = string.Empty;

    [JsonPropertyName("toUserId")]
    public string ToUserId { get; set; } = string.Empty;

    [JsonPropertyName("toUserEmail")]
    public string ToUserEmail { get; set; } = string.Empty;

    [JsonPropertyName("fromUserName")]
    public string FromUserName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending"; // pending, accepted, rejected

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("respondedAt")]
    public DateTime? RespondedAt { get; set; }
}
