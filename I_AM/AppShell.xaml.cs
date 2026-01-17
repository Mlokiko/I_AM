using I_AM.Services;

namespace I_AM
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService _authService;

        public AppShell()
        {
            InitializeComponent();
            
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(InformationPage), typeof(InformationPage));
            Routing.RegisterRoute(nameof(ManageOwnAccountPage), typeof(ManageOwnAccountPage));
            Routing.RegisterRoute(nameof(AddCaregiverPage), typeof(AddCaregiverPage));
            Routing.RegisterRoute(nameof(NotificationPage), typeof(NotificationPage));
            Routing.RegisterRoute(nameof(LandingPage), typeof(LandingPage));
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));

            _authService = ServiceHelper.GetService<IAuthenticationService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Inicjalizuj serwis autentykacji
            await _authService.InitializeAsync();
            
            // Nawiguj na podstawie stanu autentykacji
            if (_authService.IsUserAuthenticated)
            {
                // Użytkownik jest zalogowany - idź do MainPage
                await GoToAsync($"//{nameof(MainPage)}", true);
            }
            else
            {
                // Użytkownik nie jest zalogowany - idź do LandingPage
                await GoToAsync($"//{nameof(LandingPage)}", true);
            }
        }
    }
}

