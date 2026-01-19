using System.Collections.ObjectModel;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.CareGiver;

public partial class ManageCaregiverPage : ContentPage
{
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;
    public ObservableCollection<CaregiverInfo> Caregivers { get; set; }
    
    // Dictionary to track invitation IDs for accepted caregivers
    private Dictionary<string, string> _acceptedCaregiverInvitationIds = new();

    public ManageCaregiverPage()
    {
        InitializeComponent();
        Caregivers = new ObservableCollection<CaregiverInfo>();
        BindingContext = this;
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCaregiversAsync();
    }

    private async Task LoadCaregiversAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: START");
            CaregiversLoadingIndicator.IsRunning = true;
            CaregiversLoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;
            Caregivers.Clear();

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: userId={userId}");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: Missing userId or idToken");
                ShowError("B³¹d: Nie mo¿na za³adowaæ danych u¿ytkownika");
                return;
            }

            // 1. Pobierz listê zaakceptowanych opiekunów
            System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: Calling GetCaregiversAsync");
            var caregivers = await _firestoreService.GetCaregiversAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Retrieved {caregivers.Count} accepted caregivers");

            // 2. Pobierz listê oczekuj¹cych zaproszeñ WYS£ANYCH przez bie¿¹cego u¿ytkownika (caretaker)
            System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: Calling GetSentPendingInvitationsAsync");
            var sentPendingInvitations = await _firestoreService.GetSentPendingInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Retrieved {sentPendingInvitations.Count} sent pending invitations");

            // 3. Pobierz listê odrzuconych zaproszeñ WYS£ANYCH przez bie¿¹cego u¿ytkownika (caretaker)
            System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: Calling GetSentRejectedInvitationsAsync");
            var sentRejectedInvitations = await _firestoreService.GetSentRejectedInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Retrieved {sentRejectedInvitations.Count} sent rejected invitations");

            // 4. Dodaj zaakceptowanych opiekunów
            foreach (var caregiver in caregivers)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Adding accepted caregiver {caregiver.FirstName}");
                Caregivers.Add(caregiver);
                
                // Pobierz ID zaproszenia dla tego opiekuna (jeœli istnieje)
                var invitationId = await GetInvitationIdForCaregiverAsync(userId, caregiver.UserId, idToken);
                if (!string.IsNullOrEmpty(invitationId))
                {
                    _acceptedCaregiverInvitationIds[caregiver.UserId] = invitationId;
                    System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] Found invitation ID {invitationId} for caregiver {caregiver.UserId}");
                }
            }

            // 5. Dodaj wys³ane oczekuj¹ce zaproszenia jako caregivers ze statusem "pending"
            foreach (var invitation in sentPendingInvitations)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Adding sent pending invitation to {invitation.ToUserEmail}");
                Caregivers.Add(new CaregiverInfo
                {
                    UserId = invitation.ToUserId,
                    Email = invitation.ToUserEmail,
                    FirstName = invitation.ToUserEmail.Split('@').FirstOrDefault() ?? invitation.ToUserEmail,
                    LastName = string.Empty,
                    Status = "pending",
                    AddedAt = invitation.CreatedAt
                });
            }

            // 6. Dodaj wys³ane odrzucone zaproszenia jako caregivers ze statusem "rejected"
            foreach (var invitation in sentRejectedInvitations)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Adding sent rejected invitation to {invitation.ToUserEmail}");
                Caregivers.Add(new CaregiverInfo
                {
                    UserId = invitation.ToUserId,
                    Email = invitation.ToUserEmail,
                    FirstName = invitation.ToUserEmail.Split('@').FirstOrDefault() ?? invitation.ToUserEmail,
                    LastName = string.Empty,
                    Status = "rejected",
                    AddedAt = invitation.CreatedAt
                });
            }

            if (Caregivers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: No caregivers or invitations found");
                NoCaregiversLabel.IsVisible = true;
            }
            else
            {
                NoCaregiversLabel.IsVisible = false;
            }
            System.Diagnostics.Debug.WriteLine("[ManageCaregiverPage] LoadCaregiversAsync: SUCCESS");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageCaregiverPage] LoadCaregiversAsync: Exception - {ex.Message}\n{ex.StackTrace}");
            ShowError($"B³¹d podczas ³adowania opiekunów: {ex.Message}");
        }
        finally
        {
            CaregiversLoadingIndicator.IsRunning = false;
            CaregiversLoadingIndicator.IsVisible = false;
        }
    }

    private async void OnAddCaregiverClicked(object sender, EventArgs e)
    {
        if (!(sender is Button button)) return;

        var caregiverEmail = CaregiverEmailEntry.Text?.Trim() ?? string.Empty;

        // Walidacja
        if (string.IsNullOrWhiteSpace(caregiverEmail))
        {
            await DisplayAlert("B³¹d", "Email opiekuna jest wymagany", "OK");
            return;
        }

        if (!caregiverEmail.Contains("@"))
        {
            await DisplayAlert("B³¹d", "Podaj prawid³owy email", "OK");
            return;
        }

        button.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ErrorLabel.IsVisible = false;

        try
        {
            var currentUserId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();
            var currentUserEmail = await _authService.GetCurrentEmailAsync();

            System.Diagnostics.Debug.WriteLine($"OnAddCaregiverClicked: Current User ID: {currentUserId}");
            System.Diagnostics.Debug.WriteLine($"OnAddCaregiverClicked: Szukany email: {caregiverEmail}");

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(idToken))
            {
                ShowError("B³¹d: Nie mo¿na za³adowaæ danych u¿ytkownika");
                return;
            }

            // Zapobiegaj dodaniu siebie
            if (caregiverEmail.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("B³¹d", "Nie mo¿esz dodaæ siebie jako opiekuna", "OK");
                return;
            }

            // 1. Pobierz profil u¿ytkownika po email
            System.Diagnostics.Debug.WriteLine($"OnAddCaregiverClicked: Wywo³ywam GetUserPublicProfileByEmailAsync");
            var (caregiverPublicProfile, caregiverId) = await _firestoreService.GetUserPublicProfileByEmailAsync(caregiverEmail, idToken);

            System.Diagnostics.Debug.WriteLine($"OnAddCaregiverClicked: Wynik - Profile: {caregiverPublicProfile != null}, UserId: {caregiverId}");

            if (caregiverPublicProfile == null || string.IsNullOrEmpty(caregiverId))
            {
                await DisplayAlert("B³¹d", "U¿ytkownik z takim emailem nie istnieje", "OK");
                return;
            }

            // 2. Pobierz ID opiekuna
            // Ju¿ go mamy w caregiverId

            // 3. Stwórz zaproszenie
            var invitationId = Guid.NewGuid().ToString();
            var currentUserProfile = await _firestoreService.GetUserProfileAsync(currentUserId, idToken);

            if (currentUserProfile == null)
            {
                await DisplayAlert("B³¹d", "Nie mo¿na za³adowaæ Twojego profilu", "OK");
                return;
            }

            var invitation = new CaregiverInvitation
            {
                Id = invitationId,
                FromUserId = currentUserId,
                ToUserId = caregiverId,
                ToUserEmail = caregiverEmail,
                FromUserName = $"{currentUserProfile.FirstName} {currentUserProfile.LastName}",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            // 4. Zapisz zaproszenie
            var saved = await _firestoreService.SaveCaregiverInvitationAsync(invitationId, invitation, idToken);

            if (saved)
            {
                await DisplayAlert("Sukces", $"Zaproszenie wys³ane do {caregiverEmail}", "OK");
                CaregiverEmailEntry.Text = string.Empty;
                // Reload caregivers list after sending invitation
                await LoadCaregiversAsync();
            }
            else
            {
                ShowError("Nie uda³o siê wys³aæ zaproszenia");
            }
        }
        catch (Exception ex)
        {
            ShowError($"B³¹d: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnRemoveCaregiverClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var caregiver = button.BindingContext as CaregiverInfo;
        if (caregiver == null) return;

        var result = await DisplayAlert(
            "Potwierdzenie",
            $"Czy na pewno chcesz usun¹æ {caregiver.FirstName} {caregiver.LastName} z opiekunów?",
            "Tak",
            "Nie"
        );

        if (!result) return;

        try
        {
            button.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                await DisplayAlert("B³¹d", "Nie mo¿na za³adowaæ danych u¿ytkownika", "OK");
                return;
            }

            bool success = false;

            // Jeœli status to "pending" lub "rejected", usuñ zaproszenie z bazy
            if (caregiver.Status == "pending" || caregiver.Status == "rejected")
            {
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCaregiverClicked] Deleting {caregiver.Status} invitation for {caregiver.Email}");
                // Najpierw trzeba znaleŸæ ID zaproszenia
                if (caregiver.Status == "pending")
                {
                    var pendingInvitations = await _firestoreService.GetSentPendingInvitationsAsync(userId, idToken);
                    var invitation = pendingInvitations.FirstOrDefault(i => i.ToUserId == caregiver.UserId);
                    if (invitation != null)
                    {
                        success = await _firestoreService.DeleteCaregiverInvitationAsync(invitation.Id, idToken);
                    }
                }
                else if (caregiver.Status == "rejected")
                {
                    var rejectedInvitations = await _firestoreService.GetSentRejectedInvitationsAsync(userId, idToken);
                    var invitation = rejectedInvitations.FirstOrDefault(i => i.ToUserId == caregiver.UserId);
                    if (invitation != null)
                    {
                        success = await _firestoreService.DeleteCaregiverInvitationAsync(invitation.Id, idToken);
                    }
                }
            }
            else
            {
                // W innym wypadku (accepted), usuñ z listy opiekunów
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCaregiverClicked] Removing accepted caregiver {caregiver.Email}");
                success = await _firestoreService.RemoveCaregiverAsync(userId, caregiver.UserId, idToken);
                
                // Jeœli opiekun zosta³ pomyœlnie usuniêty, usuñ równie¿ zaproszenie z bazy
                if (success && _acceptedCaregiverInvitationIds.TryGetValue(caregiver.UserId, out var invitationId))
                {
                    System.Diagnostics.Debug.WriteLine($"[OnRemoveCaregiverClicked] Deleting invitation {invitationId} for removed caregiver");
                    var deleteSuccess = await _firestoreService.DeleteCaregiverInvitationAsync(invitationId, idToken);
                    System.Diagnostics.Debug.WriteLine($"[OnRemoveCaregiverClicked] Invitation deletion result: {deleteSuccess}");
                    
                    // Usuñ z s³ownika
                    _acceptedCaregiverInvitationIds.Remove(caregiver.UserId);
                }
            }

            if (success)
            {
                Caregivers.Remove(caregiver);
                
                if (Caregivers.Count == 0)
                {
                    NoCaregiversLabel.IsVisible = true;
                }

                await DisplayAlert("Sukces", "Opiekun zosta³ usuniêty", "OK");
                // Reload caregivers list after removal
                await LoadCaregiversAsync();
            }
            else
            {
                await DisplayAlert("B³¹d", "Nie uda³o siê usun¹æ opiekuna", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async Task<string?> GetInvitationIdForCaregiverAsync(string caretakerId, string caregiverId, string idToken)
    {
        try
        {
            // Pobierz wszystkie zaakceptowane zaproszenia wys³ane przez caretaker'a do tego caregiver'a
            var allInvitations = await _firestoreService.GetAllCaregiverInvitationsAsync(caretakerId, idToken);
            
            var invitation = allInvitations.FirstOrDefault(i => 
                i.FromUserId == caretakerId && 
                i.ToUserId == caregiverId && 
                i.Status == "accepted");
            
            return invitation?.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetInvitationIdForCaregiverAsync] Error: {ex.Message}");
            return null;
        }
    }
}
