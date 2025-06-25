namespace FG_Scada_2025.Services
{
    public class MqttConnectionConfig
    {
        public string BrokerHost { get; set; } = "localhost";
        public int BrokerPort { get; set; } = 1883;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<string> AllowedSites { get; set; } = new List<string>();
        public string UserId { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
    }

    public class ConnectionManager
    {
        private readonly MqttService _mqttService;
        private MqttConnectionConfig? _currentConfig;
        private readonly List<string> _subscribedSites = new List<string>();

        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<SensorDataReceivedEventArgs>? SensorDataReceived;
        public event EventHandler<string>? LogMessageReceived;

        public ConnectionManager()
        {
            _mqttService = new MqttService();

            // Forward events
            _mqttService.ConnectionStatusChanged += (s, e) => ConnectionStatusChanged?.Invoke(s, e);
            _mqttService.SensorDataReceived += (s, e) => SensorDataReceived?.Invoke(s, e);
            _mqttService.LogMessageReceived += (s, e) => LogMessageReceived?.Invoke(s, e);
        }

        public bool IsConnected => _mqttService.IsConnected;

        public async Task<bool> ConnectWithConfigAsync(MqttConnectionConfig config)
        {
            try
            {
                _currentConfig = config;
                LogMessage($"Connecting as user: {config.UserId} ({config.UserRole})");
                LogMessage($"Allowed sites: {string.Join(", ", config.AllowedSites)}");

                bool connected = await _mqttService.ConnectAsync(
                    config.BrokerHost,
                    config.BrokerPort,
                    config.Username,
                    config.Password);

                if (connected)
                {
                    LogMessage("MQTT connection established successfully");
                }
                else
                {
                    LogMessage("Failed to establish MQTT connection");
                }

                return connected;
            }
            catch (Exception ex)
            {
                LogMessage($"Error in ConnectWithConfigAsync: {ex.Message}");
                return false;
            }
        }

        public async Task SubscribeToAllowedSitesAsync()
        {
            if (_currentConfig == null || !_mqttService.IsConnected)
            {
                LogMessage("Cannot subscribe - not connected or no config available");
                return;
            }

            try
            {
                // Use auto-discovery instead of individual site subscriptions
                await _mqttService.SubscribeToAllSitesAsync();
                LogMessage("Subscribed to auto-discovery for all sites");
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to sites: {ex.Message}");
            }
        }

        public async Task SubscribeToSpecificSiteAsync(string siteId)
        {
            if (_currentConfig == null || !_currentConfig.AllowedSites.Contains(siteId))
            {
                LogMessage($"Site {siteId} not allowed for current user");
                return;
            }

            try
            {
                // For now, just use the auto-discovery subscription
                await _mqttService.SubscribeToAllSitesAsync();
                LogMessage($"Subscribed to auto-discovery (includes site: {siteId})");
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to site {siteId}: {ex.Message}");
            }
        }

        public async Task SubscribeToCustomTopicAsync(string topic)
        {
            try
            {
                if (!_mqttService.IsConnected)
                {
                    LogMessage("Cannot subscribe - not connected");
                    return;
                }

                await _mqttService.SubscribeToCustomTopicAsync(topic);
                LogMessage($"Subscribed to custom topic: {topic}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to custom topic {topic}: {ex.Message}");
            }
        }

        public async Task SubscribeToAllSitesAsync()
        {
            try
            {
                await _mqttService.SubscribeToAllSitesAsync();
                LogMessage("Subscribed to auto-discovery for all sites");
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to all sites: {ex.Message}");
            }
        }

        public List<SiteConnectionInfo> GetDiscoveredSites()
        {
            return _mqttService.GetDiscoveredSites();
        }

        public SiteConnectionInfo? GetSiteInfo(int siteId)
        {
            return _mqttService.GetSiteInfo(siteId);
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _mqttService.DisconnectAsync();
                _subscribedSites.Clear();
                LogMessage("Disconnected from MQTT broker");
            }
            catch (Exception ex)
            {
                LogMessage($"Error disconnecting: {ex.Message}");
            }
        }

        public List<string> GetSubscribedSites()
        {
            return new List<string>(_subscribedSites);
        }

        public MqttConnectionConfig? GetCurrentConfig()
        {
            return _currentConfig;
        }

        private void LogMessage(string message)
        {
            LogMessageReceived?.Invoke(this, message);
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
            _mqttService?.Dispose();
        }

    }
}