using System;
using System.Collections.Generic;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class MmhisDuan
    {
        public long Lu { get; set; }
        public double Logmeter0 { get; set; }
        public double? Logmeter1 { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public long Ld { get; set; }
    }
}
