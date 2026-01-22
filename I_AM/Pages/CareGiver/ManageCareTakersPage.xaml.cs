using System.Collections.ObjectModel;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;
using I_AM.Services.Helpers;

namespace I_AM.Pages.CareGiver;

public partial class ManageCareTakersPage : ContentPage
{
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;
    public ObservableCollection<CaregiverInfo> CareTakers { get; set; }

    public ManageCareTakersPage()
    {
        InitializeComponent();
        CareTakers = new ObservableCollection<CaregiverInfo>();
        BindingContext = this;
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCareTakersAsync();
    }

    private async Task<List<CaregiverInvitation>> GetAllInvitationsReceivedAsync(string userId, string idToken)
    {
        var allInvitations = new List<CaregiverInvitation>();
        
        // Get pending invitations
        var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
        allInvitations.AddRange(pendingInvitations);
        
        // Get accepted invitations
        var acceptedInvitations = await _firestoreService.GetAllCaregiverInvitationsAsync(userId, idToken);
        allInvitations.AddRange(acceptedInvitations.Where(i => i.Status == "accepted"));
        
        // Get rejected invitations
        allInvitations.AddRange(acceptedInvitations.Where(i => i.Status == "rejected"));

        return allInvitations;
    }

    private async Task<string?> FindInvitationIdAsync(string userId, string careTakerId, string status, string idToken)
    {
        var allInvitations = await GetAllInvitationsReceivedAsync(userId, idToken);
        var invitation = allInvitations.FirstOrDefault(i => 
            i.FromUserId == careTakerId && i.Status == status);
        return invitation?.Id;
    }

