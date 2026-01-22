using I_AM.Pages.Main;

namespace I_AM.Pages.CareTaker;

public partial class DailyActivityPage : ContentPage
{
    public DailyActivityPage()
    {
        InitializeComponent();
    }

    private async void OnTakeSurveyClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SurveyPage));
    }
}