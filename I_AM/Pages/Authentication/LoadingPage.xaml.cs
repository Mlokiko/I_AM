namespace I_AM.Pages.Authentication
{
    public partial class LoadingPage : ContentPage
    {
        private bool _isAnimating = true;
        private CancellationTokenSource? _animationCts;

        public LoadingPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("?? LoadingPage created");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("?? LoadingPage appearing");
            _isAnimating = true;
            _animationCts = new CancellationTokenSource();
            
            // Start animation on a background task, don't await it
            AnimateSplashImageAsync(_animationCts.Token);
        }

        private void AnimateSplashImageAsync(CancellationToken cancellationToken)
        {
            // Fire and forget - don't await
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_isAnimating && !cancellationToken.IsCancellationRequested)
                    {
                        // Scale up
                        await SplashImage.ScaleTo(1.1, 1000, Easing.SinInOut);
                        // Scale down
                        await SplashImage.ScaleTo(1.0, 1000, Easing.SinInOut);
                    }
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("?? Animation cancelled");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Animation error: {ex.Message}");
                }
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            System.Diagnostics.Debug.WriteLine("?? LoadingPage disappearing - stopping animation");
            _isAnimating = false;
            _animationCts?.Cancel();
            _animationCts?.Dispose();
        }
    }
}




