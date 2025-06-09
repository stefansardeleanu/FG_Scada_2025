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
    [QueryProperty(nameof(CountyName), "CountyName")]
    public class SiteViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly NavigationService _navigationService;

        private string _siteId = string.Empty;
        private string _siteName = string.Empty;
        private string _siteDisplayName = string.Empty;
        private string _countyName = string.Empty;
        private Site? _site;
        private string _connectionStatus = string.Empty;
        private string _lastUpdate = string.Empty;

        public SiteViewModel(DataService dataService, NavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;

            BackCommand = new Command(async () => await _navigationService.GoBackAsync());
            ViewSensorsCommand = new Command(async () => await NavigateToSensorsAsync());
            ViewAlarmsCommand = new Command(async () => await NavigateToAlarmsAsync());
            RefreshCommand = new Command(async () => await LoadSiteDataAsync());
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
                Title = $"{value} - Overview";
            }
        }

        public string CountyName
        {
            get => _countyName;
            set => SetProperty(ref _countyName, value);
        }

        public Site? Site
        {
            get => _site;
            set => SetProperty(ref _site, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string LastUpdate
        {
            get => _lastUpdate;
            set => SetProperty(ref _lastUpdate, value);
        }

        public ICommand BackCommand { get; }
        public ICommand ViewSensorsCommand { get; }
        public ICommand ViewAlarmsCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                await LoadSiteDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing site: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSiteDataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SiteId))
                {
                    System.Diagnostics.Debug.WriteLine("SiteId is empty");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Loading data for SiteId: {SiteId}");

                // Load site data
                Site = await _dataService.GetSiteAsync(SiteId);
                if (Site == null)
                {
                    System.Diagnostics.Debug.WriteLine("Site is null from DataService");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Site loaded: {Site.Name}, Sensors: {Site.Sensors?.Count ?? 0}");

                // Generate mock real-time data
                var random = new Random();
                foreach (var sensor in Site.Sensors)
                {
                    // Generate realistic sensor values
                    sensor.CurrentValue.ProcessValue = GenerateRealisticValue(sensor, random);
                    sensor.CurrentValue.Status = GetRandomSensorStatus(random);
                    sensor.CurrentValue.Timestamp = DateTime.Now;

                    // Update alarm status based on current value
                    UpdateSensorAlarms(sensor);
                }

                // Update site status based on sensors
                var (hasAlarm, hasFault) = StatusHelper.GetSiteStatus(Site.Sensors);
                Site.Status.HasAlarm = hasAlarm;
                Site.Status.HasFault = hasFault;
                Site.Status.LastUpdate = DateTime.Now;

                // Update connection status
                Site.PlcConnection.IsConnected = random.Next(10) > 0; // 90% chance of being connected
                Site.PlcConnection.LastUpdate = DateTime.Now;

                // Update UI properties
                ConnectionStatus = Site.PlcConnection.IsConnected ? "Connected" : "Disconnected";
                LastUpdate = Site.Status.LastUpdate.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSiteDataAsync error: {ex.Message}");
            }
        }

        private float GenerateRealisticValue(Sensor sensor, Random random)
        {
            // Generate realistic values based on sensor type
            return sensor.Type switch
            {
                SensorType.GasDetector => (float)(random.NextDouble() * 30), // 0-30% LEL or ppm
                SensorType.TemperatureSensor => (float)(15 + random.NextDouble() * 20), // 15-35°C
                SensorType.PressureSensor => (float)(1 + random.NextDouble() * 5), // 1-6 bar
                SensorType.FlowSensor => (float)(random.NextDouble() * 100), // 0-100 m³/h
                _ => (float)(random.NextDouble() * 100)
            };
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
            else if (sensor.CurrentValue.Status == SensorStatus.AlarmLevel1 || sensor.CurrentValue.Status == SensorStatus.AlarmLevel2)
                sensor.CurrentValue.Status = SensorStatus.Normal;
        }

        private SensorStatus GetRandomSensorStatus(Random random)
        {
            // Mostly normal, with occasional faults
            var value = random.Next(100);
            return value switch
            {
                >= 95 => SensorStatus.LineOpenFault,
                >= 90 => SensorStatus.LineShortFault,
                >= 85 => SensorStatus.DetectorError,
                _ => SensorStatus.Normal
            };
        }

        private async Task NavigateToSensorsAsync()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["SiteId"] = SiteId,
                    ["SiteName"] = SiteName,
                    ["SiteDisplayName"] = SiteDisplayName
                };

                await _navigationService.NavigateToAsync("SensorsPage", parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to sensors: {ex.Message}");
            }
        }

        private async Task NavigateToAlarmsAsync()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["SiteId"] = SiteId,
                    ["SiteName"] = SiteName,
                    ["SiteDisplayName"] = SiteDisplayName
                };

                await _navigationService.NavigateToAsync("AlarmHistoryPage", parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to alarms: {ex.Message}");
            }
        }
    }
}