using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FG_Scada_2025.Models
{
    public class County : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _displayName = string.Empty;
        private string _svgPath = string.Empty;
        private string _mapFilePath = string.Empty;
        private CountyPosition _position = new();
        private List<Site> _sites = new();

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string SvgPath
        {
            get => _svgPath;
            set => SetProperty(ref _svgPath, value);
        }

        public string MapFilePath
        {
            get => _mapFilePath;
            set => SetProperty(ref _mapFilePath, value);
        }

        public CountyPosition Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        public List<Site> Sites
        {
            get => _sites;
            set => SetProperty(ref _sites, value);
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

    public class CountyPosition
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
}