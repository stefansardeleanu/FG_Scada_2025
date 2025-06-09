using System.Collections.ObjectModel;
using System.Windows.Input;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;
using FG_Scada_2025.Helpers;

namespace FG_Scada_2025.ViewModels
{
    [QueryProperty(nameof(CountyId), "CountyId")]
    [QueryProperty(nameof(CountyName), "CountyName")]
    [QueryProperty(nameof(CountyDisplayName), "CountyDisplayName")]
    public class CountyViewModel : BaseViewModel
    {
        private readonly DataService _dataService;
        private readonly NavigationService _navigationService;

        private string _countyId = string.Empty;
        private string _countyName = string.Empty;
        private string _countyDisplayName = string.Empty;
        private County? _county;
        private ObservableCollection<Site> _sites = new ObservableCollection<Site>();
        private string _statusSummary = string.Empty;

        public CountyViewModel(DataService dataService, NavigationService navigationService)
        {
            _dataService = dataService;
            _navigationService = navigationService;

            BackCommand = new Command(async () => await _navigationService.GoBackAsync());
            RefreshCommand = new Command(async () => await LoadCountyDataAsync());
        }

        public string CountyId
        {
            get => _countyId;
            set => SetProperty(ref _countyId, value);
        }

        public string CountyName
        {
            get => _countyName;
            set => SetProperty(ref _countyName, value);
        }

        public string CountyDisplayName
        {
            get => _countyDisplayName;
            set
            {
                SetProperty(ref _countyDisplayName, value);
                Title = $"{value} - Sites";
            }
        }

        public County? County
        {
            get => _county;
            set => SetProperty(ref _county, value);
        }

        public ObservableCollection<Site> Sites
        {
            get => _sites;
            set => SetProperty(ref _sites, value);
        }

        public string StatusSummary
        {
            get => _statusSummary;
            set => SetProperty(ref _statusSummary, value);
        }

        public ICommand BackCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                await LoadCountyDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing county: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCountyDataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(CountyId))
                {
                    System.Diagnostics.Debug.WriteLine("CountyId is empty");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Loading data for CountyId: {CountyId}");

                // Load county data
                County = await _dataService.GetCountyAsync(CountyId);
                if (County == null)
                {
                    System.Diagnostics.Debug.WriteLine("County is null from DataService");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"County loaded: {County.Name}, Sites: {County.Sites?.Count ?? 0}");

                // Load sites for this county
                var sites = await _dataService.GetSitesForCountyAsync(CountyId);
                System.Diagnostics.Debug.WriteLine($"GetSitesForCountyAsync returned: {sites.Count} sites");

                // Generate mock status data for demonstration
                var random = new Random();
                foreach (var site in sites)
                {
                    // Generate random sensor data
                    foreach (var sensor in site.Sensors)
                    {
                        sensor.CurrentValue.ProcessValue = (float)(random.NextDouble() * 100);
                        sensor.CurrentValue.Status = GetRandomSensorStatus(random);
                        sensor.CurrentValue.Timestamp = DateTime.Now;
                    }

                    // Update site status based on sensors
                    var (hasAlarm, hasFault) = StatusHelper.GetSiteStatus(site.Sensors);
                    site.Status.HasAlarm = hasAlarm;
                    site.Status.HasFault = hasFault;
                    site.Status.LastUpdate = DateTime.Now;

                    // Mock PLC connection status
                    site.PlcConnection.IsConnected = random.Next(10) > 1; // 90% connected
                    site.PlcConnection.LastUpdate = DateTime.Now;
                }

                // Update sites collection - Fixed: Clear first, then add
                Sites.Clear();
                foreach (var site in sites)
                {
                    Sites.Add(site);
                }

                System.Diagnostics.Debug.WriteLine($"Final Sites.Count: {Sites.Count}");

                // Update status summary
                UpdateStatusSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCountyDataAsync error: {ex.Message}");
            }
        }

        private void UpdateStatusSummary()
        {
            var totalSites = Sites.Count;
            var normalSites = Sites.Count(s => s.Status.IsNormal);
            var alarmSites = Sites.Count(s => s.Status.HasAlarm);
            var faultSites = Sites.Count(s => s.Status.HasFault);

            StatusSummary = $"Total: {totalSites} | Normal: {normalSites} | Alarms: {alarmSites} | Faults: {faultSites}";
        }

        private SensorStatus GetRandomSensorStatus(Random random)
        {
            var statusValues = Enum.GetValues<SensorStatus>();
            var weights = new int[] { 70, 15, 8, 3, 2, 1, 1 }; // Normal is most likely

            var totalWeight = weights.Sum();
            var randomValue = random.Next(totalWeight);

            var currentWeight = 0;
            for (int i = 0; i < statusValues.Length && i < weights.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue < currentWeight)
                    return statusValues[i];
            }

            return SensorStatus.Normal;
        }

        public async Task OnSiteTappedAsync(Site site)
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["SiteId"] = site.Id,
                    ["SiteName"] = site.Name,
                    ["SiteDisplayName"] = site.DisplayName,
                    ["CountyName"] = CountyName
                };

                await _navigationService.NavigateToAsync("SitePage", parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to site: {ex.Message}");
            }
        }
    }
}