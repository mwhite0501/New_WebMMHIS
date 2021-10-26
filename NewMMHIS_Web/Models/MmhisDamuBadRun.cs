using System;
using System.Collections.Generic;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class MmhisDamuBadRun
    {
        public string District { get; set; }
        public string District1 { get; set; }
        public string County { get; set; }
        public string County1 { get; set; }
        public string Route { get; set; }
        public string Section { get; set; }
        public string MmhisDirection { get; set; }
        public string ArnoldDirection { get; set; }
        public string TheYear { get; set; }
        public string TheSystem { get; set; }
        public int? FrameLoadingOrder { get; set; }
        public string Note { get; set; }
        public long Ld { get; set; }
        public string TimeStamp { get; set; }
        public string ComputerHostName { get; set; }
        public string UserId { get; set; }
    }
}
