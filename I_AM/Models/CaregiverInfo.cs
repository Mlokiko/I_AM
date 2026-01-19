using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// Information about a caregiver assigned to a user
/// </summary>
public class CaregiverInfo
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "accepted"; // accepted, pending, rejected

    [JsonPropertyName("addedAt")]
    public DateTime AddedAt { get; set; }
}
