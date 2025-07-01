namespace FG_Scada_2025.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }

        // Changed from county/site names to siteIDs for direct mapping
        public List<int> AllowedSiteIDs { get; set; } = new List<int>();

        // Helper method to check site access
        public bool HasAccessToSite(int siteID)
        {
            // CEO has access to all sites
            if (Role == UserRole.CEO)
                return true;

            // Others must be explicitly granted access
            return AllowedSiteIDs.Contains(siteID);
        }
    }

    public enum UserRole
    {
        CEO,             // Can see all sites
        RegionalManager, // Can see specific sites 
        PlantOperator    // Can see specific sites only
    }
}