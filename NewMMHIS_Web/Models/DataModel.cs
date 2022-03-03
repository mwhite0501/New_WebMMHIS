using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewMMHIS_Web.Models
{
    public class DataModel
    {
        public IQueryable<MmhisDamu> Roadway { get; set; }
        public IQueryable<MmhisDian> Points { get; set; }
        public IQueryable<MmhisFen> PointData { get; set; }
        public IQueryable<string> Notes { get; set; }
    }
}
