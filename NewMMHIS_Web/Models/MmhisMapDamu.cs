using System;
using System.Collections.Generic;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class MmhisMapDamu
    {
        public string TheYear { get; set; }
        public string Key { get; set; }
        public int Xu { get; set; }
        public string ObjectType { get; set; }
        public string Category { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public long Ld { get; set; }
    }
}
