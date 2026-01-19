using System.Collections.ObjectModel;
using I_AM.Models;
using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM.Pages.Main;

public partial class NotificationPage : ContentPage
{
    public ObservableCollection<NotificationItem> Notifications { get; set; }
    private readonly IAuthenticationService _authService;
    private readonly IFirestoreService _firestoreService;

    public NotificationPage()
    {
        InitializeComponent();
        Notifications = new ObservableCollection<NotificationItem>();
        BindingContext = this;
        _authService = ServiceHelper.GetService<IAuthenticationService>();
        _firestoreService = ServiceHelper.GetService<IFirestoreService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNotificationsAsync();
    }

    private async Task LoadNotificationsAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            EmptyStateLayout.IsVisible = false;
            NotificationsCollectionView.IsVisible = true;

            Notifications.Clear();

            var userId = await _authService.GetCurrentUserIdAsync();
            var idToken = await _authService.GetCurrentIdTokenAsync();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                ShowEmpty();
                return;
            }

            // Pobierz zaproszenia do opiekunów
            var invitations = await _firestoreService.GetPendingInvitationsAsync(userId, idToken);

            if (invitations.Count == 0)
            {
                ShowEmpty();
                return;
            }

            foreach (var invitation in invitations)
            {
                Notifications.Add(new NotificationItem
                {
                    Id = invitation.Id,
                    Title = $"Zaproszenie od {invitation.FromUserName}",
                    Message = $"{invitation.FromUserName} chce byæ twoim podopiecznym",
                    CreatedAt = invitation.CreatedAt,
                    Type = "caregiver_invitation",
                    InvitationId = invitation.Id,
                    CaregiverId = invitation.FromUserId,
                    CaregiverEmail = invitation.ToUserEmail
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"B³¹d podczas ³adowania powiadomieñ: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void ShowEmpty()
    {
        NotificationsCollectionView.IsVisible = false;
        EmptyStateLayout.IsVisible = true;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadNotificationsAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnAcceptButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is NotificationItem notification)
        {
            try
            {
                button.IsEnabled = false;

                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] START");
                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] InvitationId: {notification.InvitationId}");
                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] CaregiverId: {notification.CaregiverId}");

                var userId = await _authService.GetCurrentUserIdAsync();
                var idToken = await _authService.GetCurrentIdTokenAsync();

                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] Current UserId: {userId}");
                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] Has IdToken: {!string.IsNullOrEmpty(idToken)}");

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
                {
                    await DisplayAlert("B³¹d", "Nie mo¿na za³adowaæ danych u¿ytkownika", "OK");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] Calling AcceptCaregiverInvitationAsync");
                var success = await _firestoreService.AcceptCaregiverInvitationAsync(
                    userId,
                    notification.InvitationId,
                    notification.CaregiverId,
                    idToken
                );

                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] AcceptCaregiverInvitationAsync returned: {success}");

                if (success)
                {
                    await DisplayAlert("Sukces", "Zaproszenie zaakceptowane!", "OK");
                    Notifications.Remove(notification);
                    
                    if (Notifications.Count == 0)
                    {
                        ShowEmpty();
                    }
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê zaakceptowaæ zaproszenia", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OnAcceptButtonClicked] StackTrace: {ex.StackTrace}");
                await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
            }
            finally
            {
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                }
            }
        }
    }

    private async void OnRejectButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is NotificationItem notification)
        {
            try
            {
                button.IsEnabled = false;

                var userId = await _authService.GetCurrentUserIdAsync();
                var idToken = await _authService.GetCurrentIdTokenAsync();

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
                {
                    await DisplayAlert("B³¹d", "Nie mo¿na za³adowaæ danych u¿ytkownika", "OK");
                    return;
                }

                var success = await _firestoreService.RejectCaregiverInvitationAsync(
                    userId,
                    notification.InvitationId,
                    idToken
                );

                if (success)
                {
                    await DisplayAlert("Sukces", "Zaproszenie odrzucone", "OK");
                    Notifications.Remove(notification);

                    if (Notifications.Count == 0)
                    {
                        ShowEmpty();
                    }
                }
                else
                {
                    await DisplayAlert("B³¹d", "Nie uda³o siê odrzuciæ zaproszenia", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("B³¹d", $"B³¹d: {ex.Message}", "OK");
            }
            finally
            {
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                }
            }
        }
    }
}

public class NotificationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
    public string Type { get; set; } = "info"; // info, warning, error, success, caregiver_invitation
    public string InvitationId { get; set; } = string.Empty;
    public string CaregiverId { get; set; } = string.Empty;
    public string CaregiverEmail { get; set; } = string.Empty;
}

/// <summary>
/// Stary model - pozostawiony dla kompatybilnoœci
/// </summary>
public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
    public string Type { get; set; } = "info";
}
