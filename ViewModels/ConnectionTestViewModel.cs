using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FG_Scada_2025.Services;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.ViewModels
{
    public class ConnectionTestViewModel : INotifyPropertyChanged
    {
        private readonly ConnectionManager _connectionManager;
        private string _brokerHost = "atsdhala2.ddns.net";
        private string _brokerPort = "1883";
        private string _username = "atsd";
        private string _password = "GroupATSD579!";
        private string _testTopic = "PLCNext/+";
        private string _connectionStatus = "Disconnected";
        private string _statusColor = "Red";
        private string _statusBackgroundColor = "#dc3545";

        public ObservableCollection<string> SubscribedTopics { get; } = new ObservableCollection<string>();
        public ObservableCollection<ReceivedMessage> ReceivedMessages { get; } = new ObservableCollection<ReceivedMessage>();
        public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();

        public ConnectionTestViewModel(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;

            // Subscribe to connection manager events
            _connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _connectionManager.SensorDataReceived += OnSensorDataReceived;
            _connectionManager.LogMessageReceived += OnLogMessageReceived;

            // Initialize commands
            ConnectCommand = new Command(async () => await ConnectAsync(), () => !_connectionManager.IsConnected);
            DisconnectCommand = new Command(async () => await DisconnectAsync(), () => _connectionManager.IsConnected);
            SubscribeCommand = new Command(async () => await SubscribeToTopicAsync(), () => _connectionManager.IsConnected && !string.IsNullOrEmpty(TestTopic));
            ClearMessagesCommand = new Command(() => ReceivedMessages.Clear());
            ClearLogCommand = new Command(() => LogMessages.Clear());
            BackCommand = new Command(async () => await GoBackAsync());

            AddLogMessage("Connection test page initialized");
        }

        #region Properties

        public string BrokerHost
        {
            get => _brokerHost;
            set => SetProperty(ref _brokerHost, value);
        }

        public string BrokerPort
        {
            get => _brokerPort;
            set => SetProperty(ref _brokerPort, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string TestTopic
        {
            get => _testTopic;
            set
            {
                SetProperty(ref _testTopic, value);
                ((Command)SubscribeCommand).ChangeCanExecute();
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public string StatusBackgroundColor
        {
            get => _statusBackgroundColor;
            set => SetProperty(ref _statusBackgroundColor, value);
        }

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SubscribeCommand { get; }
        public ICommand ClearMessagesCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand BackCommand { get; }

        #endregion

        #region Methods

        private async Task ConnectAsync()
        {
            try
            {
                AddLogMessage($"Attempting to connect to {BrokerHost}:{BrokerPort}");

                var config = new MqttConnectionConfig
                {
                    BrokerHost = BrokerHost,
                    BrokerPort = int.Parse(BrokerPort),
                    Username = string.IsNullOrEmpty(Username) ? null : Username,
                    Password = string.IsNullOrEmpty(Password) ? null : Password,
                    UserId = "TestUser",
                    UserRole = "Administrator",
                    AllowedSites = new List<string> { "PanouHurezani", "TestSite" }
                };

                bool connected = await _connectionManager.ConnectWithConfigAsync(config);

                if (connected)
                {
                    AddLogMessage("Connection successful!");
                }
                else
                {
                    AddLogMessage("Connection failed!");
                }

                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                AddLogMessage($"Connection error: {ex.Message}");
            }
        }

        private async Task DisconnectAsync()
        {
            try
            {
                await _connectionManager.DisconnectAsync();
                SubscribedTopics.Clear();
                AddLogMessage("Disconnected from broker");
                UpdateCommandStates();
            }
            catch (Exception ex)
            {
                AddLogMessage($"Disconnect error: {ex.Message}");
            }
        }

        private async Task SubscribeToTopicAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(TestTopic))
                {
                    AddLogMessage("Please enter a topic to subscribe to");
                    return;
                }

                // Subscribe directly to the specified topic
                var topicFilter = new MQTTnet.MqttTopicFilterBuilder()
                    .WithTopic(TestTopic)
                    .Build();

                // Access the MQTT client through the connection manager
                // For now, we'll add a method to handle custom topic subscription
                await SubscribeToCustomTopicAsync(TestTopic);

                if (!SubscribedTopics.Contains(TestTopic))
                {
                    SubscribedTopics.Add(TestTopic);
                }

                AddLogMessage($"Subscribed to topic: {TestTopic}");
            }
            catch (Exception ex)
            {
                AddLogMessage($"Subscribe error: {ex.Message}");
            }
        }

        private async Task SubscribeToCustomTopicAsync(string topic)
        {
            // Use the connection manager's new custom topic subscription method
            await _connectionManager.SubscribeToCustomTopicAsync(topic);
        }

        private async Task GoBackAsync()
        {
            try
            {
                await _connectionManager.DisconnectAsync();
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                AddLogMessage($"Navigation error: {ex.Message}");
            }
        }

        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStatus = isConnected ? "Connected" : "Disconnected";
                StatusColor = isConnected ? "Green" : "Red";
                StatusBackgroundColor = isConnected ? "#28a745" : "#dc3545";

                UpdateCommandStates();
            });
        }

        private void OnSensorDataReceived(object? sender, SensorDataReceivedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var message = new ReceivedMessage
                {
                    Topic = $"PLCNext/{e.SensorData.SensorId}",
                    Payload = $"TAG: {e.SensorData.TagName}, Current: {e.SensorData.CurrentValue:F2}mA, PV: {e.SensorData.ProcessValue:F2}, Status: {GetStatusText(e.SensorData.Status)}",
                    Timestamp = e.SensorData.Timestamp
                };

                ReceivedMessages.Insert(0, message);

                // Keep only the last 50 messages
                while (ReceivedMessages.Count > 50)
                {
                    ReceivedMessages.RemoveAt(ReceivedMessages.Count - 1);
                }
            });
        }

        private string GetStatusText(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.Normal => "Normal",
                SensorStatus.AlarmLevel1 => "Alarm L1",
                SensorStatus.AlarmLevel2 => "Alarm L2",
                SensorStatus.DetectorError => "Error",
                SensorStatus.DetectorDisabled => "Disabled",
                SensorStatus.LineOpenFault => "Line Open",
                SensorStatus.LineShortFault => "Line Short",
                _ => "Unknown"
            };
        }

        private void OnLogMessageReceived(object? sender, string logMessage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddLogMessage(logMessage);
            });
        }

        private void AddLogMessage(string message)
        {
            LogMessages.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");

            // Keep only the last 100 log messages
            while (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(LogMessages.Count - 1);
            }
        }

        private void UpdateCommandStates()
        {
            ((Command)ConnectCommand).ChangeCanExecute();
            ((Command)DisconnectCommand).ChangeCanExecute();
            ((Command)SubscribeCommand).ChangeCanExecute();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    public class ReceivedMessage
    {
        public string Topic { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}