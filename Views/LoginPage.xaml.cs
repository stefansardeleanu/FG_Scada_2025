using FG_Scada_2025.ViewModels;

namespace FG_Scada_2025.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Focus on username field when page appears
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UsernameEntry.Focus();
        });
    }
}