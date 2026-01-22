using System.Text.Json;
using I_AM.Models;

namespace I_AM.Services.Helpers;

/// <summary>
/// Helper class for building Firestore REST API JSON payloads
/// </summary>
public static class FirestorePayloadBuilder
{
    /// <summary>
    /// Builds a JSON payload for a user profile in Firestore format
    /// </summary>
    public static string BuildProfilePayload(UserProfile profile)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            WriteStringField(writer, "firstName", profile.FirstName);
            WriteStringField(writer, "lastName", profile.LastName);
            WriteIntField(writer, "age", profile.Age);
            WriteStringField(writer, "sex", profile.Sex);
            WriteStringField(writer, "phoneNumber", profile.PhoneNumber);
            WriteStringField(writer, "email", profile.Email);
            WriteTimestampField(writer, "createdAt", profile.CreatedAt);
            WriteBoolField(writer, "isCaregiver", profile.IsCaregiver);
            WriteStringArrayField(writer, "caretakersID", profile.CaretakersID);
            WriteStringArrayField(writer, "caregiversID", profile.CaregiversID);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Builds a JSON payload for a public user profile in Firestore format
    /// </summary>
    public static string BuildPublicProfilePayload(UserPublicProfile profile)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            WriteStringField(writer, "userId", profile.UserId);
            WriteStringField(writer, "email", profile.Email);
            WriteStringField(writer, "firstName", profile.FirstName);
            WriteStringField(writer, "lastName", profile.LastName);
            WriteTimestampField(writer, "createdAt", profile.CreatedAt);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Builds a JSON payload for a caregiver invitation in Firestore format
    /// </summary>
    public static string BuildInvitationPayload(CaregiverInvitation invitation)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(invitation.Id))
                WriteStringField(writer, "id", invitation.Id);

            if (!string.IsNullOrEmpty(invitation.FromUserId))
                WriteStringField(writer, "fromUserId", invitation.FromUserId);

            if (!string.IsNullOrEmpty(invitation.ToUserId))
                WriteStringField(writer, "toUserId", invitation.ToUserId);

            if (!string.IsNullOrEmpty(invitation.ToUserEmail))
                WriteStringField(writer, "toUserEmail", invitation.ToUserEmail);

            if (!string.IsNullOrEmpty(invitation.FromUserName))
                WriteStringField(writer, "fromUserName", invitation.FromUserName);

            WriteStringField(writer, "status", invitation.Status);
            WriteTimestampField(writer, "createdAt", invitation.CreatedAt);

            if (invitation.RespondedAt.HasValue)
                WriteTimestampField(writer, "respondedAt", invitation.RespondedAt.Value);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Builds a JSON payload for updating a string array field
    /// </summary>
    public static string BuildStringArrayPayload(string fieldName, List<string> values)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            WriteStringArrayField(writer, fieldName, values);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Builds a JSON payload for a question in Firestore format
    /// </summary>
    public static string BuildQuestionPayload(Question question)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            WriteStringField(writer, "id", question.Id);
            WriteStringField(writer, "caretakerId", question.CaretakerId);
            WriteStringField(writer, "caregiverId", question.CaregiverId);
            WriteStringField(writer, "text", question.Text);
            WriteStringField(writer, "description", question.Description);
            WriteStringField(writer, "type", question.Type);
            WriteQuestionOptionsField(writer, "options", question.Options);
            WriteBoolField(writer, "isActive", question.IsActive);
            WriteTimestampField(writer, "createdAt", question.CreatedAt);
            WriteTimestampField(writer, "updatedAt", question.UpdatedAt);
            WriteIntField(writer, "order", question.Order);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Builds a JSON payload for a question answer in Firestore format
    /// </summary>
    public static string BuildAnswerPayload(QuestionAnswer answer)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            WriteStringField(writer, "id", answer.Id);
            WriteStringField(writer, "questionId", answer.QuestionId);
            WriteStringField(writer, "caretakerId", answer.CaretakerId);
            WriteStringField(writer, "caregiverId", answer.CaregiverId);
            WriteStringField(writer, "selectedOption", answer.SelectedOption);
            WriteDecimalField(writer, "selectedOptionPoints", answer.SelectedOptionPoints);
            WriteStringField(writer, "openAnswer", answer.OpenAnswer);
            WriteTimestampField(writer, "answeredAt", answer.AnsweredAt);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Builds a JSON payload for a test session in Firestore format
    /// </summary>
    public static string BuildTestSessionPayload(TestSession session)
    {
        using (var stream = new MemoryStream())
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fields");
            writer.WriteStartObject();

            WriteStringField(writer, "id", session.Id);
            WriteStringField(writer, "caretakerId", session.CaretakerId);
            WriteStringField(writer, "caregiverId", session.CaregiverId);
            WriteDecimalField(writer, "totalPoints", session.TotalPoints);
            WriteDecimalField(writer, "maxPoints", session.MaxPoints);
            WriteDecimalField(writer, "percentageScore", session.PercentageScore);
            WriteTimestampField(writer, "completedAt", session.CompletedAt);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    // Helper methods for writing specific field types

    private static void WriteStringField(Utf8JsonWriter writer, string fieldName, string value)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WriteString("stringValue", value);
        writer.WriteEndObject();
    }

    private static void WriteIntField(Utf8JsonWriter writer, string fieldName, int value)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WriteString("integerValue", value.ToString());
        writer.WriteEndObject();
    }

    private static void WriteTimestampField(Utf8JsonWriter writer, string fieldName, DateTime value)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WriteString("timestampValue", value.ToString("o"));
        writer.WriteEndObject();
    }

    private static void WriteBoolField(Utf8JsonWriter writer, string fieldName, bool value)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WriteBoolean("booleanValue", value);
        writer.WriteEndObject();
    }

    private static void WriteStringArrayField(Utf8JsonWriter writer, string fieldName, List<string> values)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WritePropertyName("arrayValue");
        writer.WriteStartObject();

        if (values.Count > 0)
        {
            writer.WritePropertyName("values");
            writer.WriteStartArray();
            foreach (var value in values)
            {
                writer.WriteStartObject();
                writer.WriteString("stringValue", value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteDecimalField(Utf8JsonWriter writer, string fieldName, decimal value)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WriteString("doubleValue", value.ToString());
        writer.WriteEndObject();
    }

    private static void WriteQuestionOptionsField(Utf8JsonWriter writer, string fieldName, List<QuestionOption> options)
    {
        writer.WritePropertyName(fieldName);
        writer.WriteStartObject();
        writer.WritePropertyName("arrayValue");
        writer.WriteStartObject();

        if (options.Count > 0)
        {
            writer.WritePropertyName("values");
            writer.WriteStartArray();
            foreach (var option in options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("mapValue");
                writer.WriteStartObject();
                writer.WritePropertyName("fields");
                writer.WriteStartObject();

                WriteStringField(writer, "text", option.Text);
                WriteDecimalField(writer, "points", option.Points);
                WriteIntField(writer, "order", option.Order);

                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
