using FG_Scada_2025.ViewModels;

namespace FG_Scada_2025.Views;

public partial class SitePage : ContentPage
{
    private readonly SiteViewModel _viewModel;

    public SitePage(SiteViewModel viewModel)
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
}