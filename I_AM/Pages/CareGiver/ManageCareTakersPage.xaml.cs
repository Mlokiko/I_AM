using System.Collections.ObjectModel;
using System.Windows.Input;
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
    
    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing != value)
            {
                _isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }
    }

    public ICommand RefreshCommand { get; }

    // Dictionary to track invitation IDs for accepted caretakers
    private Dictionary<string, string> _acceptedCareTakerInvitationIds = new();
    private bool _isLoadingData = false;

    public ManageCareTakersPage()
    {
        InitializeComponent();
        CareTakers = new ObservableCollection<CaregiverInfo>();
        RefreshCommand = new Command(async () => await LoadCareTakersAsync());
        BindingContext = this;
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_isLoadingData)
        {
            await LoadCareTakersAsync();
        }
    }

    private void ConfigureButtonVisibility()
    {
        // Get the CollectionView element
        var collectionView = CareTakersCollectionView;
        if (collectionView?.ItemsSource == null)
            return;

        // We need to update visibility after items are added
        // Since we can't directly access template controls, we'll handle this in each button's clicked event
    }

    private async Task LoadCareTakersAsync()
    {
        // Prevent concurrent loads
        if (_isLoadingData)
        {
            return;
        }

        _isLoadingData = true;
        try
        {
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: START");
            IsRefreshing = true;
            ErrorLabel.IsVisible = false;
            CareTakers.Clear();
            _acceptedCareTakerInvitationIds.Clear();

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: userId={userId}");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Missing userId or idToken");
                ShowError("Error: Unable to load user data");
                return;
            }

            // 1. Get list of accepted caretakers
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetCaretakersAsync");
            var caretakers = await _firestoreService.GetCaretakersAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {caretakers.Count} accepted caretakers");

            // 2. Get list of received pending invitations
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetPendingInvitationsAsync");
            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {pendingInvitations.Count} pending invitations");

            // 3. Get list of received rejected invitations
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetReceivedRejectedInvitationsAsync");
            var rejectedInvitations = await _firestoreService.GetReceivedRejectedInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {rejectedInvitations.Count} rejected invitations");

            // 4. Get list of sent pending invitations
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetSentPendingInvitationsAsync");
            var sentPendingInvitations = await _firestoreService.GetSentPendingInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {sentPendingInvitations.Count} sent pending invitations");

            // 5. Get list of sent rejected invitations
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetSentRejectedInvitationsAsync");
            var sentRejectedInvitations = await _firestoreService.GetSentRejectedInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {sentRejectedInvitations.Count} sent rejected invitations");

            // 6. Add accepted caretakers
            foreach (var caretaker in caretakers)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding accepted caretaker {caretaker.FirstName}");
                CareTakers.Add(caretaker);
                
                // Get invitation ID for this caretaker (if it exists)
                var invitationId = await GetInvitationIdForCaretakerAsync(userId, caretaker.UserId, idToken);
                if (!string.IsNullOrEmpty(invitationId))
                {
                    _acceptedCareTakerInvitationIds[caretaker.UserId] = invitationId;
                    System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] Found invitation ID {invitationId} for caretaker {caretaker.UserId}");
                }
            }

            // 7. Add received pending invitations as caretakers with "pending" status
            foreach (var invitation in pendingInvitations)
            {
                if (!CareTakers.Any(c => c.UserId == invitation.FromUserId && c.Status == "pending" && !c.IsSentByMe))
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding received pending invitation from {invitation.FromUserName}");
                    CareTakers.Add(new CaregiverInfo
                    {
                        UserId = invitation.FromUserId,
                        Email = invitation.ToUserEmail,
                        FirstName = invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName,
                        LastName = invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty,
                        Status = "pending",
                        AddedAt = invitation.CreatedAt,
                        IsSentByMe = false
                    });
                }
            }

            // 8. Add received rejected invitations as caretakers with "rejected" status
            foreach (var invitation in rejectedInvitations)
            {
                if (!CareTakers.Any(c => c.UserId == invitation.FromUserId && c.Status == "rejected" && !c.IsSentByMe))
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding received rejected invitation from {invitation.FromUserName}");
                    CareTakers.Add(new CaregiverInfo
                    {
                        UserId = invitation.FromUserId,
                        Email = invitation.ToUserEmail,
                        FirstName = invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName,
                        LastName = invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty,
                        Status = "rejected",
                        AddedAt = invitation.CreatedAt,
                        IsSentByMe = false
                    });
                }
            }

            // 9. Add sent pending invitations as caretakers with "pending" status
            foreach (var invitation in sentPendingInvitations)
            {
                if (!CareTakers.Any(c => c.UserId == invitation.ToUserId && c.Status == "pending" && c.IsSentByMe))
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding sent pending invitation to {invitation.ToUserEmail}");
                    CareTakers.Add(new CaregiverInfo
                    {
                        UserId = invitation.ToUserId,
                        Email = invitation.ToUserEmail,
                        FirstName = invitation.ToUserEmail.Split('@').FirstOrDefault() ?? invitation.ToUserEmail,
                        LastName = string.Empty,
                        Status = "pending",
                        AddedAt = invitation.CreatedAt,
                        IsSentByMe = true
                    });
                }
            }

            // 10. Add sent rejected invitations as caretakers with "rejected" status
            foreach (var invitation in sentRejectedInvitations)
            {
                if (!CareTakers.Any(c => c.UserId == invitation.ToUserId && c.Status == "rejected" && c.IsSentByMe))
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding sent rejected invitation to {invitation.ToUserEmail}");
                    CareTakers.Add(new CaregiverInfo
                    {
                        UserId = invitation.ToUserId,
                        Email = invitation.ToUserEmail,
                        FirstName = invitation.ToUserEmail.Split('@').FirstOrDefault() ?? invitation.ToUserEmail,
                        LastName = string.Empty,
                        Status = "rejected",
                        AddedAt = invitation.CreatedAt,
                        IsSentByMe = true
                    });
                }
            }

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
            IsRefreshing = false;
            _isLoadingData = false;
        }
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
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;
        
        // Only allow accepting received pending invitations (IsSentByMe = false, Status = "pending")
        if (careTaker.IsSentByMe || careTaker.Status != "pending")
        {
            await DisplayAlert("Information", "You can only accept received pending invitations", "OK");
            return;
        }

        var result = await DisplayAlert(
            "Confirmation",
            $"Do you want to accept {careTaker.FirstName} {careTaker.LastName} as a caretaker?",
            "Yes",
            "No"
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
                await DisplayAlert("Error", "Unable to load user data", "OK");
                return;
            }

            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
            var invitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);

            if (invitation == null)
            {
                await DisplayAlert("Error", "Cannot find invitation for this caretaker", "OK");
                return;
            }

            var success = await _firestoreService.AcceptCaregiverInvitationAsync(userId, invitation.Id, careTaker.UserId, idToken);

            if (success)
            {
                // ===== WYKOMENTOWANE: Inicjalizuj pytania =====
                //var seedService = ServiceHelper.GetService<SeedQuestionsService>();
                //var questionsInitialized = await seedService.InitializeDefaultQuestionsAsync(
                //    careTaker.UserId,  // caretakerId
                //    userId,            // caregiverId
                //    idToken);

                //if (questionsInitialized)
                //{
                //    System.Diagnostics.Debug.WriteLine(
                //        "[ManageCareTakersPage] Default questions initialized successfully");
                //    await DisplayAlert(
                //        "Sukces", 
                //        $"Zaproszenie zaakceptowane od {careTaker.FirstName}\n\n Pytania zosta³y przygotowane",
                //        "OK");
                //}
                //else
                //{
                //    System.Diagnostics.Debug.WriteLine(
                //        "[ManageCareTakersPage] Failed to initialize questions");
                //    await DisplayAlert(
                //        "Uwaga", 
                //        $"Zaproszenie zaakceptowane, ale pytania nie mog³y byæ przygotowane",
                //        "OK");
                //}
                // ===== KONIEC WYKOMENTOWANEGO KODU =====

                await DisplayAlert("Sukces", $"Zaproszenie zaakceptowane od {careTaker.FirstName}", "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to accept invitation", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            button.IsEnabled = true;
        }
    }

    private async void OnRejectCareTakerClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;
        
        // Only allow rejecting received pending invitations (IsSentByMe = false, Status = "pending")
        if (careTaker.IsSentByMe || careTaker.Status != "pending")
        {
            await DisplayAlert("Information", "You can only reject received pending invitations", "OK");
            return;
        }

        var result = await DisplayAlert(
            "Confirmation",
            $"Do you want to reject invitation from {careTaker.FirstName} {careTaker.LastName}?",
            "Yes",
            "No"
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
                await DisplayAlert("Error", "Unable to load user data", "OK");
                return;
            }

            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
            var invitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);

            if (invitation == null)
            {
                await DisplayAlert("Error", "Cannot find invitation for this caretaker", "OK");
                return;
            }

            var success = await _firestoreService.RejectCaregiverInvitationAsync(userId, invitation.Id, idToken);

            if (success)
            {
                await DisplayAlert("Success", "Invitation rejected", "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to reject invitation", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
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

        var result = await DisplayAlert(
            "Confirmation",
            $"Do you want to remove {careTaker.FirstName} {careTaker.LastName}?",
            "Yes",
            "No"
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
                await DisplayAlert("Error", "Unable to load user data", "OK");
                return;
            }

            bool success = false;

            // If status is "pending" or "rejected", delete the invitation
            if (careTaker.Status == "pending" || careTaker.Status == "rejected")
            {
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Deleting {careTaker.Status} invitation for {careTaker.UserId}");
                
                // Check if this is a received invitation
                if (careTaker.Status == "pending")
                {
                    var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
                    var receivedInvitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);
                    
                    if (receivedInvitation != null)
                    {
                        success = await _firestoreService.DeleteCaregiverInvitationAsync(receivedInvitation.Id, idToken);
                    }
                    else
                    {
                        // Check if this is a sent invitation
                        var sentInvitations = await _firestoreService.GetSentPendingInvitationsAsync(userId, idToken);
                        var sentInvitation = sentInvitations.FirstOrDefault(i => i.ToUserId == careTaker.UserId);
                        if (sentInvitation != null)
                        {
                            success = await _firestoreService.DeleteCaregiverInvitationAsync(sentInvitation.Id, idToken);
                        }
                    }
                }
                else if (careTaker.Status == "rejected")
                {
                    var rejectedInvitations = await _firestoreService.GetReceivedRejectedInvitationsAsync(userId, idToken);
                    var receivedRejected = rejectedInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);
                    
                    if (receivedRejected != null)
                    {
                        success = await _firestoreService.DeleteCaregiverInvitationAsync(receivedRejected.Id, idToken);
                    }
                    else
                    {
                        // Check if this is a sent rejected invitation
                        var sentRejected = await _firestoreService.GetSentRejectedInvitationsAsync(userId, idToken);
                        var sentRejectedInv = sentRejected.FirstOrDefault(i => i.ToUserId == careTaker.UserId);
                        if (sentRejectedInv != null)
                        {
                            success = await _firestoreService.DeleteCaregiverInvitationAsync(sentRejectedInv.Id, idToken);
                        }
                    }
                }
            }
            else
            {
                // Otherwise (accepted), remove from caretakers list
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Removing accepted caretaker {careTaker.UserId}");
                success = await _firestoreService.RemoveCaretakerAsync(userId, careTaker.UserId, idToken);
                
                // If caretaker was successfully removed, also delete the invitation from database
                if (success && _acceptedCareTakerInvitationIds.TryGetValue(careTaker.UserId, out var invitationId))
                {
                    System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Deleting invitation {invitationId} for removed caretaker");
                    var deleteSuccess = await _firestoreService.DeleteCaregiverInvitationAsync(invitationId, idToken);
                    System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Invitation deletion result: {deleteSuccess}");
                    
                    // Remove from dictionary
                    _acceptedCareTakerInvitationIds.Remove(careTaker.UserId);
                }
            }

            if (success)
            {
                CareTakers.Remove(careTaker);
                
                if (CareTakers.Count == 0)
                {
                    NoCareTakersLabel.IsVisible = true;
                }

                await DisplayAlert("Success", "Caretaker has been removed", "OK");
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to remove caretaker", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
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

    private async Task<string?> GetInvitationIdForCaretakerAsync(string caregiverId, string caretakerId, string idToken)
    {
        try
        {
            // Get all accepted invitations received by caregiver from this caretaker
            var acceptedInvitations = await _firestoreService.GetAllReceivedInvitationsAsync(caregiverId, idToken);
            
            var invitation = acceptedInvitations.FirstOrDefault(i => 
                i.FromUserId == caretakerId && 
                i.ToUserId == caregiverId && 
                i.Status == "accepted");
            
            return invitation?.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetInvitationIdForCaretakerAsync] Error: {ex.Message}");
            return null;
        }
    }
}
