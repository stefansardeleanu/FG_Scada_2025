using FG_Scada_2025.Services;

namespace FG_Scada_2025;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        base.OnStart();

        // Initialize real-time data service after the app has started
        try
        {
            // Get the service from DI container
            var serviceProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IServiceProvider>(Handler.MauiContext.Services);
            var realTimeService = serviceProvider.GetService<RealTimeDataService>();

            if (realTimeService != null)
            {
                await realTimeService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Real-time data service initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Real-time data service not found in DI container");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing real-time service: {ex.Message}");
        }
    }
}