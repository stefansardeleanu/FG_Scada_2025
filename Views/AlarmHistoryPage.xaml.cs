using FG_Scada_2025.ViewModels;

namespace FG_Scada_2025.Views;

public partial class AlarmHistoryPage : ContentPage
{
    private readonly AlarmHistoryViewModel _viewModel;

    public AlarmHistoryPage(AlarmHistoryViewModel viewModel)
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