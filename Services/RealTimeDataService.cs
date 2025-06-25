using System.Collections.Concurrent;
using FG_Scada_2025.Services;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Services
{
    public class RealTimeDataService
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ConcurrentDictionary<string, SensorData> _latestSensorData = new();
        private readonly ConcurrentDictionary<int, SiteRealTimeInfo> _siteData = new();

        // Events for UI updates
        public event EventHandler<SensorDataUpdatedEventArgs>? SensorDataUpdated;
        public event EventHandler<SiteStatusChangedEventArgs>? SiteStatusChanged;

        public RealTimeDataService(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;

            // Subscribe to MQTT events
            _connectionManager.SensorDataReceived += OnSensorDataReceived;
            _connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        // REMOVED AUTO-CONNECTION - Now only initializes without connecting
        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("✅ RealTimeDataService initialized - ready for manual connection");
                // NO AUTO-CONNECT: Connection will be initiated manually from Romania Map page
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing real-time data service: {ex.Message}");
            }
        }

        // NEW: Manual connection method for Romania Map button
        public async Task<bool> ConnectToMqttAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔌 Attempting manual MQTT connection...");

                // Connect to MQTT broker with predefined settings
                var config = new MqttConnectionConfig
                {
                    BrokerHost = "atsdhala2.ddns.net",
                    BrokerPort = 1883,
                    Username = "atsd",
                    Password = "GroupATSD579!",
                    UserId = "ScadaApp",
                    UserRole = "Operator",
                    AllowedSites = new List<string> { "PanouHurezani" }
                };

                bool connected = await _connectionManager.ConnectWithConfigAsync(config);

                if (connected)
                {
                    // Subscribe to auto-discovery
                    await _connectionManager.SubscribeToAllSitesAsync();
                    System.Diagnostics.Debug.WriteLine("✅ Successfully connected to MQTT and subscribed to topics");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Failed to connect to MQTT");
                }

                return connected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting to MQTT: {ex.Message}");
                return false;
            }
        }

        // NEW: Manual disconnection method
        public async Task DisconnectFromMqttAsync()
        {
            try
            {
                await _connectionManager.DisconnectAsync();
                System.Diagnostics.Debug.WriteLine("✅ Disconnected from MQTT");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disconnecting from MQTT: {ex.Message}");
            }
        }

        // NEW: Check if currently connected
        public bool IsConnectedToMqtt => _connectionManager.IsConnected;

        private void OnSensorDataReceived(object? sender, SensorDataReceivedEventArgs e)
        {
            // Skip raw messages
            if (e.SensorData.SiteName == "Raw")
                return;

            // Update sensor data cache
            string sensorKey = $"{e.SensorData.SiteId}_{e.SensorData.SiteName}_{e.SensorData.ChannelId}";
            _latestSensorData[sensorKey] = e.SensorData;

            // Update site data
            UpdateSiteData(e.SensorData);

            // Notify UI components
            SensorDataUpdated?.Invoke(this, new SensorDataUpdatedEventArgs(e.SensorData));
        }

        private void UpdateSiteData(SensorData sensorData)
        {
            if (!_siteData.TryGetValue(sensorData.SiteId, out var siteInfo))
            {
                siteInfo = new SiteRealTimeInfo
                {
                    SiteId = sensorData.SiteId,
                    SiteName = sensorData.SiteName,
                    LastUpdateTime = DateTime.Now
                };
                _siteData[sensorData.SiteId] = siteInfo;
            }

            // Update site status based on sensor data
            siteInfo.LastUpdateTime = DateTime.Now;
            siteInfo.IsOnline = true;

            // Update sensor in site
            string sensorKey = sensorData.ChannelId;
            siteInfo.Sensors[sensorKey] = sensorData;

            // Calculate overall site status
            var previousStatus = siteInfo.OverallStatus;
            siteInfo.OverallStatus = CalculateSiteStatus(siteInfo.Sensors.Values);

            // Notify if site status changed
            if (previousStatus != siteInfo.OverallStatus)
            {
                SiteStatusChanged?.Invoke(this, new SiteStatusChangedEventArgs(siteInfo));
            }
        }

        private SiteStatus CalculateSiteStatus(IEnumerable<SensorData> sensors)
        {
            if (!sensors.Any())
                return SiteStatus.Unknown;

            // Check for any fault conditions first
            if (sensors.Any(s => s.Status == SensorStatus.DetectorError ||
                                s.Status == SensorStatus.LineOpenFault ||
                                s.Status == SensorStatus.LineShortFault))
                return SiteStatus.Fault;

            // Check for alarms
            if (sensors.Any(s => s.Status == SensorStatus.AlarmLevel2))
                return SiteStatus.Alarm;

            if (sensors.Any(s => s.Status == SensorStatus.AlarmLevel1))
                return SiteStatus.Alarm;

            // Check for disabled sensors
            if (sensors.Any(s => s.Status == SensorStatus.DetectorDisabled))
                return SiteStatus.Warning;

            // All normal
            return SiteStatus.Normal;
        }

        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            // Mark all sites as offline if connection lost
            if (!isConnected)
            {
                foreach (var site in _siteData.Values)
                {
                    site.IsOnline = false;
                    SiteStatusChanged?.Invoke(this, new SiteStatusChangedEventArgs(site));
                }
            }
        }

        // Public methods for UI components
        public SensorData? GetLatestSensorData(int siteId, string channelId)
        {
            if (_siteData.TryGetValue(siteId, out var siteInfo))
            {
                return siteInfo.Sensors.TryGetValue(channelId, out var sensor) ? sensor : null;
            }
            return null;
        }

        public SiteRealTimeInfo? GetSiteInfo(int siteId)
        {
            return _siteData.TryGetValue(siteId, out var siteInfo) ? siteInfo : null;
        }

        public SiteRealTimeInfo? GetSiteInfo(string siteName)
        {
            return _siteData.Values.FirstOrDefault(s => s.SiteName.Equals(siteName, StringComparison.OrdinalIgnoreCase));
        }

        public List<SiteRealTimeInfo> GetAllSites()
        {
            return _siteData.Values.ToList();
        }

        public List<SensorData> GetSiteSensors(int siteId)
        {
            if (_siteData.TryGetValue(siteId, out var siteInfo))
            {
                return siteInfo.Sensors.Values.ToList();
            }
            return new List<SensorData>();
        }

        public bool IsSiteOnline(int siteId)
        {
            if (_siteData.TryGetValue(siteId, out var siteInfo))
            {
                // Consider site offline if no data received in last 60 seconds
                return (DateTime.Now - siteInfo.LastUpdateTime) < TimeSpan.FromSeconds(60);
            }
            return false;
        }
    }

    // Supporting classes remain the same...
    public class SiteRealTimeInfo
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public DateTime LastUpdateTime { get; set; }
        public bool IsOnline { get; set; }
        public SiteStatus OverallStatus { get; set; } = SiteStatus.Unknown;
        public ConcurrentDictionary<string, SensorData> Sensors { get; set; } = new();
    }

    public enum SiteStatus
    {
        Unknown,
        Normal,
        Warning,
        Alarm,
        Fault,
        Offline
    }

    public class SensorDataUpdatedEventArgs : EventArgs
    {
        public SensorData SensorData { get; }

        public SensorDataUpdatedEventArgs(SensorData sensorData)
        {
            SensorData = sensorData;
        }
    }

    public class SiteStatusChangedEventArgs : EventArgs
    {
        public SiteRealTimeInfo SiteInfo { get; }

        public SiteStatusChangedEventArgs(SiteRealTimeInfo siteInfo)
        {
            SiteInfo = siteInfo;
        }
    }
}