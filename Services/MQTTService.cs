using MQTTnet;
using System.Text;
using System.Text.Json;
using System.Buffers;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Services
{
    public class MqttService
    {
        private IMqttClient? _mqttClient;
        private readonly string _clientId;
        private bool _isConnected = false;
        private Timer? _reconnectTimer;
        private Timer? _connectionMonitorTimer;
        private MqttClientOptions? _clientOptions;

        // Site tracking
        private readonly Dictionary<int, SiteConnectionInfo> _siteConnections = new();
        private readonly object _siteLock = new object();

        // Events for connection status and data received
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<SensorDataReceivedEventArgs>? SensorDataReceived;
        public event EventHandler<string>? LogMessageReceived;
        public event EventHandler<SiteDiscoveredEventArgs>? SiteDiscovered;
        public event EventHandler<SiteConnectionStatusEventArgs>? SiteConnectionStatusChanged;

        public MqttService()
        {
            _clientId = $"FG_Scada_{Guid.NewGuid()}";

            // Start connection monitoring timer
            _connectionMonitorTimer = new Timer(CheckSiteConnections, null,
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public bool IsConnected => _isConnected;

        public async Task<bool> ConnectAsync(string brokerHost, int brokerPort, string? username = null, string? password = null)
        {
            try
            {
                LogMessage($"Attempting to connect to MQTT broker at {brokerHost}:{brokerPort}");

                // Create MQTT client factory
                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient();

                // Configure MQTT client options
                var clientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(_clientId)
                    .WithTcpServer(brokerHost, brokerPort)
                    .WithCleanSession();

                // Add credentials if provided
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    clientOptionsBuilder.WithCredentials(username, password);
                }

                _clientOptions = clientOptionsBuilder.Build();

                // Set up event handlers
                _mqttClient.ConnectedAsync += OnConnectedAsync;
                _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
                _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

                // Connect to the broker
                await _mqttClient.ConnectAsync(_clientOptions);

                // Wait a bit to see if connection is established
                await Task.Delay(2000);

                return _isConnected;
            }
            catch (Exception ex)
            {
                LogMessage($"Error connecting to MQTT broker: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _reconnectTimer?.Dispose();
                _reconnectTimer = null;

                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.Dispose();
                    _mqttClient = null;
                }
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(this, false);
                LogMessage("Disconnected from MQTT broker");
            }
            catch (Exception ex)
            {
                LogMessage($"Error disconnecting from MQTT broker: {ex.Message}");
            }
        }

        public async Task SubscribeToAllSitesAsync()
        {
            try
            {
                if (_mqttClient == null || !_isConnected)
                {
                    LogMessage("Cannot subscribe - MQTT client not connected");
                    return;
                }

                // Subscribe to multiple patterns to catch different formats:
                // 1. +/+ (for /5_PanouHurezani/CH40)
                // 2. PLCNext/+/+ (for PLCNext/5_PanouHurezani/CH40)
                // 3. PLCNext/+ (for simple PLCNext/CH40 format)

                var patterns = new[] { "+/+", "PLCNext/+/+", "PLCNext/+" };

                foreach (var pattern in patterns)
                {
                    var topicFilter = new MqttTopicFilterBuilder()
                        .WithTopic(pattern)
                        .Build();

                    await _mqttClient.SubscribeAsync(topicFilter);
                    LogMessage($"Subscribed to pattern: {pattern}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to all sites: {ex.Message}");
            }
        }

        public async Task SubscribeToCustomTopicAsync(string topic)
        {
            try
            {
                if (_mqttClient == null || !_isConnected)
                {
                    LogMessage("Cannot subscribe - MQTT client not connected");
                    return;
                }

                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .Build();

                await _mqttClient.SubscribeAsync(topicFilter);
                LogMessage($"Subscribed to custom topic: {topic}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to custom topic {topic}: {ex.Message}");
            }
        }

        public async Task UnsubscribeFromSiteAsync(string siteId)
        {
            try
            {
                if (_mqttClient == null || !_isConnected)
                {
                    return;
                }

                string topic = $"{siteId}/+";
                await _mqttClient.UnsubscribeAsync(topic);
                LogMessage($"Unsubscribed from topic: {topic}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error unsubscribing from site {siteId}: {ex.Message}");
            }
        }

        public List<SiteConnectionInfo> GetDiscoveredSites()
        {
            lock (_siteLock)
            {
                return new List<SiteConnectionInfo>(_siteConnections.Values);
            }
        }

        public SiteConnectionInfo? GetSiteInfo(int siteId)
        {
            lock (_siteLock)
            {
                return _siteConnections.TryGetValue(siteId, out var site) ? site : null;
            }
        }

        private void CheckSiteConnections(object? state)
        {
            lock (_siteLock)
            {
                var now = DateTime.Now;
                foreach (var site in _siteConnections.Values)
                {
                    var wasConnected = site.IsConnected;
                    var isConnectedNow = (now - site.LastMessageTime) < TimeSpan.FromSeconds(30);

                    if (wasConnected != isConnectedNow)
                    {
                        LogMessage($"Site {site.SiteId}_{site.SiteName} connection status changed: {(isConnectedNow ? "Connected" : "Disconnected")}");
                        SiteConnectionStatusChanged?.Invoke(this, new SiteConnectionStatusEventArgs(site.SiteId, site.SiteName, isConnectedNow));
                    }
                }
            }
        }

        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            _isConnected = true;
            ConnectionStatusChanged?.Invoke(this, true);
            LogMessage("Connected to MQTT broker");

            // Setup auto-reconnect
            SetupAutoReconnect();

            return Task.CompletedTask;
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
            LogMessage($"Disconnected from MQTT broker: {e.Reason}");

            // Setup auto-reconnect if not intentional disconnect
            if (e.Reason != MqttClientDisconnectReason.NormalDisconnection)
            {
                SetupAutoReconnect();
            }

            return Task.CompletedTask;
        }

        private void SetupAutoReconnect()
        {
            if (_clientOptions == null || _mqttClient == null)
                return;

            _reconnectTimer?.Dispose();
            _reconnectTimer = new Timer(async _ =>
            {
                try
                {
                    if (!_isConnected && _mqttClient != null && _clientOptions != null)
                    {
                        LogMessage("Attempting to reconnect...");
                        await _mqttClient.ConnectAsync(_clientOptions);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Reconnection failed: {ex.Message}");
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;

                // Convert ReadOnlySequence<byte> to string
                string payload;
                var payloadSequence = e.ApplicationMessage.Payload;
                if (payloadSequence.IsSingleSegment)
                {
                    payload = Encoding.UTF8.GetString(payloadSequence.FirstSpan);
                }
                else
                {
                    var payloadArray = payloadSequence.ToArray();
                    payload = Encoding.UTF8.GetString(payloadArray);
                }

                LogMessage($"RAW MESSAGE - Topic: {topic}, Payload: {payload}");
                System.Diagnostics.Debug.WriteLine($"RAW MQTT MESSAGE - Topic: {topic}, Payload: {payload}");

                // Parse the new topic structure: /{SiteID}_{SiteName}/{ChannelID}
                var parsedTopic = ParseTopicStructure(topic);
                if (parsedTopic == null)
                {
                    LogMessage($"Invalid topic structure: {topic}");
                    return Task.CompletedTask;
                }

                // Update site connection tracking
                UpdateSiteConnection(parsedTopic.SiteId, parsedTopic.SiteName, parsedTopic.ChannelId);

                // Always raise a raw message event first
                var rawSensorData = new SensorData
                {
                    SiteId = 0,
                    SiteName = "Raw",
                    ChannelId = topic,
                    TagName = "RAW_MESSAGE",
                    ProcessValue = 0,
                    CurrentValue = 0,
                    DetectorType = 0,
                    Status = SensorStatus.Normal,
                    Timestamp = DateTime.Now,
                    FullTopic = topic
                };

                SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(rawSensorData));

                // Try to parse JSON payload
                try
                {
                    var sensorData = ParseSensorJsonData(payload, parsedTopic);
                    if (sensorData != null)
                    {
                        LogMessage($"PARSED DATA - Site: {sensorData.SiteId}_{sensorData.SiteName}, Channel: {sensorData.ChannelId}, Tag: {sensorData.TagName}, PV: {sensorData.ProcessValue}, Status: {sensorData.Status}");
                        // Raise event with parsed sensor data
                        SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(sensorData));
                    }
                }
                catch (JsonException jsonEx)
                {
                    LogMessage($"JSON parsing failed (this is OK for non-JSON messages): {jsonEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing received message: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private TopicInfo? ParseTopicStructure(string topic)
        {
            try
            {
                // Handle both formats:
                // 1. /5_PanouHurezani/CH40
                // 2. PLCNext/5_PanouHurezani/CH40

                string cleanTopic = topic;

                // Remove PLCNext prefix if present
                if (topic.StartsWith("PLCNext/"))
                {
                    cleanTopic = topic.Replace("PLCNext/", "");
                }

                // Remove leading slash if present
                if (cleanTopic.StartsWith("/"))
                {
                    cleanTopic = cleanTopic.TrimStart('/');
                }

                var parts = cleanTopic.Split('/');
                if (parts.Length != 2)
                {
                    LogMessage($"Invalid topic structure: {topic} (expected 2 parts after cleaning, got {parts.Length})");
                    return null;
                }

                var sitePart = parts[0]; // "5_PanouHurezani"
                var channelPart = parts[1]; // "CH40"

                var siteComponents = sitePart.Split('_');
                if (siteComponents.Length < 2)
                {
                    LogMessage($"Invalid site part: {sitePart} (expected at least 2 components)");
                    return null;
                }

                if (!int.TryParse(siteComponents[0], out int siteId))
                {
                    LogMessage($"Invalid site ID: {siteComponents[0]} (not a number)");
                    return null;
                }

                var siteName = string.Join("_", siteComponents.Skip(1)); // Handle site names with underscores

                LogMessage($"Parsed topic successfully: SiteID={siteId}, SiteName={siteName}, Channel={channelPart}");

                return new TopicInfo
                {
                    SiteId = siteId,
                    SiteName = siteName,
                    ChannelId = channelPart
                };
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing topic structure: {ex.Message}");
                return null;
            }
        }

        private void UpdateSiteConnection(int siteId, string siteName, string channelId)
        {
            lock (_siteLock)
            {
                if (!_siteConnections.TryGetValue(siteId, out var siteInfo))
                {
                    // New site discovered
                    siteInfo = new SiteConnectionInfo(siteId, siteName);
                    _siteConnections[siteId] = siteInfo;

                    LogMessage($"New site discovered: {siteId}_{siteName}");
                    SiteDiscovered?.Invoke(this, new SiteDiscoveredEventArgs(siteId, siteName));
                }

                // Update last message time
                siteInfo.LastMessageTime = DateTime.Now;

                // Track active channels
                if (!siteInfo.ActiveChannels.Contains(channelId))
                {
                    siteInfo.ActiveChannels.Add(channelId);
                    LogMessage($"New channel discovered: {siteId}_{siteName}/{channelId}");
                }
            }
        }

        private SensorData? ParseSensorJsonData(string jsonPayload, TopicInfo topicInfo)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonPayload);
                var root = document.RootElement;

                // Parse your actual JSON structure
                // { "rCH40_mA": "0.000000E+00", "rCH40_PV": "0.000000", "iCH40_DetStatus": "1", "strCH40_TAG": "" }

                string? channelNumber = null;
                double currentValue = 0;
                double processValue = 0;
                int detectorStatus = 0;
                string tagName = string.Empty;

                // Extract channel number and data
                foreach (var property in root.EnumerateObject())
                {
                    var propertyName = property.Name;
                    var propertyValue = property.Value.GetString() ?? "0";

                    if (propertyName.Contains("_mA") && propertyName.StartsWith("rCH"))
                    {
                        // Extract channel number (e.g., "rCH40_mA" -> "40")
                        var parts = propertyName.Split('_');
                        if (parts.Length > 0)
                        {
                            channelNumber = parts[0].Replace("rCH", "");
                        }
                        // Parse current value (handle scientific notation)
                        if (double.TryParse(propertyValue, out double current))
                        {
                            currentValue = current;
                        }
                    }
                    else if (propertyName.Contains("_PV") && propertyName.StartsWith("rCH"))
                    {
                        // Parse process value
                        if (double.TryParse(propertyValue, out double pv))
                        {
                            processValue = pv;
                        }
                    }
                    else if (propertyName.Contains("_DetStatus") && propertyName.StartsWith("iCH"))
                    {
                        // Parse detector status
                        if (int.TryParse(propertyValue, out int status))
                        {
                            detectorStatus = status;
                        }
                    }
                    else if (propertyName.Contains("_TAG") && propertyName.StartsWith("strCH"))
                    {
                        tagName = propertyValue;
                    }
                }

                if (!string.IsNullOrEmpty(channelNumber))
                {
                    // If tag name is empty, create a default one
                    if (string.IsNullOrEmpty(tagName))
                    {
                        tagName = $"CH{channelNumber}-DETECTOR";
                    }

                    // Determine detector type based on process value range and current
                    int detectorType = DetermineDetectorType(processValue, currentValue);

                    return new SensorData
                    {
                        SiteId = topicInfo.SiteId,
                        SiteName = topicInfo.SiteName,
                        ChannelId = $"CH{channelNumber}",
                        TagName = tagName,
                        ProcessValue = processValue,
                        CurrentValue = currentValue,
                        DetectorType = detectorType,
                        Status = (SensorStatus)detectorStatus,
                        Timestamp = DateTime.Now,
                        FullTopic = $"/{topicInfo.SiteId}_{topicInfo.SiteName}/{topicInfo.ChannelId}"
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing sensor JSON: {ex.Message}");
                return null;
            }
        }

        private int DetermineDetectorType(double processValue, double currentValue)
        {
            // Simple heuristic to determine detector type based on values
            // You can adjust these rules based on your specific detectors

            if (currentValue >= 4.0 && currentValue <= 20.0)
            {
                // 4-20mA signal, likely current-based detector
                if (processValue > 100)
                {
                    return 2; // Gas detector with PPM
                }
                else if (processValue <= 100 && processValue > 0)
                {
                    return 1; // Gas detector with %LEL
                }
                else
                {
                    return 3; // Flame detector or other 4-20mA device
                }
            }
            else
            {
                // Assume gas detector with %LEL for low values
                if (processValue <= 100)
                {
                    return 1; // Gas detector %LEL
                }
                else
                {
                    return 2; // Gas detector PPM
                }
            }
        }

        private void LogMessage(string message)
        {
            LogMessageReceived?.Invoke(this, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void Dispose()
        {
            _reconnectTimer?.Dispose();
            _connectionMonitorTimer?.Dispose();
            _ = DisconnectAsync();
        }

        // Helper methods for detector type logic
        public static string GetDetectorIcon(int detectorType)
        {
            return detectorType switch
            {
                1 or 2 => "G", // Gas detectors
                3 => "F",      // Flame detector
                4 => "M",      // Manual call point
                5 => "S",      // Smoke detector
                _ => "?"       // Unknown
            };
        }

        public static string GetDetectorUnits(int detectorType)
        {
            return detectorType switch
            {
                1 => "%LEL",   // Gas detector with %LEL
                2 => "PPM",    // Gas detector with PPM
                3 => "mA",     // Flame detector
                4 => "mA",     // Manual call point
                5 => "mA",     // Smoke detector
                _ => "?"       // Unknown
            };
        }

        public static string GetDetectorTypeName(int detectorType)
        {
            return detectorType switch
            {
                1 => "Gas Detector (%LEL)",
                2 => "Gas Detector (PPM)",
                3 => "Flame Detector",
                4 => "Manual Call Point",
                5 => "Smoke Detector",
                _ => "Unknown Detector"
            };
        }
    }

    // Data models for sensor information
    public class SensorData
    {
        public int SiteId { get; set; } // Numeric site ID (5, 13, etc.)
        public string SiteName { get; set; } = string.Empty; // Site name (PanouHurezani, Slavuta, etc.)
        public string ChannelId { get; set; } = string.Empty; // Channel ID (CH40, CH41, etc.)
        public string TagName { get; set; } = string.Empty;
        public double ProcessValue { get; set; } // PV value (LEL, PPM, etc.)
        public double CurrentValue { get; set; } // 4-20mA current value
        public int DetectorType { get; set; } // 1=Gas %LEL, 2=Gas PPM, 3=Flame, 4=Manual, 5=Smoke
        public SensorStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string FullTopic { get; set; } = string.Empty; // Store the full topic for reference
    }

    // Site connection tracking
    public class SiteConnectionInfo
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public bool IsConnected => DateTime.Now - LastMessageTime < TimeSpan.FromSeconds(30);
        public List<string> ActiveChannels { get; set; } = new List<string>();

        public SiteConnectionInfo(int siteId, string siteName)
        {
            SiteId = siteId;
            SiteName = siteName;
            LastMessageTime = DateTime.Now;
        }
    }

    // Event argument classes
    public class SensorDataReceivedEventArgs : EventArgs
    {
        public SensorData SensorData { get; }

        public SensorDataReceivedEventArgs(SensorData sensorData)
        {
            SensorData = sensorData;
        }
    }

    public class TopicInfo
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
    }

    public class SiteDiscoveredEventArgs : EventArgs
    {
        public int SiteId { get; }
        public string SiteName { get; }

        public SiteDiscoveredEventArgs(int siteId, string siteName)
        {
            SiteId = siteId;
            SiteName = siteName;
        }
    }

    public class SiteConnectionStatusEventArgs : EventArgs
    {
        public int SiteId { get; }
        public string SiteName { get; }
        public bool IsConnected { get; }

        public SiteConnectionStatusEventArgs(int siteId, string siteName, bool isConnected)
        {
            SiteId = siteId;
            SiteName = siteName;
            IsConnected = isConnected;
        }
    }
}