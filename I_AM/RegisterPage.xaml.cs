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
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}