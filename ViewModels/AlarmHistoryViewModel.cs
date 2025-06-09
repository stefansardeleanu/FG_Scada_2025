using System.Collections.ObjectModel;
using System.Windows.Input;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;
using FG_Scada_2025.Helpers;

namespace FG_Scada_2025.ViewModels
{
    [QueryProperty(nameof(SiteId), "SiteId")]
    [QueryProperty(nameof(SiteName), "SiteName")]
    [QueryProperty(nameof(SiteDisplayName), "SiteDisplayName")]
    public class AlarmHistoryViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly NavigationService _navigationService;

        private string _siteId = string.Empty;
        private string _siteName = string.Empty;
        private string _siteDisplayName = string.Empty;
        private ObservableCollection<Alarm> _alarms = new ObservableCollection<Alarm>();
        private ObservableCollection<Alarm> _filteredAlarms = new ObservableCollection<Alarm>();
        private string _searchText = string.Empty;
        private AlarmType? _selectedAlarmType;
        private bool _showActiveOnly = false;
        private string _alarmSummary = string.Empty;

        public AlarmHistoryViewModel(DataService dataService, NavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;

            BackCommand = new Command(async () => await _navigationService.GoBackAsync());
            RefreshCommand = new Command(async () => await LoadAlarmsAsync());
            SearchCommand = new Command(() => FilterAlarms());
            ClearFiltersCommand = new Command(() => ClearFilters());

            // Initialize alarm types for picker
            AlarmTypes = new List<AlarmTypeItem>
            {
                new AlarmTypeItem { Name = "All Types", Type = null },
                new AlarmTypeItem { Name = "Alarm Level 1", Type = AlarmType.AlarmLevel1 },
                new AlarmTypeItem { Name = "Alarm Level 2", Type = AlarmType.AlarmLevel2 },
                new AlarmTypeItem { Name = "Line Open Fault", Type = AlarmType.LineOpenFault },
                new AlarmTypeItem { Name = "Line Short Fault", Type = AlarmType.LineShortFault },
                new AlarmTypeItem { Name = "Detector Error", Type = AlarmType.DetectorError },
                new AlarmTypeItem { Name = "Communication Loss", Type = AlarmType.CommunicationLoss }
            };

            SelectedAlarmTypeItem = AlarmTypes[0]; // Default to "All Types"
        }

        public string SiteId
        {
            get => _siteId;
            set => SetProperty(ref _siteId, value);
        }

        public string SiteName
        {
            get => _siteName;
            set => SetProperty(ref _siteName, value);
        }

        public string SiteDisplayName
        {
            get => _siteDisplayName;
            set
            {
                SetProperty(ref _siteDisplayName, value);
                Title = $"{value} - Alarm History";
            }
        }

        public ObservableCollection<Alarm> Alarms
        {
            get => _alarms;
            set => SetProperty(ref _alarms, value);
        }

        public ObservableCollection<Alarm> FilteredAlarms
        {
            get => _filteredAlarms;
            set => SetProperty(ref _filteredAlarms, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterAlarms();
            }
        }

        public AlarmType? SelectedAlarmType
        {
            get => _selectedAlarmType;
            set
            {
                SetProperty(ref _selectedAlarmType, value);
                FilterAlarms();
            }
        }

        public bool ShowActiveOnly
        {
            get => _showActiveOnly;
            set
            {
                SetProperty(ref _showActiveOnly, value);
                FilterAlarms();
            }
        }

        public string AlarmSummary
        {
            get => _alarmSummary;
            set => SetProperty(ref _alarmSummary, value);
        }

        public List<AlarmTypeItem> AlarmTypes { get; }

