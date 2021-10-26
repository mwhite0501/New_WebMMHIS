using System;
using System.Collections.Generic;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class MmhisMapPoint
    {
        public long Lu { get; set; }
        public long PointOrder { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }
}
