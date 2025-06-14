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
        private MqttClientOptions? _clientOptions;

        // Events for connection status and data received
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<SensorDataReceivedEventArgs>? SensorDataReceived;
        public event EventHandler<string>? LogMessageReceived;

        public MqttService()
        {
            _clientId = $"FG_Scada_{Guid.NewGuid()}";
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

        public async Task SubscribeToSiteAsync(string siteId)
        {
            try
            {
                if (_mqttClient == null || !_isConnected)
                {
                    LogMessage("Cannot subscribe - MQTT client not connected");
                    return;
                }

                // Subscribe to all sensor topics for this site
                // Topic format: sites/{siteId}/sensors/+
                string topic = $"sites/{siteId}/sensors/+";

                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .Build();

                await _mqttClient.SubscribeAsync(topicFilter);
                LogMessage($"Subscribed to topic: {topic}");
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

                // Always raise a raw message event first
                var rawSensorData = new SensorData
                {
                    SiteId = "Raw",
                    SensorId = topic,
                    TagName = "RAW_MESSAGE",
                    ProcessValue = 0,
                    CurrentValue = 0,
                    Status = SensorStatus.Normal,
                    Timestamp = DateTime.Now
                };

                SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(rawSensorData));

                // Try to parse PLC data format
                try
                {
                    var plcData = ParsePlcJsonData(payload, topic);
                    if (plcData != null)
                    {
                        LogMessage($"PARSED DATA - Tag: {plcData.TagName}, PV: {plcData.ProcessValue}, Status: {plcData.Status}");
                        // Raise event with parsed sensor data
                        SensorDataReceived?.Invoke(this, new SensorDataReceivedEventArgs(plcData));
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

        private SensorData? ParsePlcJsonData(string jsonPayload, string topic)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonPayload);
                var root = document.RootElement;

                // Parse the new JSON structure
                // { "tag": "KGD-003", "current_ma": 15.5, "process_value": 25.3, "detector_type": 1, "status": 0, "timestamp": "2025-01-14T10:30:45Z" }

                if (!root.TryGetProperty("tag", out var tagElement) ||
                    !root.TryGetProperty("current_ma", out var currentElement) ||
                    !root.TryGetProperty("process_value", out var processElement) ||
                    !root.TryGetProperty("detector_type", out var detectorTypeElement) ||
                    !root.TryGetProperty("status", out var statusElement))
                {
                    LogMessage("JSON missing required fields");
                    return null;
                }

                // Extract values
                string tagName = tagElement.GetString() ?? "";
                double currentValue = currentElement.GetDouble();
                double processValue = processElement.GetDouble();
                int detectorType = detectorTypeElement.GetInt32();
                int status = statusElement.GetInt32();

                // Parse timestamp if provided, otherwise use current time
                DateTime timestamp = DateTime.Now;
                if (root.TryGetProperty("timestamp", out var timestampElement))
                {
                    string? timestampStr = timestampElement.GetString();
                    if (!string.IsNullOrEmpty(timestampStr) && DateTime.TryParse(timestampStr, out DateTime parsedTime))
                    {
                        timestamp = parsedTime;
                    }
                }

                // Extract channel from topic (PLCNext/CH42 -> CH42)
                string sensorId = "Unknown";
                if (topic.StartsWith("PLCNext/"))
                {
                    sensorId = topic.Replace("PLCNext/", "");
                }

                return new SensorData
                {
                    SiteId = "PLCNext", // You can make this configurable later
                    SensorId = sensorId,
                    TagName = tagName,
                    ProcessValue = processValue,
                    CurrentValue = currentValue,
                    DetectorType = detectorType, // We need to add this property
                    Status = (SensorStatus)status,
                    Timestamp = timestamp
                };
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing PLC JSON: {ex.Message}");
                return null;
            }
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

        private void LogMessage(string message)
        {
            LogMessageReceived?.Invoke(this, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        public void Dispose()
        {
            _reconnectTimer?.Dispose();
            _ = DisconnectAsync();
        }
    }

    // Data models for sensor information
    public class SensorData
    {
        public string SiteId { get; set; } = string.Empty;
        public string SensorId { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public double ProcessValue { get; set; } // PV value (LEL, PPM, etc.)
        public double CurrentValue { get; set; } // 4-20mA current value
        public int DetectorType { get; set; } // 1=Gas %LEL, 2=Gas PPM, 3=Flame, 4=Manual, 5=Smoke
        public SensorStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SensorDataReceivedEventArgs : EventArgs
    {
        public SensorData SensorData { get; }

        public SensorDataReceivedEventArgs(SensorData sensorData)
        {
            SensorData = sensorData;
        }
    }
}