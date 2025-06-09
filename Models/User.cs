using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG_Scada_2025.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public List<string> AllowedCounties { get; set; } = new List<string>();
        public List<string> AllowedSites { get; set; } = new List<string>();
    }

    public enum UserRole
    {
        CEO,           // Can see all counties and sites
        RegionalManager, // Can see specific counties
        PlantOperator    // Can see specific sites only
    }
}