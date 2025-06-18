using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FG_Scada_2025.Models
{
    // Extension to make SensorValue observable for real-time updates
    public class ObservableSensorValue : INotifyPropertyChanged
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

        // Implicit conversion from SensorValue to ObservableSensorValue
        public static implicit operator ObservableSensorValue(SensorValue sensorValue)
        {
            return new ObservableSensorValue
            {
                ProcessValue = sensorValue.ProcessValue,
                Unit = sensorValue.Unit,
                Status = sensorValue.Status,
                Timestamp = sensorValue.Timestamp
            };
        }

        // Implicit conversion from ObservableSensorValue to SensorValue
        public static implicit operator SensorValue(ObservableSensorValue observableSensorValue)
        {
            return new SensorValue
            {
                ProcessValue = observableSensorValue.ProcessValue,
                Unit = observableSensorValue.Unit,
                Status = observableSensorValue.Status,
                Timestamp = observableSensorValue.Timestamp
            };
        }
    }

    // Extension to make SensorAlarms observable
    public class ObservableSensorAlarms : INotifyPropertyChanged
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

        // Implicit conversion from SensorAlarms to ObservableSensorAlarms
        public static implicit operator ObservableSensorAlarms(SensorAlarms sensorAlarms)
        {
            return new ObservableSensorAlarms
            {
                AlarmLevel1 = sensorAlarms.AlarmLevel1,
                AlarmLevel2 = sensorAlarms.AlarmLevel2,
                IsAlarmLevel1Active = sensorAlarms.IsAlarmLevel1Active,
                IsAlarmLevel2Active = sensorAlarms.IsAlarmLevel2Active
            };
        }

        // Implicit conversion from ObservableSensorAlarms to SensorAlarms
        public static implicit operator SensorAlarms(ObservableSensorAlarms observableSensorAlarms)
        {
            return new SensorAlarms
            {
                AlarmLevel1 = observableSensorAlarms.AlarmLevel1,
                AlarmLevel2 = observableSensorAlarms.AlarmLevel2,
                IsAlarmLevel1Active = observableSensorAlarms.IsAlarmLevel1Active,
                IsAlarmLevel2Active = observableSensorAlarms.IsAlarmLevel2Active
            };
        }
    }
}