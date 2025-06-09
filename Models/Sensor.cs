using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG_Scada_2025.Models
{
    public class Sensor
    {
        public string Id { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public SensorType Type { get; set; }
        public SensorValue CurrentValue { get; set; } = new SensorValue();
        public SensorAlarms Alarms { get; set; } = new SensorAlarms();
        public SensorConfig Config { get; set; } = new SensorConfig();
    }

    public enum SensorType
    {
        GasDetector,
        TemperatureSensor,
        PressureSensor,
        FlowSensor
    }

    public class SensorValue
    {
        public float ProcessValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public SensorStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum SensorStatus
    {
        Normal,
        AlarmLevel1,
        AlarmLevel2,
        LineOpenFault,
        LineShortFault,
        DetectorError,
        DetectorDisabled
    }

    public class SensorAlarms
    {
        public float AlarmLevel1 { get; set; }
        public float AlarmLevel2 { get; set; }
        public bool IsAlarmLevel1Active { get; set; }
        public bool IsAlarmLevel2Active { get; set; }
    }

    public class SensorConfig
    {
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string ModbusAddress { get; set; } = string.Empty;
        public int UpdateInterval { get; set; } = 5; // seconds
    }
}