using System.Collections.ObjectModel;
using System.Windows.Input;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;

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
        private AlarmTypeItem _selectedAlarmTypeItem;

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
                new AlarmTypeItem { Name = "Detector Disabled", Type = AlarmType.DetectorDisabled },
                new AlarmTypeItem { Name = "Communication Loss", Type = AlarmType.CommunicationLoss }
            };

            _selectedAlarmTypeItem = AlarmTypes[0]; // Default to "All Types"
        }

        #region Properties

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

        #endregion

        #region Initialization

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
                System.Diagnostics.Debug.WriteLine($"Error initializing alarm history: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadAlarmsAsync()
        {
            try
            {
                // Generate mock alarm data for now
                // In a real implementation, this would load from your database
                var mockAlarms = GenerateMockAlarms();

                Alarms.Clear();
                foreach (var alarm in mockAlarms.OrderByDescending(a => a.StartTime))
                {
                    Alarms.Add(alarm);
                }

                FilterAlarms();
                UpdateAlarmSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading alarms: {ex.Message}");
            }
        }

        private List<Alarm> GenerateMockAlarms()
        {
            var random = new Random();
            var alarms = new List<Alarm>();
            var now = DateTime.Now;

            // Generate 50 mock alarms
            for (int i = 0; i < 50; i++)
            {
                var startTime = now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24));
                var isActive = random.Next(10) < 3; // 30% chance of being active

                var alarm = new Alarm
                {
                    Id = i + 1,
                    SiteId = SiteId,
                    SensorId = $"CH{random.Next(40, 50)}",
                    SensorTag = $"KGD-{random.Next(1, 20):D3}",
                    SensorName = $"Gas Detector {random.Next(1, 20)}",
                    Type = GetRandomAlarmType(random),
                    Severity = GetRandomAlarmSeverity(random),
                    Priority = GetRandomAlarmPriority(random),
                    Message = GetAlarmMessage(i),
                    Value = (float)(random.NextDouble() * 100),
                    Unit = random.Next(2) == 0 ? "%LEL" : "PPM",
                    Timestamp = startTime,
                    StartTime = startTime,
                    EndTime = isActive ? null : startTime.AddMinutes(random.Next(5, 120)),
                    IsActive = isActive,
                    AcknowledgedAt = random.Next(10) < 7 ? startTime.AddMinutes(random.Next(1, 30)) : null,
                    AcknowledgedBy = random.Next(10) < 7 ? $"User{random.Next(1, 5)}" : null
                };

                alarms.Add(alarm);
            }

            return alarms;
        }

        private AlarmType GetRandomAlarmType(Random random)
        {
            var types = Enum.GetValues<AlarmType>();
            return types[random.Next(types.Length)];
        }

        private AlarmSeverity GetRandomAlarmSeverity(Random random)
        {
            var severities = Enum.GetValues<AlarmSeverity>();
            return severities[random.Next(severities.Length)];
        }

        private AlarmPriority GetRandomAlarmPriority(Random random)
        {
            var priorities = Enum.GetValues<AlarmPriority>();
            return priorities[random.Next(priorities.Length)];
        }

        private string GetAlarmMessage(int index)
        {
            var messages = new[]
            {
                "Gas concentration above alarm level",
                "Detector communication failure",
                "Line open fault detected",
                "Line short circuit fault",
                "Detector requires calibration",
                "High gas concentration detected",
                "Detector disabled by user",
                "Communication timeout",
                "Sensor reading out of range",
                "Maintenance required"
            };

            return messages[index % messages.Length];
        }

        #endregion

        #region Filtering

        private void FilterAlarms()
        {
            try
            {
                var filtered = Alarms.AsEnumerable();

                // Filter by search text
                if (!string.IsNullOrEmpty(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(a =>
                        a.SensorTag.ToLower().Contains(searchLower) ||
                        a.SensorName.ToLower().Contains(searchLower) ||
                        a.Message.ToLower().Contains(searchLower));
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

                FilteredAlarms.Clear();
                foreach (var alarm in filtered.OrderByDescending(a => a.StartTime))
                {
                    FilteredAlarms.Add(alarm);
                }

                UpdateAlarmSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering alarms: {ex.Message}");
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedAlarmTypeItem = AlarmTypes[0];
            ShowActiveOnly = false;
        }

        private void UpdateAlarmSummary()
        {
            var total = FilteredAlarms.Count;
            var active = FilteredAlarms.Count(a => a.IsActive);
            var acknowledged = FilteredAlarms.Count(a => a.AcknowledgedAt.HasValue);

            AlarmSummary = $"Total: {total} | Active: {active} | Acknowledged: {acknowledged}";
        }

        #endregion
    }
}