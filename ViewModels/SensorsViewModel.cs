using System.Collections.ObjectModel;
using System.Windows.Input;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;
using FG_Scada_2025.Helpers;

namespace FG_Scada_2025.ViewModels
{
    [QueryProperty(nameof(SiteId), "SiteId")]
    [QueryProperty(nameof(SiteName), "SiteName")]
    [QueryProperty(nameof(SiteDisplayName), "SiteDisplayName")]
    public partial class SensorsViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly RealTimeDataService _realTimeDataService;
        private readonly AutodiscoveryService _autodiscoveryService;
        private readonly NavigationService _navigationService;

        private Site? _site;
        private ObservableCollection<Sensor> _sensors = new();
        private string _lastUpdate = string.Empty;
        private bool _isRealTimeEnabled = true;
        private string _sensorSummary = string.Empty;

        // Navigation parameters
        public string SiteId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string SiteDisplayName { get; set; } = string.Empty;
        public string CountyName { get; set; } = string.Empty;

        public SensorsViewModel(
            DataService dataService,
            RealTimeDataService realTimeDataService,
            AutodiscoveryService autodiscoveryService,
            NavigationService navigationService)
        {
            _dataService = dataService;
            _realTimeDataService = realTimeDataService;
            _autodiscoveryService = autodiscoveryService;
            _navigationService = navigationService;

            BackCommand = new Command(async () => await NavigateBackAsync());
            RefreshCommand = new Command(async () => await RefreshDataAsync());
            ToggleRealTimeCommand = new Command(() => ToggleRealTime());

            // Subscribe to autodiscovery events
            _autodiscoveryService.SensorDiscovered += OnSensorDiscovered;
            _autodiscoveryService.SiteUpdated += OnSiteUpdated;
        }

        #region Properties

        public Site? Site
        {
            get => _site;
            set => SetProperty(ref _site, value);
        }

        public ObservableCollection<Sensor> Sensors
        {
            get => _sensors;
            set => SetProperty(ref _sensors, value);
        }

        public string LastUpdate
        {
            get => _lastUpdate;
            set => SetProperty(ref _lastUpdate, value);
        }

        public bool IsRealTimeEnabled
        {
            get => _isRealTimeEnabled;
            set => SetProperty(ref _isRealTimeEnabled, value);
        }

        public string SensorSummary
        {
            get => _sensorSummary;
            set => SetProperty(ref _sensorSummary, value);
        }

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleRealTimeCommand { get; }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"Initializing SensorsViewModel for Site: {SiteId}");

                // Load site configuration
                await LoadSiteConfigurationAsync();

                // Start real-time updates
                IsRealTimeEnabled = true;

                System.Diagnostics.Debug.WriteLine($"SensorsViewModel initialized: {Sensors.Count} sensors");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing sensors: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSiteConfigurationAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SiteId))
                {
                    System.Diagnostics.Debug.WriteLine("SiteId is empty");
                    return;
                }

                // Get site by ID (int conversion for new structure)
                if (int.TryParse(SiteId, out int siteID))
                {
                    Site = _dataService.GetSiteByID(siteID);
                }
                else
                {
                    // Fallback for string-based site lookup
                    Site = await _dataService.GetSiteAsync(SiteId);
                }

                if (Site == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Site {SiteId} not found in configuration");
                    return;
                }

                // Load sensors from site (will be populated by autodiscovery)
                Sensors.Clear();
                if (Site.Sensors != null)
                {
                    foreach (var sensor in Site.Sensors.OrderBy(s => s.Tag))
                    {
                        Sensors.Add(sensor);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Loaded site {Site.DisplayName} with {Sensors.Count} sensors");
                UpdateLastUpdateTime();
                UpdateSensorSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading site configuration: {ex.Message}");
            }
        }

        #endregion

        #region Real-time Updates & Autodiscovery

        private void OnSensorDiscovered(object? sender, SensorDiscoveredEventArgs e)
        {
            // Only handle sensors for our current site
            if (Site == null || e.Site.SiteID != Site.SiteID)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Check if sensor already exists in our collection
                    var existingSensor = Sensors.FirstOrDefault(s =>
                        s.Tag == e.Sensor.Tag && s.Id == e.Sensor.Id);

                    if (existingSensor == null)
                    {
                        // Find correct position to insert (sorted by Tag name)
                        int insertIndex = GetSortedInsertIndex(e.Sensor.Tag);
                        Sensors.Insert(insertIndex, e.Sensor);

                        System.Diagnostics.Debug.WriteLine($"🆕 Added new sensor to UI: {e.Sensor.Tag} (Status: {e.Sensor.CurrentValue.Status})");
                        UpdateLastUpdateTime();
                        UpdateSensorSummary();
                    }
                    else
                    {
                        // Update existing sensor status
                        var oldStatus = existingSensor.CurrentValue.Status;
                        existingSensor.CurrentValue.Status = e.Sensor.CurrentValue.Status;
                        existingSensor.CurrentValue.ProcessValue = e.Sensor.CurrentValue.ProcessValue;
                        existingSensor.CurrentValue.Timestamp = e.Sensor.CurrentValue.Timestamp;

                        if (oldStatus != e.Sensor.CurrentValue.Status)
                        {
                            System.Diagnostics.Debug.WriteLine($"🔄 Sensor {e.Sensor.Tag} status changed: {oldStatus} → {e.Sensor.CurrentValue.Status}");
                            UpdateSensorSummary();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding discovered sensor to UI: {ex.Message}");
                }
            });
        }

        private void OnSiteUpdated(object? sender, SiteUpdatedEventArgs e)
        {
            // Only handle updates for our current site
            if (Site == null || e.Site.SiteID != Site.SiteID)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Update site reference
                    Site = e.Site;
                    UpdateLastUpdateTime();
                    UpdateSensorSummary();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling site update: {ex.Message}");
                }
            });
        }

        private int GetSortedInsertIndex(string tagName)
        {
            for (int i = 0; i < Sensors.Count; i++)
            {
                if (string.Compare(Sensors[i].Tag, tagName, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    return i;
                }
            }
            return Sensors.Count; // Add at end if no larger element found
        }

        private void UpdateLastUpdateTime()
        {
            LastUpdate = DateTime.Now.ToString("HH:mm:ss");
        }

        private void UpdateSensorSummary()
        {
            if (Sensors?.Count > 0)
            {
                var (normal, alarm, fault, disabled) = StatusHelper.GetSensorCounts(Sensors);
                SensorSummary = $"Normal: {normal} | Alarm: {alarm} | Fault: {fault} | Disabled: {disabled}";
            }
            else
            {
                SensorSummary = "No sensors available";
            }
        }

        #endregion

        #region Commands

        private async Task NavigateBackAsync()
        {
            try
            {
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating back: {ex.Message}");
            }
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                await LoadSiteConfigurationAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing data: {ex.Message}");
            }
        }

        private void ToggleRealTime()
        {
            IsRealTimeEnabled = !IsRealTimeEnabled;
            System.Diagnostics.Debug.WriteLine($"Real-time updates: {(IsRealTimeEnabled ? "Enabled" : "Disabled")}");
        }

        #endregion

        // Clean up event subscriptions when the ViewModel is no longer needed
        ~SensorsViewModel()
        {
            // Unsubscribe from events
            if (_autodiscoveryService != null)
            {
                _autodiscoveryService.SensorDiscovered -= OnSensorDiscovered;
                _autodiscoveryService.SiteUpdated -= OnSiteUpdated;
            }
        }
    }
}