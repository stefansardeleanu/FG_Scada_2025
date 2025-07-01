// Services/AutodiscoveryService.cs - Enhanced for disabled status handling
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Services
{
    public class AutodiscoveryService
    {
        private readonly DataService _dataService;
        private readonly RealTimeDataService _realTimeDataService;
        private readonly ConcurrentDictionary<string, Sensor> _discoveredSensors = new();

        // Events for UI updates
        public event EventHandler<SensorDiscoveredEventArgs>? SensorDiscovered;
        public event EventHandler<SiteUpdatedEventArgs>? SiteUpdated;

        public AutodiscoveryService(DataService dataService, RealTimeDataService realTimeDataService)
        {
            _dataService = dataService;
            _realTimeDataService = realTimeDataService;

            // Subscribe to MQTT sensor data updates
            _realTimeDataService.SensorDataUpdated += OnSensorDataReceived;
        }

        private async void OnSensorDataReceived(object? sender, SensorDataUpdatedEventArgs e)
        {
            var sensorData = e.SensorData;

            // Skip raw messages
            if (sensorData.SiteName == "Raw")
                return;

            System.Diagnostics.Debug.WriteLine($"🔍 Processing autodiscovery for Site {sensorData.SiteId}, Sensor {sensorData.TagName}, Status: {sensorData.Status}");

            // Get the configured site from DataService
            var site = _dataService.GetSiteByID(sensorData.SiteId);
            if (site == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Site {sensorData.SiteId} not found in configuration - skipping autodiscovery");
                return;
            }

            // Update site connection status
            site.LastMqttUpdate = DateTime.Now;

            // Discover or update sensor in this site
            var sensor = await DiscoverOrUpdateSensorAsync(sensorData, site);

            // Add sensor to site if it's new
            if (!site.Sensors.Any(s => s.Tag == sensor.Tag && s.Id == sensor.Id))
            {
                // Insert sensor in sorted order by Tag name
                var insertIndex = GetSortedInsertIndex(site.Sensors, sensor.Tag);
                site.Sensors.Insert(insertIndex, sensor);

                System.Diagnostics.Debug.WriteLine($"✅ Added new sensor {sensor.Tag} to site {site.DisplayName} (Status: {sensor.CurrentValue.Status})");

                // Notify that a new sensor was discovered
                SensorDiscovered?.Invoke(this, new SensorDiscoveredEventArgs(sensor, site));
            }

            // Update site status based on all sensors
            UpdateSiteStatus(site);

            // Notify that site was updated
            SiteUpdated?.Invoke(this, new SiteUpdatedEventArgs(site));
        }

        private async Task<Sensor> DiscoverOrUpdateSensorAsync(SensorData sensorData, Site site)
        {
            string sensorKey = $"{sensorData.SiteId}_{sensorData.ChannelId}_{sensorData.TagName}";

            if (!_discoveredSensors.TryGetValue(sensorKey, out var sensor))
            {
                // Create new sensor with autodiscovered data
                sensor = new Sensor
                {
                    Id = sensorData.ChannelId,
                    Tag = sensorData.TagName,
                    Name = sensorData.TagName, // Use tag as name initially
                    SiteId = sensorData.SiteId.ToString(),
                    Type = MapDetectorTypeToSensorType(sensorData.DetectorType),
                    CurrentValue = new SensorValue
                    {
                        ProcessValue = (float)sensorData.ProcessValue,
                        Unit = GetDetectorUnits(sensorData.DetectorType),
                        Status = sensorData.Status, // This will be DetectorDisabled when Status=4
                        Timestamp = sensorData.Timestamp
                    },
                    Alarms = new SensorAlarms
                    {
                        AlarmLevel1 = GetDefaultAlarmLevel1(sensorData.DetectorType),
                        AlarmLevel2 = GetDefaultAlarmLevel2(sensorData.DetectorType)
                    },
                    Config = new SensorConfig
                    {
                        MinValue = sensorData.DetectorType <= 2 ? 0 : 4,
                        MaxValue = sensorData.DetectorType <= 2 ? 100 : 20,
                        UpdateInterval = 5
                    }
                };

                _discoveredSensors[sensorKey] = sensor;

                var statusText = GetStatusDescription(sensorData.Status);
                System.Diagnostics.Debug.WriteLine($"🔍 Discovered new sensor: {sensor.Tag} - Type: {GetDetectorTypeName(sensorData.DetectorType)}, Status: {statusText}");
            }
            else
            {
                // Update existing sensor with latest data
                var oldStatus = sensor.CurrentValue.Status;
                sensor.CurrentValue.ProcessValue = (float)sensorData.ProcessValue;
                sensor.CurrentValue.Status = sensorData.Status;
                sensor.CurrentValue.Timestamp = sensorData.Timestamp;
                sensor.CurrentValue.Unit = GetDetectorUnits(sensorData.DetectorType);

                // Log status changes for disabled sensors
                if (oldStatus != sensorData.Status)
                {
                    var oldStatusText = GetStatusDescription(oldStatus);
                    var newStatusText = GetStatusDescription(sensorData.Status);
                    System.Diagnostics.Debug.WriteLine($"🔄 Sensor {sensor.Tag} status changed: {oldStatusText} → {newStatusText}");

                    if (sensorData.Status == SensorStatus.DetectorDisabled)
                    {
                        System.Diagnostics.Debug.WriteLine($"⏸ Sensor {sensor.Tag} is now DISABLED");
                    }
                    else if (oldStatus == SensorStatus.DetectorDisabled)
                    {
                        System.Diagnostics.Debug.WriteLine($"▶ Sensor {sensor.Tag} is now ENABLED");
                    }
                }

                // Update alarm states only if sensor is not disabled
                if (sensorData.Status != SensorStatus.DetectorDisabled)
                {
                    UpdateAlarmStates(sensor);
                }

                System.Diagnostics.Debug.WriteLine($"📊 Updated sensor {sensor.Tag} = {sensorData.ProcessValue} {sensor.CurrentValue.Unit} (Status: {GetStatusDescription(sensorData.Status)})");
            }

            return sensor;
        }

        private string GetStatusDescription(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.Normal => "Normal",
                SensorStatus.AlarmLevel1 => "Alarm L1",
                SensorStatus.AlarmLevel2 => "Alarm L2",
                SensorStatus.DetectorError => "Error",
                SensorStatus.DetectorDisabled => "DISABLED",
                SensorStatus.LineOpenFault => "Line Open",
                SensorStatus.LineShortFault => "Line Short",
                _ => "Unknown"
            };
        }

        private int GetSortedInsertIndex(ObservableCollection<Sensor> sensors, string tagName)
        {
            for (int i = 0; i < sensors.Count; i++)
            {
                if (string.Compare(sensors[i].Tag, tagName, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    return i;
                }
            }
            return sensors.Count; // Add at end if no larger element found
        }

        private void UpdateSiteStatus(Site site)
        {
            if (!site.Sensors.Any())
            {
                site.Status.HasAlarm = false;
                site.Status.HasFault = false;
                site.Status.LastUpdate = DateTime.Now;
                return;
            }

            // Only consider active (non-disabled) sensors for site status
            var activeSensors = site.Sensors.Where(s => s.CurrentValue.Status != SensorStatus.DetectorDisabled);

            // Check for faults first
            site.Status.HasFault = activeSensors.Any(s =>
                s.CurrentValue.Status == SensorStatus.DetectorError ||
                s.CurrentValue.Status == SensorStatus.LineOpenFault ||
                s.CurrentValue.Status == SensorStatus.LineShortFault);

            // Check for alarms
            site.Status.HasAlarm = activeSensors.Any(s =>
                s.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                s.CurrentValue.Status == SensorStatus.AlarmLevel2);

            site.Status.LastUpdate = DateTime.Now;

            var disabledCount = site.Sensors.Count(s => s.CurrentValue.Status == SensorStatus.DetectorDisabled);
            if (disabledCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"📊 Site {site.DisplayName}: {disabledCount} disabled sensors out of {site.Sensors.Count} total");
            }
        }

        private void UpdateAlarmStates(Sensor sensor)
        {
            var value = sensor.CurrentValue.ProcessValue;
            sensor.Alarms.IsAlarmLevel2Active = value >= sensor.Alarms.AlarmLevel2;
            sensor.Alarms.IsAlarmLevel1Active = value >= sensor.Alarms.AlarmLevel1 && !sensor.Alarms.IsAlarmLevel2Active;
        }

        private SensorType MapDetectorTypeToSensorType(int detectorType)
        {
            return detectorType switch
            {
                1 or 2 => SensorType.GasDetector,      // Gas detectors
                3 => SensorType.TemperatureSensor,     // Flame detector
                4 or 5 => SensorType.PressureSensor,   // Manual call point and smoke
                _ => SensorType.GasDetector
            };
        }

        private float GetDefaultAlarmLevel1(int detectorType)
        {
            return detectorType switch
            {
                1 => 25f,    // 25% LEL
                2 => 500f,   // 500 PPM
                _ => 15f     // 15mA for others
            };
        }

        private float GetDefaultAlarmLevel2(int detectorType)
        {
            return detectorType switch
            {
                1 => 50f,    // 50% LEL
                2 => 1000f,  // 1000 PPM
                _ => 18f     // 18mA for others
            };
        }

        private string GetDetectorUnits(int detectorType)
        {
            return detectorType switch
            {
                1 => "%LEL",   // Gas detector with %LEL
                2 => "PPM",    // Gas detector with PPM
                3 => "mA",     // Flame detector
                4 => "mA",     // Manual call point
                5 => "mA",     // Smoke detector
                _ => "?"       // Unknown
            };
        }

        private string GetDetectorTypeName(int detectorType)
        {
            return detectorType switch
            {
                1 => "Gas Detector (%LEL)",
                2 => "Gas Detector (PPM)",
                3 => "Flame Detector",
                4 => "Manual Call Point",
                5 => "Smoke Detector",
                _ => "Unknown Detector"
            };
        }
    }

    // Event argument classes for autodiscovery
    public class SensorDiscoveredEventArgs : EventArgs
    {
        public Sensor Sensor { get; }
        public Site Site { get; }

        public SensorDiscoveredEventArgs(Sensor sensor, Site site)
        {
            Sensor = sensor;
            Site = site;
        }
    }

    public class SiteUpdatedEventArgs : EventArgs
    {
        public Site Site { get; }

        public SiteUpdatedEventArgs(Site site)
        {
            Site = site;
        }
    }
}