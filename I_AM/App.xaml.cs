using I_AM.Services;
using I_AM.Services.Interfaces;

namespace I_AM
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var serviceProvider = new ServiceCollection()
                .AddSingleton<IAuthenticationStateService, AuthenticationStateService>()
                .AddSingleton<IAuthenticationService, AuthenticationService>()
                .AddSingleton<IFirestoreService, FirestoreService>()
                .BuildServiceProvider();

            ServiceHelper.Initialize(serviceProvider);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}