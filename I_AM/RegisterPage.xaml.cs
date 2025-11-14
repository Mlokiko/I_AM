namespace I_AM;

public partial class RegisterPage : ContentPage
{
    public List<string> SexOptions { get; set; }
    public RegisterPage()
	{
		InitializeComponent();

        SexOptions = new List<string>
        {
            "Mê¿czyzna",
            "Kobieta",
            "Inne",
            "Nie chcê podawaæ"
        };
        this.BindingContext = this;
    }
    private async void OnRegisterButtonClicked(object sender, EventArgs e)
    {
        // This is the "magic" navigation line.
        // The "//" prefix tells the app to replace the
        // entire navigation stack with the new page.
        // This stops the user from pressing "back" and returning
        // to the Login page.
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}