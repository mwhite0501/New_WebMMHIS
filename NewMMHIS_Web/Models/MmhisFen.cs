using System;
using System.Collections.Generic;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class MmhisFen
    {
        public long Lu { get; set; }
        public long Lt { get; set; }
        public string FieldCategory { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }
}
