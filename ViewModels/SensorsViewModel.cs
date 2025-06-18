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
        private readonly RealTimeDataService _realTimeDataService;

        private string _siteId = string.Empty;
        private string _siteName = string.Empty;
        private string _siteDisplayName = string.Empty;
        private Site? _site;
        private ObservableCollection<Sensor> _sensors = new ObservableCollection<Sensor>();
        private string _lastUpdate = string.Empty;
        private bool _isRealTimeEnabled = true;

        private readonly Dictionary<string, DateTime> _lastMessageTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, DateTime> _sensorTimestamps = new Dictionary<string, DateTime>();
        private Timer? _refreshTimer;

        public SensorsViewModel(DataService dataService, NavigationService navigationService, RealTimeDataService realTimeDataService)
        {
            _dataService = dataService;
            _navigationService = navigationService;
            _realTimeDataService = realTimeDataService;

            BackCommand = new Command(async () => await _navigationService.GoBackAsync());
            RefreshCommand = new Command(async () => await LoadSensorsDataAsync());
            ToggleRealTimeCommand = new Command(() => ToggleRealTime());

            // Subscribe to real-time data updates
            _realTimeDataService.SensorDataUpdated += OnSensorDataUpdated;

            System.Diagnostics.Debug.WriteLine("SensorsViewModel created with MQTT integration");
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
                System.Diagnostics.Debug.WriteLine("Initializing SensorsViewModel with MQTT...");

                // Ensure real-time is enabled by default
                IsRealTimeEnabled = true;
                System.Diagnostics.Debug.WriteLine($"Real-time explicitly set to: {IsRealTimeEnabled}");

                // Initialize real-time service
                await _realTimeDataService.InitializeAsync();

                // Load sensors data
                await LoadSensorsDataAsync();

                // Start real-time updates
                StartRealTimeUpdates();

                System.Diagnostics.Debug.WriteLine($"SensorsViewModel initialized: {Sensors.Count} sensors, Real-time: {IsRealTimeEnabled}");
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

                System.Diagnostics.Debug.WriteLine($"Loading sensors for SiteId: {SiteId}, SiteName: {SiteName}");

                // Load site data from JSON configuration
                Site = await _dataService.GetSiteAsync(SiteId);

                if (Site?.Sensors != null)
                {
                    // Update sensors collection
                    Sensors.Clear();
                    foreach (var sensor in Site.Sensors)
                    {
                        Sensors.Add(sensor);
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {Sensors.Count} sensors from configuration");

                    // Apply real-time data overlay
                    LoadRealTimeDataOverlay();
                }

                LastUpdate = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sensors data: {ex.Message}");
            }
        }

        private void LoadRealTimeDataOverlay()
        {
            try
            {
                // Get real-time data for PanouHurezani (site ID 5)
                var siteInfo = _realTimeDataService.GetSiteInfo("PanouHurezani");

                if (siteInfo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found real-time data for PanouHurezani with {siteInfo.Sensors.Count} sensors");

                    foreach (var realTimeData in siteInfo.Sensors.Values)
                    {
                        UpdateSensorWithRealTimeData(realTimeData);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No real-time data found for PanouHurezani");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading real-time data overlay: {ex.Message}");
            }
        }

        private void UpdateSensorWithRealTimeData(SensorData realTimeData)
        {
            try
            {
                var sensorKey = $"{realTimeData.SiteId}_{realTimeData.TagName}";
                var now = DateTime.Now;

                // Track message reception time for this sensor
                var previousMessageTime = _lastMessageTimes.ContainsKey(sensorKey) ? _lastMessageTimes[sensorKey] : now;
                _lastMessageTimes[sensorKey] = now;

                // Track sensor's own timestamp for relative time calculations
                var previousSensorTime = _sensorTimestamps.ContainsKey(sensorKey) ? _sensorTimestamps[sensorKey] : realTimeData.Timestamp;
                _sensorTimestamps[sensorKey] = realTimeData.Timestamp;

                // Calculate time since last message (reception time)
                var timeSinceLastMessage = now - previousMessageTime;

                // Calculate time difference in sensor's own timestamps
                var sensorTimeDifference = realTimeData.Timestamp - previousSensorTime;

                System.Diagnostics.Debug.WriteLine($"📊 Sensor {realTimeData.TagName}: Time since last reception: {timeSinceLastMessage.TotalSeconds:F1}s, Sensor time diff: {sensorTimeDifference.TotalSeconds:F1}s");

                // Find existing sensor by tag name
                var sensor = Sensors.FirstOrDefault(s =>
                    s.Tag == realTimeData.TagName ||
                    s.Id == realTimeData.ChannelId);

                if (sensor != null)
                {
                    // Store the index to replace the sensor and trigger UI update
                    var index = Sensors.IndexOf(sensor);

                    // Update sensor with real-time data
                    sensor.CurrentValue.ProcessValue = (float)realTimeData.ProcessValue;
                    sensor.CurrentValue.Status = realTimeData.Status;
                    sensor.CurrentValue.Timestamp = realTimeData.Timestamp;
                    sensor.CurrentValue.Unit = MqttService.GetDetectorUnits(realTimeData.DetectorType);
                    sensor.Type = MapDetectorTypeToSensorType(realTimeData.DetectorType);

                    // Update alarm states
                    UpdateAlarmStates(sensor);

                    // Force UI update by removing and re-adding the sensor
                    Sensors.RemoveAt(index);
                    Sensors.Insert(index, sensor);

                    System.Diagnostics.Debug.WriteLine($"✅ Updated sensor {sensor.Tag} = {realTimeData.ProcessValue} {sensor.CurrentValue.Unit} (Reception interval: {timeSinceLastMessage.TotalSeconds:F1}s)");
                }
                else
                {
                    // Create new sensor from real-time data
                    var newSensor = CreateSensorFromRealTimeData(realTimeData);
                    Sensors.Add(newSensor);

                    System.Diagnostics.Debug.WriteLine($"✅ Created new sensor {newSensor.Tag} = {realTimeData.ProcessValue}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating sensor with real-time data: {ex.Message}");
            }
        }

        private Sensor CreateSensorFromRealTimeData(SensorData realTimeData)
        {
            return new Sensor
            {
                Id = realTimeData.ChannelId,
                Tag = realTimeData.TagName,
                Name = realTimeData.TagName,
                SiteId = realTimeData.SiteId.ToString(),
                Type = MapDetectorTypeToSensorType(realTimeData.DetectorType),

                CurrentValue = new SensorValue
                {
                    ProcessValue = (float)realTimeData.ProcessValue,
                    Unit = MqttService.GetDetectorUnits(realTimeData.DetectorType),
                    Status = realTimeData.Status,
                    Timestamp = realTimeData.Timestamp
                },

                Alarms = new SensorAlarms
                {
                    AlarmLevel1 = GetDefaultAlarmLevel1(realTimeData.DetectorType),
                    AlarmLevel2 = GetDefaultAlarmLevel2(realTimeData.DetectorType)
                },

                Config = new SensorConfig
                {
                    MinValue = realTimeData.DetectorType <= 2 ? 0 : 4,
                    MaxValue = realTimeData.DetectorType <= 2 ? 100 : 20
                }
            };
        }

        private SensorType MapDetectorTypeToSensorType(int detectorType)
        {
            return detectorType switch
            {
                1 or 2 => SensorType.GasDetector,
                3 => SensorType.TemperatureSensor, // Flame detector
                4 or 5 => SensorType.PressureSensor, // Manual call point and smoke
                _ => SensorType.GasDetector
            };
        }

        private float GetDefaultAlarmLevel1(int detectorType)
        {
            return detectorType switch
            {
                1 => 25f,    // 25% LEL
                2 => 500f,   // 500 PPM
                _ => 15f     // 15mA for others
            };
        }

        private float GetDefaultAlarmLevel2(int detectorType)
        {
            return detectorType switch
            {
                1 => 50f,    // 50% LEL
                2 => 1000f,  // 1000 PPM
                _ => 18f     // 18mA for others
            };
        }

        private void UpdateAlarmStates(Sensor sensor)
        {
            var value = sensor.CurrentValue.ProcessValue;

            sensor.Alarms.IsAlarmLevel2Active = value >= sensor.Alarms.AlarmLevel2;
            sensor.Alarms.IsAlarmLevel1Active = value >= sensor.Alarms.AlarmLevel1 && !sensor.Alarms.IsAlarmLevel2Active;
        }

        private void OnSensorDataUpdated(object? sender, SensorDataUpdatedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 MQTT data received: Site={e.SensorData.SiteId}_{e.SensorData.SiteName}, Tag={e.SensorData.TagName}, Value={e.SensorData.ProcessValue}, Real-time enabled: {IsRealTimeEnabled}");

            // Process data for PanouHurezani (site ID 5) or any site that matches our current site
            if ((e.SensorData.SiteId == 5 && e.SensorData.SiteName == "PanouHurezani") ||
                e.SensorData.SiteName.Equals(SiteName, StringComparison.OrdinalIgnoreCase))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Always apply updates regardless of real-time setting for now (to fix the issue)
                    UpdateSensorWithRealTimeData(e.SensorData);
                    LastUpdate = DateTime.Now.ToString("HH:mm:ss");

                    System.Diagnostics.Debug.WriteLine($"✅ Auto-update applied: {e.SensorData.TagName} = {e.SensorData.ProcessValue} at {LastUpdate}");
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ignoring data for different site: {e.SensorData.SiteId}_{e.SensorData.SiteName}");
            }
        }

        private void StartRealTimeUpdates()
        {
            if (!IsRealTimeEnabled)
            {
                System.Diagnostics.Debug.WriteLine("Real-time updates disabled");
                return;
            }

            // Start timer to check for offline sensors based on relative message timing
            _refreshTimer = new Timer(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var now = DateTime.Now;

                    foreach (var sensor in Sensors)
                    {
                        var sensorKey = $"{sensor.SiteId}_{sensor.Tag}";

                        // Check if we have timing data for this sensor
                        if (_lastMessageTimes.ContainsKey(sensorKey))
                        {
                            var timeSinceLastMessage = now - _lastMessageTimes[sensorKey];

                            // Mark sensor as offline if no message received for more than 90 seconds
                            // This is based on when we last received an MQTT message, not sensor timestamps
                            if (timeSinceLastMessage.TotalSeconds > 90 && sensor.CurrentValue.Status != SensorStatus.DetectorDisabled)
                            {
                                var index = Sensors.IndexOf(sensor);
                                sensor.CurrentValue.Status = SensorStatus.DetectorDisabled;

                                // Update UI
                                Sensors.RemoveAt(index);
                                Sensors.Insert(index, sensor);

                                System.Diagnostics.Debug.WriteLine($"⚠️ Sensor {sensor.Tag} marked offline - no MQTT message for {timeSinceLastMessage.TotalSeconds:F0} seconds");
                            }
                        }
                    }
                });
            }, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15)); // Check every 15 seconds

            System.Diagnostics.Debug.WriteLine("✅ Real-time updates started - using relative message timing for offline detection");
        }

        private void ToggleRealTime()
        {
            IsRealTimeEnabled = !IsRealTimeEnabled;

            System.Diagnostics.Debug.WriteLine($"🔄 Real-time toggled: {(IsRealTimeEnabled ? "ON" : "OFF")}");

            if (IsRealTimeEnabled)
            {
                StartRealTimeUpdates();
            }
            else
            {
                _refreshTimer?.Dispose();
                _refreshTimer = null;
                System.Diagnostics.Debug.WriteLine("❌ Real-time updates stopped");
            }
        }

        public void OnDisappearing()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;

            // Clear timing data when leaving the page
            _lastMessageTimes.Clear();
            _sensorTimestamps.Clear();

            System.Diagnostics.Debug.WriteLine("SensorsViewModel disposed - timing data cleared");
        }
    }
}