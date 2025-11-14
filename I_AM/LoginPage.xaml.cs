namespace I_AM;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}
    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        // This is the "magic" navigation line.
        // The "//" prefix tells the app to replace the
        // entire navigation stack with the new page.
        // This stops the user from pressing "back" and returning
        // to the Login page.
        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }
}