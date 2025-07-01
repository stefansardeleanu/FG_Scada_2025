using System.Text.Json.Serialization;

namespace FG_Scada_2025.Models
{
    // Configuration file models (no sensor data, only site metadata)
    public class CountiesConfigRoot
    {
        public List<County> Counties { get; set; } = new List<County>();
    }

    public class CountyConfig
    {
        public string CountyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<SiteConfig> Sites { get; set; } = new List<SiteConfig>();
    }

    public class SiteConfig
    {
        public int SiteID { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class UsersConfig
    {
        public List<UserConfig> Users { get; set; } = new List<UserConfig>();
    }

    public class UserConfig
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<int> AllowedSiteIDs { get; set; } = new List<int>();
        public string Comment { get; set; } = string.Empty;
    }
}