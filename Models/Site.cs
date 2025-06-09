using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG_Scada_2025.Models
{
    public class Site
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CountyId { get; set; } = string.Empty;
        public List<Sensor> Sensors { get; set; } = new List<Sensor>();
        public SitePosition Position { get; set; } = new SitePosition();
        public PLCConnection PlcConnection { get; set; } = new PLCConnection();
        public SiteStatus Status { get; set; } = new SiteStatus();
    }

    public class SitePosition
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class PLCConnection
    {
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Protocol { get; set; } = "MQTT"; // MQTT, Modbus, OPC-UA
        public string Topic { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class SiteStatus
    {
        public bool HasAlarm { get; set; }
        public bool HasFault { get; set; }
        public bool IsNormal => !HasAlarm && !HasFault;
        public DateTime LastUpdate { get; set; }
    }
}
