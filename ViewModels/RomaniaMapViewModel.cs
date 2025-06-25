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
        private readonly RealTimeDataService _realTimeDataService;

        private User? _currentUser;
        private ObservableCollection<County> _counties = new ObservableCollection<County>();
        private string _userWelcomeText = string.Empty;

        // MQTT-related properties
        private bool _isConnectedToMqtt = false;
        private string _lastUpdateTime = "Never";
        private int _connectedSitesCount = 0;
        private Timer? _statusUpdateTimer;

        public RomaniaMapViewModel(DataService dataService, NavigationService navigationService, RealTimeDataService realTimeDataService)
        {
            _dataService = dataService;
            _navigationService = navigationService;
            _realTimeDataService = realTimeDataService;
            Title = "Romania Fire Detection Map";

            LogoutCommand = new Command(async () => await LogoutAsync());
            RefreshCommand = new Command(async () => await RefreshDataAsync());

            // NEW: MQTT Connection Command
            ToggleMqttConnectionCommand = new Command(async () => await ToggleMqttConnectionAsync());

            // Subscribe to MQTT connection status changes
            _realTimeDataService.SiteStatusChanged += OnSiteStatusChanged;

            System.Diagnostics.Debug.WriteLine("RomaniaMapViewModel created with MQTT integration");
        }

        #region Properties

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

        // NEW: MQTT Properties
        public bool IsConnectedToMqtt
        {
            get => _isConnectedToMqtt;
            set => SetProperty(ref _isConnectedToMqtt, value);
        }

        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public int ConnectedSitesCount
        {
            get => _connectedSitesCount;
            set => SetProperty(ref _connectedSitesCount, value);
        }

        #endregion

        #region Commands

        public ICommand LogoutCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleMqttConnectionCommand { get; }  // NEW

        #endregion

        #region Initialization

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

                // Update MQTT connection status
                UpdateMqttConnectionStatus();

                // Start status update timer
                StartStatusUpdateTimer();

                System.Diagnostics.Debug.WriteLine($"Romania Map initialized with {Counties.Count} counties");
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

        #endregion

        #region Navigation (Your existing method)

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

        #endregion

        #region MQTT Connection Management (NEW)

        private async Task ToggleMqttConnectionAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                if (IsConnectedToMqtt)
                {
                    // Disconnect from MQTT
                    System.Diagnostics.Debug.WriteLine("🔌 Disconnecting from MQTT...");
                    await _realTimeDataService.DisconnectFromMqttAsync();
                    IsConnectedToMqtt = false;
                    LastUpdateTime = "Disconnected";
                    ConnectedSitesCount = 0;
                    System.Diagnostics.Debug.WriteLine("✅ MQTT Disconnected");
                }
                else
                {
                    // Connect to MQTT
                    System.Diagnostics.Debug.WriteLine("🔌 Connecting to MQTT...");
                    bool connected = await _realTimeDataService.ConnectToMqttAsync();

                    if (connected)
                    {
                        IsConnectedToMqtt = true;
                        LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                        System.Diagnostics.Debug.WriteLine("✅ MQTT Connected successfully");

                        // Show success message
                        await Application.Current?.MainPage?.DisplayAlert("Success", "Connected to MQTT broker successfully!", "OK");
                    }
                    else
                    {
                        IsConnectedToMqtt = false;
                        System.Diagnostics.Debug.WriteLine("❌ MQTT Connection failed");

                        // Show error message
                        await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to connect to MQTT broker. Please check your connection.", "OK");
                    }
                }

                UpdateMqttConnectionStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling MQTT connection: {ex.Message}");
                await Application.Current?.MainPage?.DisplayAlert("Error", $"Connection error: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateMqttConnectionStatus()
        {
            // Update connection status from the service
            IsConnectedToMqtt = _realTimeDataService.IsConnectedToMqtt;

            if (IsConnectedToMqtt)
            {
                LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");

                // Count active sites
                var activeSites = _realTimeDataService.GetAllSites().Count(s => s.IsOnline);
                ConnectedSitesCount = activeSites;
            }
            else
            {
                LastUpdateTime = "Not Connected";
                ConnectedSitesCount = 0;
            }

            System.Diagnostics.Debug.WriteLine($"MQTT Status: {(IsConnectedToMqtt ? "Connected" : "Disconnected")}, Sites: {ConnectedSitesCount}");
        }

        private void StartStatusUpdateTimer()
        {
            // Update status every 10 seconds
            _statusUpdateTimer = new Timer(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (IsConnectedToMqtt)
                    {
                        LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");

                        // Update active sites count
                        var activeSites = _realTimeDataService.GetAllSites().Count(s => s.IsOnline);
                        ConnectedSitesCount = activeSites;
                    }
                });
            }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        private void OnSiteStatusChanged(object? sender, SiteStatusChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Update connected sites count when site status changes
                if (IsConnectedToMqtt)
                {
                    var activeSites = _realTimeDataService.GetAllSites().Count(s => s.IsOnline);
                    ConnectedSitesCount = activeSites;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                }
            });
        }

        #endregion

        #region Existing Methods

        private async Task RefreshDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                await LoadCountiesAsync();
                UpdateMqttConnectionStatus();
                System.Diagnostics.Debug.WriteLine("Romania Map data refreshed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                // Disconnect from MQTT before logout
                if (IsConnectedToMqtt)
                {
                    await _realTimeDataService.DisconnectFromMqttAsync();
                }

                // Dispose timer
                _statusUpdateTimer?.Dispose();
                _statusUpdateTimer = null;

                _dataService.ClearCurrentUser();
                await _navigationService.NavigateToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        public void OnDisappearing()
        {
            _statusUpdateTimer?.Dispose();
            _statusUpdateTimer = null;
        }

        #endregion
    }
}