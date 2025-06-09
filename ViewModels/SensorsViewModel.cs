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
    public class SensorsViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly NavigationService _navigationService;

        private string _siteId = string.Empty;
        private string _siteName = string.Empty;
        private string _siteDisplayName = string.Empty;
        private Site? _site;
        private ObservableCollection<Sensor> _sensors = new ObservableCollection<Sensor>();
        private string _lastUpdate = string.Empty;
        private bool _isRealTimeEnabled = true;

        private Timer? _refreshTimer;

        public SensorsViewModel(DataService dataService, NavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;

            BackCommand = new Command(async () => await _navigationService.GoBackAsync());
            RefreshCommand = new Command(async () => await LoadSensorsDataAsync());
            ToggleRealTimeCommand = new Command(() => ToggleRealTime());
        }

        public string SiteId
        {
            get => _siteId;
            set => SetProperty(ref _siteId, value);
        }

        public string SiteName
        {
            get => _siteName;
            set => SetProperty(ref _siteName, value);
        }

        public string SiteDisplayName
        {
            get => _siteDisplayName;
            set
            {
                SetProperty(ref _siteDisplayName, value);
                Title = $"{value} - Sensors";
            }
        }

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

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleRealTimeCommand { get; }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                await LoadSensorsDataAsync();
                StartRealTimeUpdates();
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

        private async Task LoadSensorsDataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SiteId))
                {
                    System.Diagnostics.Debug.WriteLine("SiteId is empty");
                    return;
                }

                // Load site data
                Site = await _dataService.GetSiteAsync(SiteId);
                if (Site == null)
                {
                    System.Diagnostics.Debug.WriteLine("Site is null from DataService");
                    return;
                }

                // Update sensors with real-time data
                GenerateRealTimeData();

                // Update sensors collection
                Sensors.Clear();
                foreach (var sensor in Site.Sensors)
                {
                    Sensors.Add(sensor);
                }

                LastUpdate = DateTime.Now.ToString("HH:mm:ss");
                System.Diagnostics.Debug.WriteLine($"Loaded {Sensors.Count} sensors for real-time display");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSensorsDataAsync error: {ex.Message}");
            }
        }

        private void GenerateRealTimeData()
        {
            if (Site?.Sensors == null) return;

            var random = new Random();

            foreach (var sensor in Site.Sensors)
            {
                // Generate realistic sensor values with some variation
                float baseValue = sensor.CurrentValue.ProcessValue;
                float variation = (float)(random.NextDouble() - 0.5) * 4; // ±2 units variation
                float newValue = Math.Max(0, baseValue + variation);

                // Keep within sensor range
                newValue = Math.Min(newValue, sensor.Config.MaxValue * 0.8f); // Don't exceed 80% of max

                sensor.CurrentValue.ProcessValue = newValue;
                sensor.CurrentValue.Timestamp = DateTime.Now;

                // Update alarm status based on current value
                UpdateSensorAlarms(sensor);

                // Randomly simulate faults (very rare)
                if (random.Next(1000) == 0) // 0.1% chance
                {
                    var faultTypes = new[] { SensorStatus.LineOpenFault, SensorStatus.LineShortFault, SensorStatus.DetectorError };
                    sensor.CurrentValue.Status = faultTypes[random.Next(faultTypes.Length)];
                }
                else if (sensor.CurrentValue.Status != SensorStatus.AlarmLevel1 &&
                         sensor.CurrentValue.Status != SensorStatus.AlarmLevel2)
                {
                    sensor.CurrentValue.Status = SensorStatus.Normal;
                }
            }
        }

        private void UpdateSensorAlarms(Sensor sensor)
        {
            var value = sensor.CurrentValue.ProcessValue;

            sensor.Alarms.IsAlarmLevel2Active = value >= sensor.Alarms.AlarmLevel2;
            sensor.Alarms.IsAlarmLevel1Active = value >= sensor.Alarms.AlarmLevel1 && !sensor.Alarms.IsAlarmLevel2Active;

            if (sensor.Alarms.IsAlarmLevel2Active)
                sensor.CurrentValue.Status = SensorStatus.AlarmLevel2;
            else if (sensor.Alarms.IsAlarmLevel1Active)
                sensor.CurrentValue.Status = SensorStatus.AlarmLevel1;
            else if (sensor.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                     sensor.CurrentValue.Status == SensorStatus.AlarmLevel2)
                sensor.CurrentValue.Status = SensorStatus.Normal;
        }

        private void StartRealTimeUpdates()
        {
            if (IsRealTimeEnabled)
            {
                _refreshTimer = new Timer(async _ =>
                {
                    if (IsRealTimeEnabled)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            GenerateRealTimeData();
                            LastUpdate = DateTime.Now.ToString("HH:mm:ss");

                            // Trigger property change notifications for all sensors
                            foreach (var sensor in Sensors)
                            {
                                OnPropertyChanged(nameof(Sensors));
                            }
                        });
                    }
                }, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)); // Update every 3 seconds
            }
        }

        private void StopRealTimeUpdates()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }

        private void ToggleRealTime()
        {
            IsRealTimeEnabled = !IsRealTimeEnabled;

            if (IsRealTimeEnabled)
            {
                StartRealTimeUpdates();
            }
            else
            {
                StopRealTimeUpdates();
            }
        }

        public void OnDisappearing()
        {
            StopRealTimeUpdates();
        }
    }
}