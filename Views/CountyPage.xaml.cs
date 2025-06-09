using FG_Scada_2025.ViewModels;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Views;

public partial class CountyPage : ContentPage
{
    private readonly CountyViewModel _viewModel;

    public CountyPage(CountyViewModel viewModel)
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

    private async void OnSiteTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.BindingContext is Site site)
        {
            await _viewModel.OnSiteTappedAsync(site);
        }
    }
}