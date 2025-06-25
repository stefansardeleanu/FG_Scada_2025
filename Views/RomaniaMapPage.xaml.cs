using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using FG_Scada_2025.ViewModels;
using FG_Scada_2025.Helpers;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;

namespace FG_Scada_2025.Views;

public partial class RomaniaMapPage : ContentPage
{
    private readonly RomaniaMapViewModel _viewModel;

    // County paths from SVG
    private Dictionary<string, (SKPath Path, SKPoint Center, string Name)> _countyPaths =
        new Dictionary<string, (SKPath, SKPoint, string)>();

    // For transformation
    private float _scale = 1.0f;
    private float _offsetX = 0;
    private float _offsetY = 0;

    // SVG viewbox dimensions (adjust based on your SVG)
    private readonly float _svgWidth = 1000;
    private readonly float _svgHeight = 704;

    // Selected county for highlighting
    private string? _selectedCounty = null;

    public RomaniaMapPage(RomaniaMapViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Initialize ViewModel
        await _viewModel.InitializeAsync();

        // Load SVG data
        await LoadSvgDataAsync();

        // Refresh canvas
        MapCanvas.InvalidateSurface();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Clean up ViewModel
        _viewModel.OnDisappearing();
    }

    private async Task LoadSvgDataAsync()
    {
        try
        {
            Console.WriteLine("Starting to load SVG data...");
            _countyPaths = await SVGHelper.ParseRomaniaMapAsync();
            Console.WriteLine($"Loaded {_countyPaths.Count} county paths");

            if (_countyPaths.Count == 0)
            {
                await DisplayAlert("Warning", "No county data loaded from SVG file", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading SVG data: {ex.Message}");
            await DisplayAlert("Error", $"Failed to load map data: {ex.Message}", "OK");
        }
    }

    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        // Clear canvas
        canvas.Clear(SKColors.White);

        if (_countyPaths.Count == 0)
        {
            DrawLoadingMessage(canvas, info);
            return;
        }

        // Calculate transformation
        CalculateTransformation(info);

        // Apply transformation
        canvas.Save();
        canvas.Translate(_offsetX, _offsetY);
        canvas.Scale(_scale);

        // Draw counties
        DrawCounties(canvas);

        canvas.Restore();
    }

    private void DrawLoadingMessage(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.Gray,
            TextSize = 24,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawText("Loading Romania Map...", info.Width / 2, info.Height / 2, paint);
    }

    private void CalculateTransformation(SKImageInfo info)
    {
        float scaleX = info.Width / _svgWidth;
        float scaleY = info.Height / _svgHeight;
        _scale = Math.Min(scaleX, scaleY) * 0.9f; // 90% to provide margins

        _offsetX = (info.Width - (_svgWidth * _scale)) / 2;
        _offsetY = (info.Height - (_svgHeight * _scale)) / 2;
    }

    private void DrawCounties(SKCanvas canvas)
    {
        foreach (var countyPath in _countyPaths)
        {
            string countyId = countyPath.Key;
            var (path, center, name) = countyPath.Value;

            // Get county from ViewModel
            var county = _viewModel.Counties.FirstOrDefault(c => c.Id == countyId);
            bool hasAlarm = false;
            bool hasFault = false;

            if (county != null)
            {
                var (alarm, fault) = StatusHelper.GetCountyStatus(county.Sites);
                hasAlarm = alarm;
                hasFault = fault;
            }

            // Determine fill color
            bool isSelected = countyId == _selectedCounty;
            SKColor fillColor = SVGHelper.GetCountyFillColor(hasAlarm, hasFault, isSelected);

            // Draw county fill
            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = fillColor,
                IsAntialias = true
            })
            {
                canvas.DrawPath(path, paint);
            }

            // Draw county outline
            using (var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
                StrokeWidth = 0.5f,
                IsAntialias = true
            })
            {
                canvas.DrawPath(path, paint);
            }

            // Draw county name
            using (var paint = new SKPaint
            {
                TextSize = 12,
                Color = SKColors.Black,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            })
            {
                canvas.DrawText(name, center.X, center.Y, paint);
            }

            // Draw status indicators if county has sites
            if (county != null && county.Sites.Count > 0)
            {
                SVGHelper.DrawStatusIndicators(canvas, center, hasAlarm, hasFault);
            }
        }
    }

    private void OnCanvasViewTouch(object sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            HandleTouchEvent(e.Location);
        }

        e.Handled = true;
    }

    private void HandleTouchEvent(SKPoint touchPoint)
    {
        // Convert to SVG coordinates
        float svgX = (touchPoint.X - _offsetX) / _scale;
        float svgY = (touchPoint.Y - _offsetY) / _scale;

        // Check which county was touched
        string? touchedCountyId = null;

        foreach (var countyPath in _countyPaths)
        {
            if (countyPath.Value.Path.Contains(svgX, svgY))
            {
                touchedCountyId = countyPath.Key;
                break;
            }
        }

        Console.WriteLine($"Touch detected at: {touchPoint.X}, {touchPoint.Y}");
        Console.WriteLine($"SVG coordinates: {svgX}, {svgY}");
        Console.WriteLine($"Touched county: {touchedCountyId}");

        if (touchedCountyId != null)
        {
            // Update selected county
            _selectedCounty = touchedCountyId;
            MapCanvas.InvalidateSurface();

            Console.WriteLine($"Navigating to county: {touchedCountyId}");

            // Navigate to county page using your existing method
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _viewModel.OnCountyTappedAsync(touchedCountyId);
                    Console.WriteLine("Navigation completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Navigation error: {ex.Message}");
                    await DisplayAlert("Navigation Error", ex.Message, "OK");
                }
            });
        }
        else
        {
            Console.WriteLine("No county found at touch location");
        }
    }

    // REMOVED: OnTestMqttClicked method - replaced by MQTT button in header
    // The MQTT functionality is now handled by the ToggleMqttConnectionCommand in the ViewModel
}