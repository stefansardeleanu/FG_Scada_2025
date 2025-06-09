using FG_Scada_2025.ViewModels;

namespace FG_Scada_2025.Views;

public partial class SensorsPage : ContentPage
{
    private readonly SensorsViewModel _viewModel;

    public SensorsPage(SensorsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}