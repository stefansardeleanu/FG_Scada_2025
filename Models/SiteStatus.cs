using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FG_Scada_2025.Models
{
    public class SiteStatus : INotifyPropertyChanged
    {
        private bool _hasAlarm;
        private bool _hasFault;
        private DateTime _lastUpdate;

        public bool HasAlarm
        {
            get => _hasAlarm;
            set => SetProperty(ref _hasAlarm, value);
        }

        public bool HasFault
        {
            get => _hasFault;
            set => SetProperty(ref _hasFault, value);
        }

        public bool IsNormal => !HasAlarm && !HasFault;

        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set => SetProperty(ref _lastUpdate, value);
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
    }
}