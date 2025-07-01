namespace FG_Scada_2025.Models
{
    public class Alarm
    {
        public int Id { get; set; }
        public string SiteId { get; set; } = string.Empty;
        public string SensorId { get; set; } = string.Empty; // Added for compatibility
        public string SensorTag { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
        public AlarmType Type { get; set; }
        public AlarmSeverity Severity { get; set; }
        public AlarmPriority Priority { get; set; } // Added for compatibility
        public string Message { get; set; } = string.Empty;
        public float Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime StartTime { get; set; } // Added for compatibility
        public DateTime? EndTime { get; set; } // Added for compatibility
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ResolvedAt { get; set; }
    }

    public enum AlarmType
    {
        AlarmLevel1,
        AlarmLevel2,
        DetectorError,
        DetectorDisabled,
        LineOpenFault,
        LineShortFault,
        CommunicationLoss,
        CalibrationRequired,
        MaintenanceRequired
    }

    public enum AlarmSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlarmPriority // Added for compatibility
    {
        Low,
        Medium,
        High,
        Critical,
        Emergency
    }

    // Helper class for alarm type picker
    public class AlarmTypeItem
    {
        public string Name { get; set; } = string.Empty;
        public AlarmType? Type { get; set; }
    }
}