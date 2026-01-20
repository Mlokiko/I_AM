using System.Collections.ObjectModel;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.CareGiver;

public partial class ManageCareTakersPage : ContentPage
{
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;
    public ObservableCollection<CaregiverInfo> CareTakers { get; set; }
    
    // Dictionary to track invitation IDs for accepted careTakers
    private Dictionary<string, string> _acceptedCareTakerInvitationIds = new();

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

            // 1. Get list of careTakers that invited this caregiver
            System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: Calling GetPendingInvitationsAsync");
            var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);

            System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Retrieved {pendingInvitations.Count} pending invitations");

            // 2. Add pending invitations from careTakers
            foreach (var invitation in pendingInvitations)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageCareTakersPage] LoadCareTakersAsync: Adding pending invitation from {invitation.FromUserName}");
                CareTakers.Add(new CaregiverInfo
                {
                    UserId = invitation.FromUserId,
                    Email = invitation.FromUserName,
                    FirstName = invitation.FromUserName.Split(' ').FirstOrDefault() ?? invitation.FromUserName,
                    LastName = invitation.FromUserName.Split(' ').Length > 1 ? invitation.FromUserName.Split(' ').Last() : string.Empty,
                    Status = "pending",
                    AddedAt = invitation.CreatedAt
                });
            }

            if (CareTakers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[ManageCareTakersPage] LoadCareTakersAsync: No careTakers found");
                NoCareTakersLabel.IsVisible = true;
            }
            else
            {
                NoCareTakersLabel.IsVisible = false;
            }
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

    private async void OnRemoveCareTakerClicked(object sender, EventArgs e)
    {
        if (sender is not Button button) return;

        var careTaker = button.BindingContext as CaregiverInfo;
        if (careTaker == null) return;

        var result = await DisplayAlert(
            "Confirmation",
            $"Are you sure you want to remove {careTaker.FirstName} {careTaker.LastName}?",
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

            // If status is "pending", delete the invitation
            if (careTaker.Status == "pending")
            {
                System.Diagnostics.Debug.WriteLine($"[OnRemoveCareTakerClicked] Deleting {careTaker.Status} invitation from {careTaker.Email}");
                // First need to find the invitation ID
                var pendingInvitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);
                var invitation = pendingInvitations.FirstOrDefault(i => i.FromUserId == careTaker.UserId);
                if (invitation != null)
                {
                    success = await _firestoreService.DeleteCaregiverInvitationAsync(invitation.Id, idToken);
                }
            }

            if (success)
            {
                CareTakers.Remove(careTaker);
                
                if (CareTakers.Count == 0)
                {
                    NoCareTakersLabel.IsVisible = true;
                }

                await DisplayAlert("Success", "CareTaker was removed", "OK");
                // Reload careTakers list after removal
                await LoadCareTakersAsync();
            }
            else
            {
                await DisplayAlert("Error", "Unable to remove careTaker", "OK");
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
}
