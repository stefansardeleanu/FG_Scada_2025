// Services/DataService.cs - Debug version with enhanced logging
using System.Collections.ObjectModel;
using System.Text.Json;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Services
{
    public class DataService
    {
        private List<County> _counties = new List<County>();
        private List<User> _users = new List<User>();
        private Dictionary<int, Site> _discoveredSites = new Dictionary<int, Site>();

        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 DataService.InitializeAsync called");
                await LoadCountiesAsync();
                await LoadUsersAsync();
                System.Diagnostics.Debug.WriteLine($"✅ DataService.InitializeAsync completed. Users loaded: {_users.Count}");

                // Debug: Print all loaded users
                foreach (var user in _users)
                {
                    System.Diagnostics.Debug.WriteLine($"👤 User: {user.Username} | Role: {user.Role} | SiteIDs: [{string.Join(",", user.AllowedSiteIDs)}]");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DataService.InitializeAsync error: {ex.Message}");
            }
        }

        #region User Management

        private async Task LoadUsersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Loading users configuration...");

                var usersConfigJson = await LoadJsonFileAsync("Users.json");
                System.Diagnostics.Debug.WriteLine($"📄 Users.json content: {usersConfigJson}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var usersConfig = JsonSerializer.Deserialize<UsersConfig>(usersConfigJson, options);

                if (usersConfig?.Users != null && usersConfig.Users.Any())
                {
                    _users = usersConfig.Users.Select(userConfig => new User
                    {
                        Id = userConfig.Id,
                        Username = userConfig.Username,
                        Password = userConfig.Password,
                        Role = Enum.Parse<UserRole>(userConfig.Role),
                        AllowedSiteIDs = userConfig.AllowedSiteIDs
                    }).ToList();

                    System.Diagnostics.Debug.WriteLine($"✅ Loaded {_users.Count} users from Users.json");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No users found in Users.json or file is empty, loading fallback users");
                    LoadFallbackUsers();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading users: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("⚠️ Loading fallback users due to error");
                LoadFallbackUsers();
            }
        }

        private void LoadFallbackUsers()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Loading fallback hardcoded users...");

            _users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin",
                    Role = UserRole.CEO,
                    AllowedSiteIDs = new List<int>() // CEO sees all
                },
                new User
                {
                    Id = 2,
                    Username = "manager",
                    Password = "manager",
                    Role = UserRole.RegionalManager,
                    AllowedSiteIDs = new List<int> { 5 }
                },
                new User
                {
                    Id = 3,
                    Username = "operator",
                    Password = "operator",
                    Role = UserRole.PlantOperator,
                    AllowedSiteIDs = new List<int> { 5 }
                }
            };

            System.Diagnostics.Debug.WriteLine($"✅ Loaded {_users.Count} fallback users");
        }

        public User? ValidateUser(string username, string password)
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Validating user: '{username}' with password: '{password}'");
            System.Diagnostics.Debug.WriteLine($"📊 Available users: {_users.Count}");

            foreach (var user in _users)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Checking: {user.Username} == {username} && {user.Password} == {password}");
                if (user.Username == username && user.Password == password)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ User validation successful: {user.Username} ({user.Role})");
                    return user;
                }
            }

            System.Diagnostics.Debug.WriteLine($"❌ User validation failed for: {username}");
            return null;
        }

        public void SetCurrentUser(User user)
        {
            Preferences.Set("CurrentUserId", user.Id);
            Preferences.Set("CurrentUserName", user.Username);
            Preferences.Set("CurrentUserRole", user.Role.ToString());
            System.Diagnostics.Debug.WriteLine($"💾 Stored current user: {user.Username} ({user.Role})");
        }

        #endregion

        #region County & Site Configuration (No sensor data)

        public async Task<List<County>> GetCountiesAsync()
        {
            if (_counties.Count == 0)
            {
                await LoadCountiesAsync();
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
                System.Diagnostics.Debug.WriteLine("🔄 Loading counties configuration...");

                // Load main counties list
                var countiesConfigJson = await LoadJsonFileAsync("CountiesConfig.json");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var countiesConfig = JsonSerializer.Deserialize<CountiesConfigRoot>(countiesConfigJson, options);

                if (countiesConfig?.Counties != null)
                {
                    _counties = countiesConfig.Counties;

                    // Load sites for each county
                    foreach (var county in _counties)
                    {
                        await LoadCountySitesAsync(county);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_counties.Count} counties");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading counties: {ex.Message}");
            }
        }

        private async Task LoadCountySitesAsync(County county)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Loading sites for county: {county.Name}");

                var countyConfigJson = await LoadJsonFileAsync($"Counties/{county.Name}.json");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var countyConfig = JsonSerializer.Deserialize<CountyConfig>(countyConfigJson, options);

                if (countyConfig?.Sites != null)
                {
                    county.Sites = new List<Site>();

                    foreach (var siteConfig in countyConfig.Sites)
                    {
                        var site = new Site
                        {
                            SiteID = siteConfig.SiteID,
                            DisplayName = siteConfig.DisplayName,
                            CountyId = county.Id,
                            Sensors = new ObservableCollection<Sensor>(), // Empty - will be populated by autodiscovery
                            Status = new Models.SiteStatus() // Explicitly use Models.SiteStatus
                        };

                        county.Sites.Add(site);

                        // Also add to global discovered sites dictionary for MQTT mapping
                        _discoveredSites[site.SiteID] = site;
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ Loaded {countyConfig.Sites.Count} sites for county {county.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading sites for county {county.Name}: {ex.Message}");
            }
        }

        #endregion

        #region Site Discovery & MQTT Integration

        public Site? GetSiteByID(int siteID)
        {
            return _discoveredSites.TryGetValue(siteID, out var site) ? site : null;
        }

        public List<Site> GetAllConfiguredSites()
        {
            return _discoveredSites.Values.ToList();
        }

        public async Task<Site?> GetSiteAsync(string siteId)
        {
            // Convert string siteId to int for compatibility
            if (int.TryParse(siteId, out int siteID))
            {
                return GetSiteByID(siteID);
            }

            // Fallback: search by display name for backward compatibility
            return _discoveredSites.Values.FirstOrDefault(s =>
                s.DisplayName.Equals(siteId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<Site>> GetSitesForCountyAsync(string countyId)
        {
            var county = await GetCountyAsync(countyId);
            return county?.Sites ?? new List<Site>();
        }

        #endregion

        #region User Access Control

        public List<County> GetAllowedCounties(User user)
        {
            if (user.Role == UserRole.CEO)
                return _counties;

            // For other roles, return counties that contain sites the user has access to
            var allowedCounties = new List<County>();
            foreach (var county in _counties)
            {
                if (county.Sites.Any(site => user.HasAccessToSite(site.SiteID)))
                {
                    allowedCounties.Add(county);
                }
            }
            return allowedCounties;
        }

        public List<Site> GetAllowedSites(User user)
        {
            if (user.Role == UserRole.CEO)
            {
                return GetAllConfiguredSites();
            }

            return GetAllConfiguredSites()
                .Where(site => user.HasAccessToSite(site.SiteID))
                .ToList();
        }

        public User? GetCurrentUser()
        {
            try
            {
                var userId = Preferences.Get("CurrentUserId", -1);
                if (userId == -1) return null;

                return _users.FirstOrDefault(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error getting current user: {ex.Message}");
                return null;
            }
        }

        public void ClearCurrentUser()
        {
            Preferences.Remove("CurrentUserId");
            Preferences.Remove("CurrentUserName");
            Preferences.Remove("CurrentUserRole");
        }

        #endregion

        #region Helper Methods

        private async Task<string> LoadJsonFileAsync(string fileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Loading file: Data/{fileName}");
                using var stream = await FileSystem.OpenAppPackageFileAsync($"Data/{fileName}");
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                System.Diagnostics.Debug.WriteLine($"✅ File loaded: {fileName} ({content.Length} chars)");
                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading {fileName}: {ex.Message}");
                return "{}";
            }
        }

        #endregion
    }
}