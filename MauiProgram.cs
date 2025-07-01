using FG_Scada_2025.Services;
using FG_Scada_2025.ViewModels;
using FG_Scada_2025.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FG_Scada_2025;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register services in correct dependency order
        RegisterCoreServices(builder.Services);
        RegisterDataServices(builder.Services);
        RegisterMqttServices(builder.Services);
        RegisterAutodiscoveryServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);

        return builder.Build();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        // Core navigation service (no dependencies)
        services.AddSingleton<NavigationService>();

        // Data initializer interface
        services.AddSingleton<IDataInitializer, DataInitializer>();
    }

    private static void RegisterDataServices(IServiceCollection services)
    {
        // Data service for configuration management (no dependencies)
        services.AddSingleton<DataService>();
    }

    private static void RegisterMqttServices(IServiceCollection services)
    {
        // MQTT connection management
        services.AddSingleton<ConnectionManager>();

        // Real-time data service (depends on ConnectionManager)
        services.AddSingleton<RealTimeDataService>();
    }

    private static void RegisterAutodiscoveryServices(IServiceCollection services)
    {
        // Autodiscovery service (depends on DataService and RealTimeDataService)
        services.AddSingleton<AutodiscoveryService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        // Register all ViewModels as transient (new instance each time)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RomaniaMapViewModel>();
        services.AddTransient<CountyViewModel>();
        services.AddTransient<SiteViewModel>();
        services.AddTransient<SensorsViewModel>();
        services.AddTransient<AlarmHistoryViewModel>();
        services.AddTransient<ConnectionTestViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        // Register all Pages as transient (new instance each time)
        services.AddTransient<LoginPage>();
        services.AddTransient<RomaniaMapPage>();
        services.AddTransient<CountyPage>();
        services.AddTransient<SitePage>();
        services.AddTransient<SensorsPage>();
        services.AddTransient<AlarmHistoryPage>();
        services.AddTransient<ConnectionTestPage>();
    }
}

// Helper interface for data initialization
public interface IDataInitializer
{
    Task InitializeAsync();
}

// Data initializer implementation
public class DataInitializer : IDataInitializer
{
    private readonly DataService _dataService;

    public DataInitializer(DataService dataService)
    {
        _dataService = dataService;
    }

    public async Task InitializeAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Initializing DataService...");
            await _dataService.InitializeAsync();
            System.Diagnostics.Debug.WriteLine("✅ DataService initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error initializing DataService: {ex.Message}");
            throw;
        }
    }
}