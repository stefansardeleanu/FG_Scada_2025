using System.Collections.Concurrent;
using FG_Scada_2025.Models;
using FG_Scada_2025.Services;

namespace FG_Scada_2025.Services
{
    public class AutodiscoveryService
    {
        private readonly ConcurrentDictionary<int, Site> _discoveredSites = new();
        private readonly ConcurrentDictionary<string, Sensor> _discoveredSensors = new();
        private readonly RealTimeDataService _realTimeDataService;

        // Events for UI updates
        public event EventHandler<SiteDiscoveredEventArgs>? SiteDiscovered;
        public event EventHandler<SensorDiscoveredEventArgs>? SensorDiscovered;
        public event EventHandler<SiteUpdatedEventArgs>? SiteUpdated;

        public AutodiscoveryService(RealTimeDataService realTimeDataService)
        {
            _realTimeDataService = realTimeDataService;

            // Subscribe to MQTT sensor data updates
            _realTimeDataService.SensorDataUpdated += OnSensorDataReceived;
        }

        public Site? GetSite(int siteId)
        {
            return _discoveredSites.TryGetValue(siteId, out var site) ? site : null;
        }

        public Site? GetSiteByName(string siteName)
        {
            return _discoveredSites.Values.FirstOrDefault(s =>
                s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
        }

        public List<Site> GetAllSites()
        {
            return _discoveredSites.Values.ToList();
        }

        public List<Site> GetSitesForCounty(string countyId)
        {
            return _discoveredSites.Values
                .Where(s => s.CountyId.Equals(countyId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void OnSensorDataReceived(object? sender, SensorDataUpdatedEventArgs e)
        {
            var sensorData = e.SensorData;

            // Skip raw messages
            if (sensorData.SiteName == "Raw")
                return;

            // Discover or update site
            var site = DiscoverOrUpdateSite(sensorData);

            // Discover or update sensor
            var sensor = DiscoverOrUpdateSensor(sensorData, site);

            // Update site with sensor if it's new
            if (!site.Sensors.Any(s => s.Id == sensor.Id))
            {
                site.Sensors.Add(sensor);

                // Update site status based on all sensors
                UpdateSiteStatus(site);

                // Notify that site was updated
                SiteUpdated?.Invoke(this, new SiteUpdatedEventArgs(site));
            }
        }

        private Site DiscoverOrUpdateSite(SensorData sensorData)
        {
            if (!_discoveredSites.TryGetValue(sensorData.SiteId, out var site))
            {
                // Create new site
                site = new Site
                {
                    Id = sensorData.SiteId.ToString(),
                    Name = sensorData.SiteName,
                    DisplayName = FormatDisplayName(sensorData.SiteName),
                    CountyId = DetermineCountyFromSiteId(sensorData.SiteId), // You can customize this logic
                    Sensors = new List<Sensor>(),
                    PlcConnection = new PLCConnection
                    {
                        Protocol = "MQTT",
                        Topic = $"/PLCNEXT/{sensorData.SiteId}_{sensorData.SiteName}/",
                        IsConnected = true,
                        LastUpdate = DateTime.Now
                    },
                    Status = new Models.SiteStatus  // FIXED: Explicitly use Models.SiteStatus
                    {
                        LastUpdate = DateTime.Now
                    }
                };

                _discoveredSites[sensorData.SiteId] = site;

                System.Diagnostics.Debug.WriteLine($"🔍 Discovered new site: {site.Id} - {site.DisplayName}");

                // Notify that a new site was discovered
                SiteDiscovered?.Invoke(this, new SiteDiscoveredEventArgs(sensorData.SiteId, sensorData.SiteName));
            }
            else
            {
                // Update existing site connection status
                site.PlcConnection.LastUpdate = DateTime.Now;
                site.PlcConnection.IsConnected = true;
            }

            return site;
        }

        private Sensor DiscoverOrUpdateSensor(SensorData sensorData, Site site)
        {
            string sensorKey = $"{sensorData.SiteId}_{sensorData.ChannelId}";

            if (!_discoveredSensors.TryGetValue(sensorKey, out var sensor))
            {
                // Create new sensor
                sensor = new Sensor
                {
                    Id = sensorData.ChannelId,
                    Tag = sensorData.TagName,
                    Name = sensorData.TagName,
                    SiteId = sensorData.SiteId.ToString(),
                    Type = MapDetectorTypeToSensorType(sensorData.DetectorType),
                    CurrentValue = new SensorValue
                    {
                        ProcessValue = (float)sensorData.ProcessValue,
                        Unit = GetDetectorUnits(sensorData.DetectorType),
                        Status = sensorData.Status,
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

                System.Diagnostics.Debug.WriteLine($"🔍 Discovered new sensor: {sensor.Id} - {sensor.Tag} (Type: {GetDetectorTypeName(sensorData.DetectorType)})");

                // Notify that a new sensor was discovered
                SensorDiscovered?.Invoke(this, new SensorDiscoveredEventArgs(sensor, site));
            }
            else
            {
                // Update existing sensor with latest data
                sensor.CurrentValue.ProcessValue = (float)sensorData.ProcessValue;
                sensor.CurrentValue.Status = sensorData.Status;
                sensor.CurrentValue.Timestamp = sensorData.Timestamp;

                // Update alarm states
                UpdateAlarmStates(sensor);
            }

            return sensor;
        }

        private void UpdateSiteStatus(Site site)
        {
            if (!site.Sensors.Any())
            {
                site.Status.HasAlarm = false;
                site.Status.HasFault = false;
                return;
            }

            // Check for faults first
            site.Status.HasFault = site.Sensors.Any(s =>
                s.CurrentValue.Status == SensorStatus.DetectorError ||
                s.CurrentValue.Status == SensorStatus.LineOpenFault ||
                s.CurrentValue.Status == SensorStatus.LineShortFault);

            // Check for alarms
            site.Status.HasAlarm = site.Sensors.Any(s =>
                s.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                s.CurrentValue.Status == SensorStatus.AlarmLevel2);

            site.Status.LastUpdate = DateTime.Now;
        }

        private void UpdateAlarmStates(Sensor sensor)
        {
            var value = sensor.CurrentValue.ProcessValue;
            sensor.Alarms.IsAlarmLevel2Active = value >= sensor.Alarms.AlarmLevel2;
            sensor.Alarms.IsAlarmLevel1Active = value >= sensor.Alarms.AlarmLevel1 && !sensor.Alarms.IsAlarmLevel2Active;
        }

        private string FormatDisplayName(string siteName)
        {
            // Convert PanouHurezani to "Panou Hurezani"
            // Add spaces before capital letters for better display
            return System.Text.RegularExpressions.Regex.Replace(siteName, "([a-z])([A-Z])", "$1 $2");
        }

        private string DetermineCountyFromSiteId(int siteId)
        {
            // You can customize this logic based on your site ID ranges
            // For now, default all to ROGJ (Gorj county)
            return siteId switch
            {
                5 => "ROGJ", // PanouHurezani is in Gorj
                _ => "ROGJ"  // Default to Gorj for now
            };
        }

        private SensorType MapDetectorTypeToSensorType(int detectorType)
        {
            return detectorType switch
            {
                1 or 2 => SensorType.GasDetector,
                3 => SensorType.TemperatureSensor, // Flame detector
                4 or 5 => SensorType.PressureSensor, // Manual call point and smoke
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