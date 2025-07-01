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

        // Initialize all services in the correct order
        try
        {
            // Get the service provider from DI container
            var serviceProvider = Handler?.MauiContext?.Services;
            if (serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Service provider is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine("🚀 Starting application initialization...");

            // 1. Initialize data service first (loads county/site configurations)
            var dataInitializer = serviceProvider.GetService<IDataInitializer>();
            if (dataInitializer != null)
            {
                await dataInitializer.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("✅ Data service initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Data initializer not found in DI container");
            }

            // 2. Initialize real-time data service (prepares MQTT but doesn't auto-connect)
            var realTimeService = serviceProvider.GetService<RealTimeDataService>();
            if (realTimeService != null)
            {
                await realTimeService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("✅ Real-time data service initialized successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Real-time data service not found in DI container");
            }

            // 3. Initialize autodiscovery service (registers for MQTT events)
            var autodiscoveryService = serviceProvider.GetService<AutodiscoveryService>();
            if (autodiscoveryService != null)
            {
                System.Diagnostics.Debug.WriteLine("✅ Autodiscovery service initialized successfully");
                System.Diagnostics.Debug.WriteLine("📡 Ready for sensor autodiscovery when MQTT connects");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Autodiscovery service not found in DI container");
            }

            System.Diagnostics.Debug.WriteLine("🎉 Application initialization completed successfully");
            System.Diagnostics.Debug.WriteLine("💡 Note: MQTT connection will be initiated manually from Romania Map page");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 FATAL ERROR during app initialization: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            // You might want to show an error dialog to the user here
            // or implement fallback initialization
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("😴 Application going to sleep");

        // Optionally disconnect MQTT when app goes to sleep to save battery
        try
        {
            var serviceProvider = Handler?.MauiContext?.Services;
            var realTimeService = serviceProvider?.GetService<RealTimeDataService>();
            if (realTimeService != null && realTimeService.IsConnectedToMqtt)
            {
                // Don't await this - just fire and forget
                _ = Task.Run(async () => await realTimeService.DisconnectFromMqttAsync());
                System.Diagnostics.Debug.WriteLine("🔌 MQTT disconnection initiated on sleep");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during sleep cleanup: {ex.Message}");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("🌅 Application resuming from sleep");

        // Note: MQTT reconnection should be handled manually by user
        // or automatically by your connection management logic
    }
}