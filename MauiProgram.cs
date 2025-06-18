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

        // Register Services
        builder.Services.AddSingleton<DataService>();
        builder.Services.AddSingleton<NavigationService>();

        // Register MQTT Services
        builder.Services.AddSingleton<ConnectionManager>();
        builder.Services.AddSingleton<RealTimeDataService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RomaniaMapViewModel>();
        builder.Services.AddTransient<CountyViewModel>();
        builder.Services.AddTransient<SiteViewModel>();
        builder.Services.AddTransient<SensorsViewModel>();
        builder.Services.AddTransient<AlarmHistoryViewModel>();
        builder.Services.AddTransient<ConnectionTestViewModel>();
        builder.Services.AddTransient<ConnectionTestViewModel>();

        // Register Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RomaniaMapPage>();
        builder.Services.AddTransient<CountyPage>();
        builder.Services.AddTransient<SitePage>();
        builder.Services.AddTransient<SensorsPage>();
        builder.Services.AddTransient<AlarmHistoryPage>();
        builder.Services.AddTransient<ConnectionTestPage>();

        return builder.Build();
    }
}