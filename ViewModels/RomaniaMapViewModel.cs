using System.Collections.ObjectModel;
using System.Windows.Input;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;
using FG_Scada_2025.Helpers;
using System.Text.Json;

namespace FG_Scada_2025.ViewModels
{
    public class RomaniaMapViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly NavigationService _navigationService;

        private User? _currentUser;
        private ObservableCollection<County> _counties = new ObservableCollection<County>();
        private string _userWelcomeText = string.Empty;

        public RomaniaMapViewModel(DataService dataService, NavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;
            Title = "Romania Fire Detection Map";

            LogoutCommand = new Command(async () => await LogoutAsync());
            RefreshCommand = new Command(async () => await RefreshDataAsync());
        }

        public ObservableCollection<County> Counties
        {
            get => _counties;
            set => SetProperty(ref _counties, value);
        }

        public string UserWelcomeText
        {
            get => _userWelcomeText;
            set => SetProperty(ref _userWelcomeText, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // Get current user
                _currentUser = _dataService.GetCurrentUser();
                if (_currentUser == null)
                {
                    await _navigationService.NavigateToAsync("//LoginPage");
                    return;
                }

                // Set welcome text
                UserWelcomeText = $"Welcome, {_currentUser.Username} ({_currentUser.Role})";

                // Load counties based on user permissions
                await LoadCountiesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Romania Map: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCountiesAsync()
        {
            try
            {
                var allCounties = await _dataService.GetCountiesAsync();
                var allowedCounties = _dataService.GetAllowedCounties(_currentUser!);

                // Clear existing counties
                Counties.Clear();

                // Add allowed counties with mock status data
                var random = new Random();
                foreach (var county in allowedCounties)
                {
                    // Generate mock status for demonstration
                    var sites = await _dataService.GetSitesForCountyAsync(county.Id);
                    foreach (var site in sites)
                    {
                        // Generate random status for demo
                        site.Status.HasAlarm = random.Next(10) < 3; // 30% chance
                        site.Status.HasFault = random.Next(20) < 2; // 10% chance
                        site.Status.LastUpdate = DateTime.Now;
                    }

                    Counties.Add(county);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading counties: {ex.Message}");
            }
        }

        public async Task OnCountyTappedAsync(string countyId)
        {
            try
            {
                Console.WriteLine($"OnCountyTappedAsync called with: {countyId}");

                var county = Counties.FirstOrDefault(c => c.Id == countyId);
                if (county == null)
                {
                    Console.WriteLine($"County not found: {countyId}");
                    Console.WriteLine($"Available counties: {string.Join(", ", Counties.Select(c => c.Id))}");
                    return;
                }

                Console.WriteLine($"Found county: {county.Name}");

                // Navigate to county page with parameters
                var parameters = new Dictionary<string, object>
                {
                    ["CountyId"] = countyId,
                    ["CountyName"] = county.Name,
                    ["CountyDisplayName"] = county.DisplayName
                };

                Console.WriteLine($"Navigating with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");

                await _navigationService.NavigateToAsync("CountyPage", parameters);

                Console.WriteLine("Navigation call completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to county: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task RefreshDataAsync()
        {
            await LoadCountiesAsync();
        }

        private async Task LogoutAsync()
        {
            try
            {
                _dataService.ClearCurrentUser();
                await _navigationService.NavigateToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
            }
        }
    }
}