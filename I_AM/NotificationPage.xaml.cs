using System.Collections.ObjectModel;

namespace I_AM;

public partial class NotificationPage : ContentPage
{
    public ObservableCollection<Notification> Notifications { get; set; }

    public NotificationPage()
    {
        InitializeComponent();
        Notifications = new ObservableCollection<Notification>();
        BindingContext = this;
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

            // Symuluj ³adowanie danych
            await Task.Delay(500);

            // Tutaj mo¿esz dodaæ logikê ³adowania powiadomieñ z serwera/bazy danych
            // Na razie jest pusta lista - brak powiadomieñ

            if (Notifications.Count == 0)
            {
                // Poka¿ pusty stan
                NotificationsCollectionView.IsVisible = false;
                EmptyStateLayout.IsVisible = true;
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

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadNotificationsAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

/// <summary>
/// Model powiadomienia
/// </summary>
public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; } = false;
    public string Type { get; set; } = "info"; // info, warning, error, success
}
