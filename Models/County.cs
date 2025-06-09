using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG_Scada_2025.Models
{
    public class County
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SvgPath { get; set; } = string.Empty;
        public string MapFilePath { get; set; } = string.Empty;
        public List<Site> Sites { get; set; } = new List<Site>();
        public CountyPosition Position { get; set; } = new CountyPosition();
    }

    public class CountyPosition
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
}