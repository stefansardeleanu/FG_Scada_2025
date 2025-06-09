using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG_Scada_2025.Models
{
    public class Alarm
    {
        public int Id { get; set; }
        public string SensorId { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public AlarmType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public float Value { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; }
        public AlarmPriority Priority { get; set; }
    }

    public enum AlarmType
    {
        AlarmLevel1,
        AlarmLevel2,
        LineOpenFault,
        LineShortFault,
        DetectorError,
        DetectorDisabled,
        CommunicationLoss
    }

    public enum AlarmPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
