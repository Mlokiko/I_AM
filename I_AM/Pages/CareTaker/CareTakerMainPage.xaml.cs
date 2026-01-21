using I_AM.Services;
using I_AM.Pages.Authentication;
using I_AM.Pages.CareTaker;
using I_AM.Pages.Main;

namespace I_AM.Pages.CareTaker
{
    public partial class CareTakerMainPage : ContentPage
    {
        private readonly IAuthenticationService _authService;

        public CareTakerMainPage()
        {
            InitializeComponent();
            _authService = ServiceHelper.GetService<IAuthenticationService>();
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        private async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegisterPage));
        }

        private async void OnManageOwnAccountButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ManageOwnAccountPage));
        }

        private async void OnManagCaregiversButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ManageCaregiversPage));
        }

        private async void OnNotificationsButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(NotificationPage));
        }

        private async void OnCalendarButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CalendarPage));
        }
        private async void OnDailyActivityButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(DailyActivityPage));
        }

        private async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Potwierdzenie", "Czy na pewno chcesz się wylogować?", "Tak", "Nie");
            
            if (result)
            {
                await _authService.LogoutAsync();
                // Use relative route instead of absolute global route
                await Shell.Current.GoToAsync(nameof(LandingPage));
            }
        }
        private async void OnLabelTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(InformationPage));
        }
    }
}



