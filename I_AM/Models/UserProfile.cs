using System.Text.Json.Serialization;

namespace I_AM.Models;

/// <summary>
/// User profile containing personal information and caregiver/caretaker relationships
/// </summary>
public class UserProfile
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("sex")]
    public string Sex { get; set; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("isCaregiver")]
    public bool IsCaregiver { get; set; }

    [JsonPropertyName("caretakersID")]
    public List<string> CaretakersID { get; set; } = new();

    [JsonPropertyName("caregiversID")]
    public List<string> CaregiversID { get; set; } = new();
}
