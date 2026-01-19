namespace I_AM
{
    public partial class LandingPage : ContentPage
    {
        public LandingPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetLogoBasedOnTheme();
            
            // Listen for theme changes
            Application.Current!.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Application.Current!.RequestedThemeChanged -= OnRequestedThemeChanged;
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            SetLogoBasedOnTheme();
        }

        private void SetLogoBasedOnTheme()
        {
            var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Unspecified;
            LogoImage.Source = currentTheme == AppTheme.Dark ? "logo_light.svg" : "logo_dark.svg";
        }

        private async void OnLoginButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
        private async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(RegisterPage));
        }
        private async void OnSpanTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(InformationPage));
        }
    }
}

