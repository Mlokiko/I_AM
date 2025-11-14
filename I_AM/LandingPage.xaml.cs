namespace I_AM
{
    public partial class LandingPage : ContentPage
    {
        public LandingPage()
        {
            InitializeComponent();
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
