using System;
using System.Collections.Generic;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class MmhisDian
    {
        public long Lu { get; set; }
        public float Logmeter0 { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public long Ld { get; set; }

        //public static explicit operator decimal(MmhisDian v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
