using System.Windows.Input;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;

namespace FG_Scada_2025.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly NavigationService _navigationService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoginEnabled = true;

        public LoginViewModel(DataService dataService, NavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;
            Title = "FG Scada 2025 - Login";

            LoginCommand = new Command(async () => await LoginAsync(), () => IsLoginEnabled);
        }

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                UpdateLoginEnabled();
                ClearErrorMessage();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                UpdateLoginEnabled();
                ClearErrorMessage();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoginEnabled
        {
            get => _isLoginEnabled;
            set
            {
                SetProperty(ref _isLoginEnabled, value);
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }

        public ICommand LoginCommand { get; }

        private async Task LoginAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            IsLoginEnabled = false;
            ErrorMessage = string.Empty;

            try
            {
                // Initialize data service if not already done
                await _dataService.InitializeAsync();

                // Validate user credentials
                var user = _dataService.ValidateUser(Username, Password);

                if (user != null)
                {
                    // Store current user in preferences
                    Preferences.Set("CurrentUserId", user.Id);
                    Preferences.Set("CurrentUserName", user.Username);
                    Preferences.Set("CurrentUserRole", user.Role.ToString());

                    // Navigate to Romania Map
                    await _navigationService.NavigateToAsync("//RomaniaMapPage");
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                IsLoginEnabled = true;
            }
        }

        private void UpdateLoginEnabled()
        {
            IsLoginEnabled = !string.IsNullOrWhiteSpace(Username) &&
                           !string.IsNullOrWhiteSpace(Password) &&
                           !IsBusy;
        }

        private void ClearErrorMessage()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ErrorMessage = string.Empty;
        }
    }
}