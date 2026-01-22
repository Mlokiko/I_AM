namespace I_AM.Constants;

/// <summary>
/// Application-wide constants for status values, validation messages, and configuration
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Caregiver invitation status constants
    /// </summary>
    public static class InvitationStatus
    {
        public const string Pending = "pending";
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
    }

    /// <summary>
    /// Caregiver relationship status constants
    /// </summary>
    public static class CaregiverStatus
    {
        public const string Accepted = "accepted";
        public const string Pending = "pending";
        public const string Rejected = "rejected";
    }

    /// <summary>
    /// Notification type constants
    /// </summary>
    public static class NotificationType
    {
        public const string Info = "info";
        public const string Warning = "warning";
        public const string Error = "error";
        public const string Success = "success";
        public const string CaregiverInvitation = "caregiver_invitation";
    }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public static class ValidationMessages
    {
        public const string EmailRequired = "Email jest wymagany";
        public const string InvalidEmail = "Podaj prawid³owy email";
        public const string PasswordTooShort = "Has³o musi mieæ co najmniej 6 znaków";
        public const string CannotAddSelf = "Nie mo¿esz dodaæ siebie jako opiekuna";
        public const string FirstNameRequired = "Imiê jest wymagane";
        public const string LastNameRequired = "Nazwisko jest wymagane";
        public const string PhoneRequired = "Numer telefonu jest wymagany";
    }

    /// <summary>
    /// Success messages
    /// </summary>
    public static class SuccessMessages
    {
        public const string InvitationSent = "Zaproszenie wys³ane pomyœlnie";
        public const string InvitationAccepted = "Zaproszenie zaakceptowane";
        public const string InvitationRejected = "Zaproszenie odrzucone";
        public const string CaregiverRemoved = "Opiekun zosta³ usuniêty";
        public const string ProfileUpdated = "Profil zosta³ zaktualizowany";
    }

    /// <summary>
    /// Error messages
    /// </summary>
    public static class ErrorMessages
    {
        public const string RegistrationFailed = "Rejestracja nie powiod³a siê";
        public const string LoginFailed = "Logowanie nie powiod³o siê";
        public const string UserNotFound = "U¿ytkownik nie znaleziony";
        public const string InvalidCredentials = "Niepoprawny email lub has³o";
        public const string AccountDisabled = "Konto zosta³o wy³¹czone";
        public const string OperationFailed = "Operacja nie powiod³a siê";
    }

    /// <summary>
    /// Question type constants
    /// </summary>
    public static class QuestionType
    {
        public const string Closed = "closed";
        public const string Open = "open";
    }

    /// <summary>
    /// Survey messages
    /// </summary>
    public static class SurveyMessages
    {
        public const string QuestionsSaved = "Pytania zosta³y zapisane";
        public const string QuestionDeleted = "Pytanie zosta³o usuniête";
        public const string AnswersSubmitted = "Twoje odpowiedzi zosta³y wys³ane";
        public const string AllQuestionsRequired = "Proszê odpowiedzieæ na wszystkie pytania";
        public const string NoQuestionsAvailable = "Opiekun nie przygotowa³ jeszcze pytañ";
        public const string NoResults = "Brak zapisanych wyników testów";
    }
}
