using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FG_Scada_2025.Models
{
    public class Site : INotifyPropertyChanged
    {
        private int _siteID;
        private string _displayName = string.Empty;
        private string _countyId = string.Empty;
        private ObservableCollection<Sensor> _sensors = new();
        private SiteStatus _status = new();
        private DateTime _lastMqttUpdate = DateTime.MinValue;
        private SitePosition _position = new();
        private PLCConnection _plcConnection = new();

        // Use SiteID as primary identifier (from MQTT topics)
        public int SiteID
        {
            get => _siteID;
            set => SetProperty(ref _siteID, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string CountyId
        {
            get => _countyId;
            set => SetProperty(ref _countyId, value);
        }

        // Autodiscovered sensors from MQTT
        public ObservableCollection<Sensor> Sensors
        {
            get => _sensors;
            set => SetProperty(ref _sensors, value);
        }

        public SiteStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime LastMqttUpdate
        {
            get => _lastMqttUpdate;
            set => SetProperty(ref _lastMqttUpdate, value);
        }

        public SitePosition Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public PLCConnection PlcConnection
        {
            get => _plcConnection;
            set => SetProperty(ref _plcConnection, value);
        }

        // Derived properties for backward compatibility
        public string Id => SiteID.ToString();
        public string Name => DisplayName;

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
    }

    public class SitePosition
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class PLCConnection
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Protocol { get; set; } = "MQTT"; // MQTT, Modbus, OPC-UA
        public string Topic { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}