using I_AM.Services;
using I_AM.Services.Interfaces;
using I_AM.Pages.Authentication;
using I_AM.Pages.Main;
using I_AM.Pages.CareTaker;
using I_AM.Pages.CareGiver;

namespace I_AM
{
    public partial class AppShell : Shell
    {
        private readonly IAuthenticationService _authService;
        private readonly IFirestoreService _firestoreService;

        public AppShell()
        {
            InitializeComponent();
            
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(InformationPage), typeof(InformationPage));
            Routing.RegisterRoute(nameof(ManageOwnAccountPage), typeof(ManageOwnAccountPage));
            Routing.RegisterRoute(nameof(ManageCaregiverPage), typeof(ManageCaregiverPage));
            Routing.RegisterRoute(nameof(NotificationPage), typeof(NotificationPage));
            Routing.RegisterRoute(nameof(LandingPage), typeof(LandingPage));
            Routing.RegisterRoute(nameof(CareTakerMainPage), typeof(CareTakerMainPage));
            Routing.RegisterRoute(nameof(CareGiverMainPage), typeof(CareGiverMainPage));
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
            Routing.RegisterRoute(nameof(DailyActivityPage), typeof(DailyActivityPage));
            Routing.RegisterRoute(nameof(EditCareTakerQuestionsPage), typeof(EditCareTakerQuestionsPage));

            _authService = ServiceHelper.GetService<IAuthenticationService>();
            _firestoreService = ServiceHelper.GetService<IFirestoreService>();
            
            // Navigate when shell is loaded
            Loaded += OnShellLoaded;
        }

        private async void OnShellLoaded(object sender, EventArgs e)
        {
            Loaded -= OnShellLoaded;
            
            try
            {
                // Initialize authentication service
                await _authService.InitializeAsync();
                
                System.Diagnostics.Debug.WriteLine($"📱 AppShell loaded. User authenticated: {_authService.IsUserAuthenticated}");
                
                // Navigate to the appropriate page
                await NavigateToAppropriatePageAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error in OnShellLoaded: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Public method to navigate to the appropriate page based on authentication and user role
        /// </summary>
        public async Task NavigateToAppropriatePageAsync()
        {
            // Navigate based on authentication state using absolute routing
            if (_authService.IsUserAuthenticated)
            {
                // User is authenticated - get profile to check if caregiver
                var userId = await _authService.GetCurrentUserIdAsync();
                var idToken = await _authService.GetCurrentIdTokenAsync();
                
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(idToken))
                {
                    try
                    {
                        var userProfile = await _firestoreService.GetUserProfileAsync(userId, idToken);
                        
                        if (userProfile != null)
                        {
                            if (userProfile.IsCaregiver)
                            {
                                System.Diagnostics.Debug.WriteLine($"📱 Navigating to CareGiverMainPage");
                                await Shell.Current.GoToAsync($"///{nameof(CareGiverMainPage)}", false);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"📱 Navigating to CareTakerMainPage");
                                await Shell.Current.GoToAsync($"///{nameof(CareTakerMainPage)}", false);
                            }
                        }
                        else
                        {
                            // Profile not found, navigate to LandingPage
                            System.Diagnostics.Debug.WriteLine($"📱 User profile not found, navigating to LandingPage");
                            await Shell.Current.GoToAsync($"///{nameof(LandingPage)}", false);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error fetching user profile: {ex.Message}");
                        // On error, navigate to CareTakerMainPage as default
                        await Shell.Current.GoToAsync($"///{nameof(CareTakerMainPage)}", false);
                    }
                }
                else
                {
                    // No user ID or token, navigate to LandingPage
                    System.Diagnostics.Debug.WriteLine($"📱 No user ID or token, navigating to LandingPage");
                    await Shell.Current.GoToAsync($"///{nameof(LandingPage)}", false);
                }
            }
            else
            {
                // User is not authenticated - go to LandingPage
                System.Diagnostics.Debug.WriteLine($"📱 User not authenticated, navigating to LandingPage");
                await Shell.Current.GoToAsync($"///{nameof(LandingPage)}", false);
            }
        }
    }
}


