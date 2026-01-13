namespace I_AM.Services;

public static class ServiceHelper
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceHelper nie zosta³ zainicjalizowany");

        return _serviceProvider.GetRequiredService<T>();
    }
}