    private async Task LoadCareTakersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: START");
            CareTakersLoadingIndicator.IsRunning = true;
            CareTakersLoadingIndicator.IsVisible = true;
            ErrorLabel.IsVisible = false;
            CareTakers.Clear();

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: userId={userId}");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Missing userId or idToken");
                ShowError("Error: Unable to load user data");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetPendingInvitationsAsync");
            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {pendingInvitations.Count} pending invitations");

            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Getting all invitations to find accepted/rejected");
            var allInvitations = await GetAllInvitationsReceivedAsync(userId, idToken);
            
            var acceptedInvitations = allInvitations.Where(i => i.Status == "accepted").ToList();
            var rejectedInvitations = allInvitations.Where(i => i.Status == "rejected").ToList();

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: {pendingInvitations.Count} pending, {acceptedInvitations.Count} accepted, {rejectedInvitations.Count} rejected");

            await AddInvitationsToListAsync(pendingInvitations, "pending", idToken);
            await AddInvitationsToListAsync(acceptedInvitations, "accepted", idToken);
            await AddInvitationsToListAsync(rejectedInvitations, "rejected", idToken);

            NoCareTakersLabel.IsVisible = CareTakers.Count == 0;
            if (CareTakers.Count == 0)
                System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: No careTakers found");
            
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: SUCCESS");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Exception - {ex.Message}\n{ex.StackTrace}");
            ShowError($"Error loading careTakers: {ex.Message}");
        }
        finally
        {
            CareTakersLoadingIndicator.IsRunning = false;
            CareTakersLoadingIndicator.IsVisible = false;
        }
    }

    private async Task AddInvitationsToListAsync(List<CaregiverInvitation> invitations, string status, string idToken)
    {
        foreach (var invitation in invitations)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding {status} invitation from {invitation.FromUserName}");
            
            var (email, firstName, lastName) = await GetCareTakerInfoAsync(invitation, idToken, status == "pending");
            
            CareTakers.Add(new CaregiverInfo
            {
                UserId = invitation.FromUserId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Status = status,
                AddedAt = invitation.CreatedAt
            });
        }
    }

    private async Task<(string email, string firstName, string lastName)> GetCareTakerInfoAsync(
        CaregiverInvitation invitation, string idToken, bool skipProfileFetch = false)
    {
        string email = invitation.ToUserEmail;
        string firstName = invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName;
        string lastName = invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty;

        if (!skipProfileFetch)
        {
            var careTakerProfile = await _firestoreService.GetUserProfileAsync(invitation.FromUserId, idToken);
            if (careTakerProfile != null)
            {
                email = careTakerProfile.Email ?? email;
                firstName = careTakerProfile.FirstName ?? firstName;
                lastName = careTakerProfile.LastName ?? lastName;
            }
        }

        return (email, firstName, lastName);
    }

    private async void OnAddCareTakerClicked(object sender, EventArgs e)
    {
        if (!(sender is Button button)) return;

        var careTakerEmail = CareTakerEmailEntry.Text?.Trim() ?? string.Empty;

        // Validation
        if (string.IsNullOrWhiteSpace(careTakerEmail))
        {
            await DisplayAlert("Error", "CareTaker email is required", "OK");
            return;
        }

        if (!careTakerEmail.Contains("@"))
        {
            await DisplayAlert("Error", "Please provide a valid email", "OK");
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

            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Current User ID: {currentUserId}");
            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Target email: {careTakerEmail}");

            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(idToken))
            {
                ShowError("Error: Unable to load user data");
                return;
            }

            // Prevent adding yourself
            if (careTakerEmail.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Error", "You cannot add yourself as a careTaker", "OK");
                return;
            }

            // 1. Get user profile by email
            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Calling GetUserPublicProfileByEmailAsync");
            var (careTakerPublicProfile, careTakerId) = await _firestoreService.GetUserPublicProfileByEmailAsync(careTakerEmail, idToken);

            System.Diagnostics.Debug.WriteLine($"OnAddCareTakerClicked: Result - Profile: {careTakerPublicProfile != null}, UserId: {careTakerId}");

            if (careTakerPublicProfile == null || string.IsNullOrEmpty(careTakerId))
            {
                await DisplayAlert("Error", "User with this email does not exist", "OK");
                return;
            }

            // 2. Create invitation
            var invitationId = Guid.NewGuid().ToString();
            var currentUserProfile = await _firestoreService.GetUserProfileAsync(currentUserId, idToken);

            if (currentUserProfile == null)
            {
                await DisplayAlert("Error", "Unable to load your profile", "OK");
                return;
            }

            var invitation = new CaregiverInvitation
            {
                Id = invitationId,
                FromUserId = currentUserId,
                ToUserId = careTakerId,
                ToUserEmail = careTakerEmail,
                FromUserName = $"{currentUserProfile.FirstName} {currentUserProfile.LastName}",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            // 3. Save invitation
            var saved = await _firestoreService.SaveCaregiverInvitationAsync(invitationId, invitation, idToken);

            if (saved)
            {
                await DisplayAlert("Success", $"Invitation sent to {careTakerEmail}", "OK");
                CareTakerEmailEntry.Text = string.Empty;
                // Reload careTakers list after sending invitation
                await LoadCareTakersAsync();
            }
            else
            {
                ShowError("Failed to send invitation");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnAcceptCareTakerClicked(object sender, EventArgs e)
    {
        await HandleInvitationResponseAsync(sender, "accept", 
            "Potwierdzenie",
            $"Czy chcesz zaakceptowaæ ",
            $" jako podopiecznego?",
            "Sukces",
            "Zaproszenie zaakceptowane od ",
            "B³¹d",
            "Nie mo¿na znaleŸæ zaproszenia dla tego podopiecznego");
    }

    private async void OnRejectCareTakerClicked(object sender, EventArgs e)
    {
        await HandleInvitationResponseAsync(sender, "reject",
            "Potwierdzenie",
            $"Czy chcesz odrzuciæ zaproszenie od ",
            "?",
            "Sukces",
            "Zaproszenie odrzucone",
            "B³¹d",
            "Nie mo¿na znaleŸæ zaproszenia dla tego podopiecznego");
    }

    private async Task HandleInvitationResponseAsync(object sender, string action, 
        string titleText, string messagePrefix, string messageSuffix,
        string successTitle, string successMessage, 
        string errorTitle, string notFoundMessage)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;
        
        if (careTaker.Status != "pending")
        {
            await DisplayAlert("Informacja", "Mo¿na tylko zaakceptowaæ/odrzuciæ oczekuj¹ce zaproszenia", "OK");
            return;
        }

        var displayMessage = action == "accept" 
            ? messagePrefix + careTaker.FirstName + " " + careTaker.LastName + messageSuffix
            : messagePrefix + careTaker.FirstName + " " + careTaker.LastName + messageSuffix;

        var result = await DisplayAlert(titleText, displayMessage, "Tak", "Nie");
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
                await DisplayAlert(errorTitle, "Nie mo¿na za³adowaæ danych u¿ytkownika", "OK");
                return;
            }

            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
            var invitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);

            if (invitation == null)
            {
                await DisplayAlert(errorTitle, notFoundMessage, "OK");
                return;
            }

            bool success = action == "accept"
                ? await _firestoreService.AcceptCaregiverInvitationAsync(userId, invitation.Id, careTaker.UserId, idToken)
                : await _firestoreService.RejectCaregiverInvitationAsync(userId, invitation.Id, idToken);

            if (success)
            {
                var message = action == "accept" ? successMessage + careTaker.FirstName : successMessage;
                await DisplayAlert(successTitle, message, "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert(errorTitle, $"Nie uda³o siê wykonaæ operacji", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(errorTitle, $"B³¹d: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnRemoveCareTakerClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;

        if (careTaker.Status == "pending")
        {
            await DisplayAlert("Informacja", "Nie mozna usunac oczekujacego zaproszenia. Prosze najpierw przyj ac lub odrzucic.", "OK");
            return;
        }

        var result = await DisplayAlert(
            "Potwierdzenie",
            $"Czy na pewno chcesz usunac {careTaker.FirstName} {careTaker.LastName}?",
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
                await DisplayAlert("Blad", "Nie mozna zaladowac danych uzytkownika", "OK");
                return;
            }

            var invitationId = await FindInvitationIdAsync(userId, careTaker.UserId, careTaker.Status, idToken);
            
            if (string.IsNullOrEmpty(invitationId))
            {
                await DisplayAlert("B³¹d", "Nie mo¿na znaleŸæ zaproszenia", "OK");
                return;
            }

            var success = await _firestoreService.DeleteCaregiverInvitationAsync(invitationId, idToken);

            if (success)
            {
                CareTakers.Remove(careTaker);
                
                if (CareTakers.Count == 0)
                {
                    NoCareTakersLabel.IsVisible = true;
                }

                await DisplayAlert("Sukces", "Podopieczny zostal usuniety", "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("Blad", "Nie udalo sie usunac podopiecznego", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Blad", $"Blad: {ex.Message}", "OK");
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
}
