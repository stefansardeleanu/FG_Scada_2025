using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FG_Scada_2025.Models
{
    public class Sensor : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _tag = string.Empty;
        private string _name = string.Empty;
        private string _siteId = string.Empty;
        private SensorType _type;
        private SensorValue _currentValue = new SensorValue();
        private SensorAlarms _alarms = new SensorAlarms();
        private SensorConfig _config = new SensorConfig();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string SiteId
        {
            get => _siteId;
            set => SetProperty(ref _siteId, value);
        }

        public SensorType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public SensorValue CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        public SensorAlarms Alarms
        {
            get => _alarms;
            set => SetProperty(ref _alarms, value);
        }

        public SensorConfig Config
        {
            get => _config;
            set => SetProperty(ref _config, value);
        }

        #region INotifyPropertyChanged Implementation

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

    public enum SensorType
    {
        GasDetector,
        TemperatureSensor,
        PressureSensor,
        FlowSensor
    }

    public class SensorValue : INotifyPropertyChanged
    {
        private float _processValue;
        private string _unit = string.Empty;
        private SensorStatus _status;
        private DateTime _timestamp;

        public float ProcessValue
        {
            get => _processValue;
            set => SetProperty(ref _processValue, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public SensorStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        #region INotifyPropertyChanged Implementation

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

    public enum SensorStatus
    {
        Normal,
        AlarmLevel1,
        AlarmLevel2,
        LineOpenFault,
        LineShortFault,
        DetectorError,
        DetectorDisabled
    }

    public class SensorAlarms : INotifyPropertyChanged
    {
        private float _alarmLevel1;
        private float _alarmLevel2;
        private bool _isAlarmLevel1Active;
        private bool _isAlarmLevel2Active;

        public float AlarmLevel1
        {
            get => _alarmLevel1;
            set => SetProperty(ref _alarmLevel1, value);
        }

        public float AlarmLevel2
        {
            get => _alarmLevel2;
            set => SetProperty(ref _alarmLevel2, value);
        }

        public bool IsAlarmLevel1Active
        {
            get => _isAlarmLevel1Active;
            set => SetProperty(ref _isAlarmLevel1Active, value);
        }

        public bool IsAlarmLevel2Active
        {
            get => _isAlarmLevel2Active;
            set => SetProperty(ref _isAlarmLevel2Active, value);
        }

        #region INotifyPropertyChanged Implementation

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

    public class SensorConfig
    {
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string ModbusAddress { get; set; } = string.Empty;
        public int UpdateInterval { get; set; } = 5; // seconds
    }
}