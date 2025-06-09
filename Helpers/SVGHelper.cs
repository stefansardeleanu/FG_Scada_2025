using SkiaSharp;
using System.Xml.Linq;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Helpers
{
    public class SVGHelper
    {
        public static async Task<Dictionary<string, (SKPath Path, SKPoint Center, string Name)>> ParseRomaniaMapAsync()
        {
            var countyPaths = new Dictionary<string, (SKPath Path, SKPoint Center, string Name)>();

            try
            {
                Console.WriteLine("Attempting to load romania_map.svg...");

                // Try different possible paths for the SVG file
                string[] possiblePaths = {
                    "romania_map.svg",
                    "Maps/romania_map.svg",
                    "Resources/Maps/romania_map.svg"
                };

                string? svgContent = null;
                string usedPath = "";

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        Console.WriteLine($"Trying path: {path}");
                        using var stream = await FileSystem.OpenAppPackageFileAsync(path);
                        using var reader = new StreamReader(stream);
                        svgContent = await reader.ReadToEndAsync();
                        usedPath = path;
                        Console.WriteLine($"Successfully loaded SVG from: {path}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load from {path}: {ex.Message}");
                    }
                }

                if (string.IsNullOrEmpty(svgContent))
                {
                    Console.WriteLine("ERROR: Could not load SVG file from any path!");
                    return countyPaths;
                }

                Console.WriteLine($"SVG content length: {svgContent.Length} characters");
                Console.WriteLine($"SVG preview: {svgContent.Substring(0, Math.Min(200, svgContent.Length))}...");

                // Parse SVG XML
                var svgDoc = XDocument.Parse(svgContent);
                XNamespace svgNs = "http://www.w3.org/2000/svg";

                // County ID to name mapping
                var countyIdToName = GetCountyIdToNameMapping();

                // Find all path elements in the SVG
                var pathElements = svgDoc.Descendants(svgNs + "path");
                Console.WriteLine($"Found {pathElements.Count()} path elements in SVG");

                foreach (var pathElement in pathElements)
                {
                    // Get the path ID
                    string? id = pathElement.Attribute("id")?.Value;
                    Console.WriteLine($"Processing path with ID: {id}");

                    if (!string.IsNullOrEmpty(id) && countyIdToName.ContainsKey(id))
                    {
                        // Get the path data
                        string? pathData = pathElement.Attribute("d")?.Value;

                        if (!string.IsNullOrEmpty(pathData))
                        {
                            try
                            {
                                // Parse the path data into an SKPath
                                SKPath path = SKPath.ParseSvgPathData(pathData);

                                // Calculate center point for the county
                                SKRect bounds = path.Bounds;
                                SKPoint center = new SKPoint(
                                    bounds.MidX,
                                    bounds.MidY
                                );

                                // Store the path with county name
                                countyPaths[id] = (path, center, countyIdToName[id]);
                                Console.WriteLine($"Successfully parsed county: {id} - {countyIdToName[id]}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing path for county {id}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No path data found for county {id}");
                        }
                    }
                    else if (!string.IsNullOrEmpty(id))
                    {
                        Console.WriteLine($"Unknown county ID: {id}");
                    }
                }

                Console.WriteLine($"Successfully parsed {countyPaths.Count} counties from SVG");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing SVG: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return countyPaths;
        }

        private static Dictionary<string, string> GetCountyIdToNameMapping()
        {
            return new Dictionary<string, string>
            {
                { "ROSM", "Satu Mare" },
                { "ROAR", "Arad" },
                { "ROBH", "Bihor" },
                { "ROTM", "Timis" },
                { "ROAB", "Alba" },
                { "ROAG", "Arges" },
                { "ROBC", "Bacau" },
                { "ROBN", "Bistrita-Nasaud" },
                { "ROBT", "Botosani" },
                { "ROBR", "Braila" },
                { "ROBV", "Brasov" },
                { "ROB", "Bucuresti" },
                { "ROBZ", "Buzau" },
                { "ROCL", "Calarasi" },
                { "ROCS", "Caras-Severin" },
                { "ROCJ", "Cluj" },
                { "ROCT", "Constanta" },
                { "ROCV", "Covasna" },
                { "RODB", "Dambovita" },
                { "RODJ", "Dolj" },
                { "ROGL", "Galati" },
                { "ROGR", "Giurgiu" },
                { "ROGJ", "Gorj" },
                { "ROHR", "Harghita" },
                { "ROHD", "Hunedoara" },
                { "ROIL", "Ialomita" },
                { "ROIS", "Iasi" },
                { "ROIF", "Ilfov" },
                { "ROMM", "Maramures" },
                { "ROMH", "Mehedinti" },
                { "ROMS", "Mures" },
                { "RONT", "Neamt" },
                { "ROOT", "Olt" },
                { "ROPH", "Prahova" },
                { "ROSJ", "Salaj" },
                { "ROSB", "Sibiu" },
                { "ROSV", "Suceava" },
                { "ROTR", "Teleorman" },
                { "ROTL", "Tulcea" },
                { "ROVL", "Valcea" },
                { "ROVS", "Vaslui" },
                { "ROVN", "Vrancea" }
            };
        }

        public static void DrawStatusIndicators(SKCanvas canvas, SKPoint center, bool hasAlarm, bool hasFault)
        {
            float indicatorY = center.Y + 15; // Below county name
            float indicatorRadius = 4;
            float spacing = 4;

            // Normal indicator (green if normal, gray otherwise)
            SKColor normalColor = (!hasAlarm && !hasFault) ? SKColors.Green : SKColors.Gray;
            using (var paint = new SKPaint { Color = normalColor, IsAntialias = true })
            {
                canvas.DrawCircle(center.X - indicatorRadius - spacing, indicatorY, indicatorRadius, paint);
            }

            // Alarm indicator (orange if alarm, gray otherwise)
            SKColor alarmColor = hasAlarm ? SKColors.Orange : SKColors.Gray;
            using (var paint = new SKPaint { Color = alarmColor, IsAntialias = true })
            {
                canvas.DrawCircle(center.X, indicatorY, indicatorRadius, paint);
            }

            // Fault indicator (red if fault, gray otherwise)
            SKColor faultColor = hasFault ? SKColors.Red : SKColors.Gray;
            using (var paint = new SKPaint { Color = faultColor, IsAntialias = true })
            {
                canvas.DrawCircle(center.X + indicatorRadius + spacing, indicatorY, indicatorRadius, paint);
            }
        }

        public static SKColor GetCountyFillColor(bool hasAlarm, bool hasFault, bool isSelected = false)
        {
            if (isSelected)
                return new SKColor(100, 149, 237, 180); // Light blue for selected county

            if (hasFault)
                return new SKColor(255, 0, 0, 100); // Red for fault
            else if (hasAlarm)
                return new SKColor(255, 165, 0, 100); // Orange for alarm
            else
                return new SKColor(111, 156, 118, 180); // Green for normal
        }
    }
}