        private AlarmTypeItem _selectedAlarmTypeItem;
        public AlarmTypeItem SelectedAlarmTypeItem
        {
            get => _selectedAlarmTypeItem;
            set
            {
                SetProperty(ref _selectedAlarmTypeItem, value);
                SelectedAlarmType = value?.Type;
            }
        }

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                await LoadAlarmsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing alarms: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadAlarmsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SiteId))
                {
                    System.Diagnostics.Debug.WriteLine("SiteId is empty");
                    return;
                }

                // Generate mock alarm history data
                var mockAlarms = GenerateMockAlarmHistory();

                // Update alarms collection
                Alarms.Clear();
                foreach (var alarm in mockAlarms.OrderByDescending(a => a.StartTime))
                {
                    Alarms.Add(alarm);
                }

                // Apply current filters
                FilterAlarms();

                System.Diagnostics.Debug.WriteLine($"Loaded {Alarms.Count} alarms for site {SiteId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAlarmsAsync error: {ex.Message}");
            }
        }

        private List<Alarm> GenerateMockAlarmHistory()
        {
            var alarms = new List<Alarm>();
            var random = new Random();
            var sensorIds = new[] { "GD001", "GD002", "GD003", "TS001" };
            var alarmTypes = Enum.GetValues<AlarmType>();

            // Generate 30 days of alarm history
            var startDate = DateTime.Now.AddDays(-30);

            for (int i = 0; i < 50; i++) // Generate 50 mock alarms
            {
                var sensorId = sensorIds[random.Next(sensorIds.Length)];
                var alarmType = alarmTypes[random.Next(alarmTypes.Length)];
                var startTime = startDate.AddHours(random.Next(24 * 30)); // Random time in last 30 days
                var isActive = random.Next(10) < 2; // 20% chance of being active

                var alarm = new Alarm
                {
                    Id = i + 1,
                    SensorId = sensorId,
                    SiteId = SiteId,
                    Type = alarmType,
                    Message = GenerateAlarmMessage(sensorId, alarmType),
                    Value = (float)(random.NextDouble() * 100),
                    StartTime = startTime,
                    EndTime = isActive ? null : startTime.AddMinutes(random.Next(5, 120)), // 5 min to 2 hours duration
                    IsActive = isActive,
                    Priority = GetAlarmPriority(alarmType)
                };

                alarms.Add(alarm);
            }

            return alarms;
        }

        private string GenerateAlarmMessage(string sensorId, AlarmType type)
        {
            return type switch
            {
                AlarmType.AlarmLevel1 => $"{sensorId}: Low level alarm triggered",
                AlarmType.AlarmLevel2 => $"{sensorId}: High level alarm triggered",
                AlarmType.LineOpenFault => $"{sensorId}: Line open fault detected",
                AlarmType.LineShortFault => $"{sensorId}: Line short fault detected",
                AlarmType.DetectorError => $"{sensorId}: Detector malfunction",
                AlarmType.DetectorDisabled => $"{sensorId}: Detector disabled",
                AlarmType.CommunicationLoss => $"{sensorId}: Communication timeout",
                _ => $"{sensorId}: Unknown alarm condition"
            };
        }

        private AlarmPriority GetAlarmPriority(AlarmType type)
        {
            return type switch
            {
                AlarmType.AlarmLevel2 => AlarmPriority.Critical,
                AlarmType.DetectorError => AlarmPriority.High,
                AlarmType.LineOpenFault => AlarmPriority.High,
                AlarmType.LineShortFault => AlarmPriority.High,
                AlarmType.AlarmLevel1 => AlarmPriority.Medium,
                AlarmType.CommunicationLoss => AlarmPriority.Medium,
                AlarmType.DetectorDisabled => AlarmPriority.Low,
                _ => AlarmPriority.Low
            };
        }

        private void FilterAlarms()
        {
            try
            {
                var filtered = Alarms.AsEnumerable();

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(a =>
                        a.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        a.SensorId.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by alarm type
                if (SelectedAlarmType.HasValue)
                {
                    filtered = filtered.Where(a => a.Type == SelectedAlarmType.Value);
                }

                // Filter by active status
                if (ShowActiveOnly)
                {
                    filtered = filtered.Where(a => a.IsActive);
                }

                // Update filtered collection
                FilteredAlarms.Clear();
                foreach (var alarm in filtered.OrderByDescending(a => a.StartTime))
                {
                    FilteredAlarms.Add(alarm);
                }

                // Update summary
                UpdateAlarmSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FilterAlarms error: {ex.Message}");
            }
        }

        private void UpdateAlarmSummary()
        {
            var total = FilteredAlarms.Count;
            var active = FilteredAlarms.Count(a => a.IsActive);
            var critical = FilteredAlarms.Count(a => a.Priority == AlarmPriority.Critical);
            var high = FilteredAlarms.Count(a => a.Priority == AlarmPriority.High);

            AlarmSummary = $"Total: {total} | Active: {active} | Critical: {critical} | High: {high}";
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedAlarmTypeItem = AlarmTypes[0]; // Reset to "All Types"
            ShowActiveOnly = false;
        }
    }

    // Helper class for alarm type picker
    public class AlarmTypeItem
    {
        public string Name { get; set; } = string.Empty;
        public AlarmType? Type { get; set; }
    }
}