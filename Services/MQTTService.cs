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

        // Replace the SubscribeToAllSitesAsync method in MqttService.cs with this version:

        public async Task SubscribeToAllSitesAsync()
        {
            try
            {
                if (_mqttClient == null || !_isConnected)
                {
                    LogMessage("Cannot subscribe - MQTT client not connected");
                    return;
                }

                // Subscribe to your specific pattern
                var subscriptionTopics = new[]
                {
            "/PLCNEXT/+/+",      // For /PLCNEXT/5_PanouHurezani/CH41
            "/PLCNEXT/+",        // For site-level topics /PLCNEXT/5_PanouHurezani
            "PLCNEXT/+/+",       // Without leading slash
            "PLCNEXT/+",         // Site-level without leading slash
            "/+/+",              // Generic fallback
            "+/+"                // Generic fallback without slash
        };

                foreach (var topic in subscriptionTopics)
                {
                    try
                    {
                        await _mqttClient.SubscribeAsync(topic);
                        LogMessage($"✅ Subscribed to: {topic}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"❌ Failed to subscribe to {topic}: {ex.Message}");
                    }
                }

                LogMessage("MQTT subscription completed - waiting for messages...");
            }
            catch (Exception ex)
            {
                LogMessage($"Error subscribing to topics: {ex.Message}");
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

        // Replace the OnMessageReceivedAsync method in MqttService.cs with this safe version:

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚀 OnMessageReceivedAsync called");

                string topic = e.ApplicationMessage.Topic;
                System.Diagnostics.Debug.WriteLine($"📍 Topic extracted: {topic}");

                // Convert ReadOnlySequence<byte> to string
                string payload;
                try
                {
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
                    System.Diagnostics.Debug.WriteLine($"📦 Payload extracted: {payload}");
                }
                catch (Exception payloadEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error extracting payload: {payloadEx.Message}");
                    return Task.CompletedTask;
                }

                LogMessage($"RAW MESSAGE - Topic: {topic}, Payload: {payload}");
                System.Diagnostics.Debug.WriteLine($"RAW MQTT MESSAGE - Topic: {topic}, Payload: {payload}");

                // Parse the topic structure
                TopicInfo? parsedTopic = null;
                try
                {
                    parsedTopic = ParseTopicStructure(topic);
                    if (parsedTopic == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Invalid topic structure: {topic}");
                        return Task.CompletedTask;
                    }
                    System.Diagnostics.Debug.WriteLine($"✅ Topic parsed successfully: Site={parsedTopic.SiteId}_{parsedTopic.SiteName}, Channel={parsedTopic.ChannelId}");
                }
                catch (Exception topicEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error parsing topic: {topicEx.Message}");
                    return Task.CompletedTask;
                }

                // Update site connection tracking
                try
                {
                    UpdateSiteConnection(parsedTopic.SiteId, parsedTopic.SiteName, parsedTopic.ChannelId);
                    System.Diagnostics.Debug.WriteLine($"🔗 Site connection updated");
                }
                catch (Exception connEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error updating site connection: {connEx.Message}");
                }

                // Always raise a raw message event first
                try
                {
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

                    System.Diagnostics.Debug.WriteLine($"📤 About to fire raw sensor event");
                    SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(rawSensorData));
                    System.Diagnostics.Debug.WriteLine($"✅ Raw sensor event fired");
                }
                catch (Exception rawEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error firing raw sensor event: {rawEx.Message}");
                }

                // Try to parse JSON payload
                try
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Attempting to parse JSON for {parsedTopic.SiteId}_{parsedTopic.SiteName}/{parsedTopic.ChannelId}");

                    var sensorData = ParseSensorJsonData(payload, parsedTopic);
                    if (sensorData != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ PARSED DATA - Site: {sensorData.SiteId}_{sensorData.SiteName}, Channel: {sensorData.ChannelId}, Tag: {sensorData.TagName}, PV: {sensorData.ProcessValue}, Current: {sensorData.CurrentValue}, Status: {sensorData.Status}");

                        // Raise event with parsed sensor data
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"📤 About to fire parsed sensor event");
                            SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(sensorData));
                            System.Diagnostics.Debug.WriteLine($"✅ Parsed sensor event fired for {sensorData.TagName}");
                        }
                        catch (Exception eventEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Error firing parsed sensor event: {eventEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"❌ Event exception stack trace: {eventEx.StackTrace}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ JSON parsing returned null for topic {topic}");
                    }
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ JSON parsing failed: {jsonEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Payload was: {payload}");
                }
                catch (Exception parseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ General parsing error: {parseEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"❌ Parse exception stack trace: {parseEx.StackTrace}");
                }

                System.Diagnostics.Debug.WriteLine($"🏁 OnMessageReceivedAsync completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 FATAL ERROR in OnMessageReceivedAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 Stack trace: {ex.StackTrace}");
                LogMessage($"FATAL ERROR in message processing: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        // Replace the ParseTopicStructure method in MqttService.cs with this version:

        private TopicInfo? ParseTopicStructure(string topic)
        {
            try
            {
                LogMessage($"Parsing topic: '{topic}'");

                string cleanTopic = topic;

                // Remove PLCNEXT prefix if present (case insensitive)
                if (topic.ToUpper().Contains("PLCNEXT"))
                {
                    // Remove /PLCNEXT/ or PLCNEXT/ keeping what comes after
                    cleanTopic = System.Text.RegularExpressions.Regex.Replace(
                        topic, @"^/?PLCNEXT/?", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                // Remove leading slash if present
                cleanTopic = cleanTopic.TrimStart('/');

                LogMessage($"Cleaned topic: '{cleanTopic}'");

                var parts = cleanTopic.Split('/');

                if (parts.Length < 1)
                {
                    LogMessage($"Invalid topic structure: {topic} (no parts after cleaning)");
                    return null;
                }

                var sitePart = parts[0]; // "5_PanouHurezani"
                string channelPart = parts.Length > 1 ? parts[1] : "SITE"; // "CH41" or "SITE" for site-level

                var siteComponents = sitePart.Split('_');
                if (siteComponents.Length < 2)
                {
                    LogMessage($"Invalid site part: {sitePart} (expected format: ID_Name)");
                    return null;
                }

                if (!int.TryParse(siteComponents[0], out int siteId))
                {
                    LogMessage($"Invalid site ID: {siteComponents[0]} (not a number)");
                    return null;
                }

                var siteName = string.Join("_", siteComponents.Skip(1)); // Handle site names with underscores

                LogMessage($"Successfully parsed - SiteID={siteId}, SiteName={siteName}, Channel={channelPart}");

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

        // Replace the ParseSensorJsonData method in MqttService.cs with this robust version:

        private SensorData? ParseSensorJsonData(string jsonPayload, TopicInfo topicInfo)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonPayload);
                var root = document.RootElement;

                LogMessage($"Parsing JSON for {topicInfo.SiteId}_{topicInfo.SiteName}/{topicInfo.ChannelId}");

                string? channelNumber = null;
                double currentValue = 0;
                double processValue = 0;
                int detectorStatus = 0;
                int detectorType = 0;
                string tagName = string.Empty;

                // Extract channel number and data
                foreach (var property in root.EnumerateObject())
                {
                    var propertyName = property.Name;
                    var propertyValue = property.Value.GetString() ?? "0";

                    LogMessage($"Processing property: {propertyName} = '{propertyValue}'");

                    try
                    {
                        if (propertyName.Contains("_mA") && propertyName.StartsWith("rCH"))
                        {
                            // Extract channel number (e.g., "rCH41_mA" -> "41")
                            var parts = propertyName.Split('_');
                            if (parts.Length > 0)
                            {
                                channelNumber = parts[0].Replace("rCH", "");
                                LogMessage($"Extracted channel number: {channelNumber}");
                            }

                            // Parse current value (handle scientific notation)
                            if (double.TryParse(propertyValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double current))
                            {
                                currentValue = current;
                                LogMessage($"Parsed current value: {currentValue}");
                            }
                            else
                            {
                                LogMessage($"Failed to parse current value: '{propertyValue}'");
                            }
                        }
                        else if (propertyName.Contains("_PV") && propertyName.StartsWith("rCH"))
                        {
                            // Parse process value
                            if (double.TryParse(propertyValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double pv))
                            {
                                processValue = pv;
                                LogMessage($"Parsed process value: {processValue}");
                            }
                            else
                            {
                                LogMessage($"Failed to parse process value: '{propertyValue}'");
                            }
                        }
                        else if (propertyName.Contains("_DetStatus") && propertyName.StartsWith("iCH"))
                        {
                            // Parse detector status - handle both int and scientific notation
                            if (double.TryParse(propertyValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double statusDouble))
                            {
                                detectorStatus = (int)Math.Round(statusDouble);
                                LogMessage($"Parsed detector status: {detectorStatus} (from '{propertyValue}')");
                            }
                            else
                            {
                                LogMessage($"Failed to parse detector status: '{propertyValue}'");
                            }
                        }
                        else if (propertyName.Contains("_DetType") && propertyName.StartsWith("iCH"))
                        {
                            // Parse detector type
                            if (int.TryParse(propertyValue, out int type))
                            {
                                detectorType = type;
                                LogMessage($"Parsed detector type: {detectorType}");
                            }
                            else
                            {
                                LogMessage($"Failed to parse detector type: '{propertyValue}'");
                            }
                        }
                        else if (propertyName.Contains("_TAG") && propertyName.StartsWith("strCH"))
                        {
                            tagName = propertyValue;
                            LogMessage($"Parsed tag name: '{tagName}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error parsing property {propertyName}: {ex.Message}");
                    }
                }

                // Create sensor data if we have a valid channel number AND valid conditions
                if (!string.IsNullOrEmpty(channelNumber))
                {
                    LogMessage($"Channel {channelNumber}: Tag='{tagName}', Status={detectorStatus}, Type={detectorType}");

                    // Filter out detectors with empty tags OR status > 10
                    if (string.IsNullOrEmpty(tagName) || string.IsNullOrWhiteSpace(tagName))
                    {
                        LogMessage($"❌ Skipping CH{channelNumber} - empty tag name");
                        return null;
                    }

                    if (detectorStatus > 10)
                    {
                        LogMessage($"❌ Skipping CH{channelNumber} - invalid status: {detectorStatus} (must be <= 10)");
                        return null;
                    }

                    // Use the tag from MQTT (we know it's not empty due to filter above)
                    string finalTagName = tagName.Trim();

                    // Use the detector type from PLC data
                    int finalDetectorType = detectorType;
                    if (detectorType == 0)
                    {
                        finalDetectorType = DetermineDetectorType(processValue, currentValue);
                        LogMessage($"PLC provided detector type 0 for CH{channelNumber}, using heuristic: {finalDetectorType}");
                    }
                    else
                    {
                        LogMessage($"Using PLC-provided detector type for CH{channelNumber}: {detectorType}");
                    }

                    var sensorData = new SensorData
                    {
                        SiteId = topicInfo.SiteId,
                        SiteName = topicInfo.SiteName,
                        ChannelId = $"CH{channelNumber}",
                        TagName = finalTagName,
                        ProcessValue = processValue,
                        CurrentValue = currentValue,
                        DetectorType = finalDetectorType,
                        Status = (SensorStatus)detectorStatus,
                        Timestamp = DateTime.Now,
                        FullTopic = $"/{topicInfo.SiteId}_{topicInfo.SiteName}/{topicInfo.ChannelId}"
                    };

                    LogMessage($"✅ Successfully created sensor data for {finalTagName}: PV={processValue}, Current={currentValue}, Status={detectorStatus}");
                    return sensorData;
                }
                else
                {
                    LogMessage($"❌ No valid channel number found in JSON");
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing sensor JSON: {ex.Message}");
                LogMessage($"JSON payload was: {jsonPayload}");
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