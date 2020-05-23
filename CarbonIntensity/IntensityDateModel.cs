using System;
using System.Collections.Generic;
using System.Text;

namespace CarbonIntensity
{
    public class Intensity
    {
        public int forecast { get; set; }
        public int? actual { get; set; }
        public string index { get; set; }
    }

    public class IntensityDate
    {
        public DateTime from { get; set; }
        public DateTime to { get; set; }
        public Intensity intensity { get; set; }
    }

    public class IntensityDateModel
    {
        public List<IntensityDate> data { get; set; }
    }
}
