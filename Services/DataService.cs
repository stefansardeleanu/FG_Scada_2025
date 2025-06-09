using System.Text.Json;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Services
{
    public class DataService
    {
        private List<County> _counties = new List<County>();
        private List<User> _users = new List<User>();
        private Dictionary<string, Site> _sites = new Dictionary<string, Site>();

        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DataService.InitializeAsync called");
                await LoadCountiesAsync();
                System.Diagnostics.Debug.WriteLine($"DataService.InitializeAsync completed. _counties.Count = {_counties.Count}");
                await LoadSitesAsync();
                LoadMockUsers();
                System.Diagnostics.Debug.WriteLine("DataService.InitializeAsync fully completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DataService.InitializeAsync error: {ex.Message}");
            }
        }

        #region County Data
        public async Task<List<County>> GetCountiesAsync()
        {
            if (_counties.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("_counties is empty, calling LoadCountiesAsync");
                await LoadCountiesAsync();
                System.Diagnostics.Debug.WriteLine($"After LoadCountiesAsync: _counties.Count = {_counties.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"_counties already has {_counties.Count} counties");
            }
            return _counties;
        }

        public async Task<County?> GetCountyAsync(string countyId)
        {
            var counties = await GetCountiesAsync();
            return counties.FirstOrDefault(c => c.Id == countyId);
        }

        private async Task LoadCountiesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("DataService.LoadCountiesAsync starting...");

                // Load main counties configuration
                var countiesConfigJson = await LoadJsonFileAsync("CountiesConfig.json");
                System.Diagnostics.Debug.WriteLine($"Loaded JSON: {countiesConfigJson.Length} characters");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var countiesConfig = JsonSerializer.Deserialize<CountiesConfigRoot>(countiesConfigJson, options);
                System.Diagnostics.Debug.WriteLine($"Deserialized: {countiesConfig?.Counties?.Count ?? 0} counties");

                if (countiesConfig?.Counties != null)
                {
                    _counties = countiesConfig.Counties;
                    System.Diagnostics.Debug.WriteLine($"Assigned to _counties: {_counties.Count} counties");

                    // Simple debug
                    System.Diagnostics.Debug.WriteLine($"BEFORE LoadCountyDetails - County ROGJ has: {_counties.FirstOrDefault(c => c.Id == "ROGJ")?.Sites?.Count ?? 0} sites");
                }

                // Load detailed county data
                foreach (var county in _counties)
                {
                    await LoadCountyDetailsAsync(county);
                }

                // Simple debug
                System.Diagnostics.Debug.WriteLine($"AFTER LoadCountyDetails - County ROGJ has: {_counties.FirstOrDefault(c => c.Id == "ROGJ")?.Sites?.Count ?? 0} sites");

                System.Diagnostics.Debug.WriteLine($"DataService.LoadCountiesAsync completed with {_counties.Count} counties");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading counties: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadCountyDetailsAsync(County county)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading details for county: {county.Name}");

                var countyDetailJson = await LoadJsonFileAsync($"Counties/{county.Name}.json");
                System.Diagnostics.Debug.WriteLine($"Loaded county detail JSON: {countyDetailJson.Length} characters");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var countyDetail = JsonSerializer.Deserialize<CountyDetail>(countyDetailJson, options);

                if (countyDetail?.Sites != null)
                {
                    county.Sites = countyDetail.Sites;
                    System.Diagnostics.Debug.WriteLine($"Loaded {countyDetail.Sites.Count} sites for county {county.Name}");

                    // Load detailed sensor data for each site
                    foreach (var site in county.Sites)
                    {
                        await LoadDetailedSiteDataAsync(site);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No sites found in county detail for {county.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading county details for {county.Name}: {ex.Message}");
            }
        }
        #endregion



        private async Task LoadDetailedSiteDataAsync(Site site)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading detailed data for site: {site.Id}");

                var siteJson = await LoadJsonFileAsync($"Sites/{site.Id}.json");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var siteDetail = JsonSerializer.Deserialize<SiteDetail>(siteJson, options);

                if (siteDetail?.Sensors != null)
                {
                    site.Sensors = siteDetail.Sensors;
                    System.Diagnostics.Debug.WriteLine($"Loaded {siteDetail.Sensors.Count} sensors for site {site.Id}");
                }

                // Update other site details if needed
                if (siteDetail != null)
                {
                    site.PlcConnection = siteDetail.PlcConnection;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading detailed site data for {site.Id}: {ex.Message}");
            }
        }

        #region Site Data
        public async Task<Site?> GetSiteAsync(string siteId)
        {
            if (!_sites.ContainsKey(siteId))
                await LoadSiteAsync(siteId);

            return _sites.TryGetValue(siteId, out var site) ? site : null;
        }

        public async Task<List<Site>> GetSitesForCountyAsync(string countyId)
        {
            var county = await GetCountyAsync(countyId);
            var sites = county?.Sites ?? new List<Site>();
            System.Diagnostics.Debug.WriteLine($"GetSitesForCountyAsync for {countyId}: returning {sites.Count} sites");
            return sites;
        }

        private async Task LoadSitesAsync()
        {
            // Pre-load known sites
            await LoadSiteAsync("PanouHurezani");
        }

        private async Task LoadSiteAsync(string siteId)
        {
            try
            {
                var siteJson = await LoadJsonFileAsync($"Sites/{siteId}.json");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var siteDetail = JsonSerializer.Deserialize<SiteDetail>(siteJson, options);

                if (siteDetail != null)
                {
                    var site = new Site
                    {
                        Id = siteDetail.SiteId,
                        Name = siteDetail.Name,
                        DisplayName = siteDetail.DisplayName,
                        CountyId = siteDetail.CountyId,
                        PlcConnection = siteDetail.PlcConnection,
                        Sensors = siteDetail.Sensors ?? new List<Sensor>()
                    };

                    _sites[siteId] = site;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading site {siteId}: {ex.Message}");
            }
        }
        #endregion

        #region User Data
        public User? ValidateUser(string username, string password)
        {
            return _users.FirstOrDefault(u => u.Username == username && u.Password == password);
        }

        public List<County> GetAllowedCounties(User user)
        {
            if (user.Role == UserRole.CEO)
                return _counties;

            return _counties.Where(c => user.AllowedCounties.Contains(c.Id)).ToList();
        }

        public List<Site> GetAllowedSites(User user)
        {
            var allowedSites = new List<Site>();

            if (user.Role == UserRole.CEO)
            {
                foreach (var county in _counties)
                    allowedSites.AddRange(county.Sites);
            }
            else if (user.Role == UserRole.RegionalManager)
            {
                var allowedCounties = GetAllowedCounties(user);
                foreach (var county in allowedCounties)
                    allowedSites.AddRange(county.Sites);
            }
            else if (user.Role == UserRole.PlantOperator)
            {
                foreach (var county in _counties)
                {
                    allowedSites.AddRange(county.Sites.Where(s => user.AllowedSites.Contains(s.Id)));
                }
            }

            return allowedSites;
        }

        public User? GetCurrentUser()
        {
            try
            {
                var userId = Preferences.Get("CurrentUserId", -1);
                if (userId == -1) return null;

                var username = Preferences.Get("CurrentUserName", string.Empty);
                var roleString = Preferences.Get("CurrentUserRole", string.Empty);

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roleString))
                    return null;

                if (Enum.TryParse<UserRole>(roleString, out var role))
                {
                    return _users.FirstOrDefault(u => u.Id == userId && u.Username == username);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting current user: {ex.Message}");
            }

            return null;
        }

        public void ClearCurrentUser()
        {
            Preferences.Remove("CurrentUserId");
            Preferences.Remove("CurrentUserName");
            Preferences.Remove("CurrentUserRole");
        }

        private void LoadMockUsers()
        {
            _users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin",
                    Role = UserRole.CEO,
                    AllowedCounties = new List<string>(), // CEO sees all
                    AllowedSites = new List<string>()     // CEO sees all
                },
                new User
                {
                    Id = 2,
                    Username = "manager",
                    Password = "manager",
                    Role = UserRole.RegionalManager,
                    AllowedCounties = new List<string> { "ROGJ" },
                    AllowedSites = new List<string>()
                },
                new User
                {
                    Id = 3,
                    Username = "operator",
                    Password = "operator",
                    Role = UserRole.PlantOperator,
                    AllowedCounties = new List<string>(),
                    AllowedSites = new List<string> { "PanouHurezani" }
                }
            };
        }
        #endregion

        #region Helper Methods
        private async Task<string> LoadJsonFileAsync(string fileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to load file: Data/{fileName}");
                using var stream = await FileSystem.OpenAppPackageFileAsync($"Data/{fileName}");
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                System.Diagnostics.Debug.WriteLine($"Successfully loaded {fileName}: {content.Length} characters");
                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading {fileName}: {ex.Message}");
                return "{}";
            }
        }
        #endregion
    }

    // Helper classes for JSON deserialization
    public class CountiesConfigRoot
    {
        public List<County> Counties { get; set; } = new List<County>();
    }

    public class CountyDetail
    {
        public string CountyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string MapFilePath { get; set; } = string.Empty;
        public List<Site> Sites { get; set; } = new List<Site>();
    }

    public class SiteDetail
    {
        public string SiteId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CountyId { get; set; } = string.Empty;
        public PLCConnection PlcConnection { get; set; } = new PLCConnection();
        public List<Sensor> Sensors { get; set; } = new List<Sensor>();
    }
}