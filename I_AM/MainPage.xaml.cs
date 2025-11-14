namespace I_AM
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
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
        private async void OnManageOwnAccountButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ManageOwnAccountPage));
        }
    }
}
