using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewMMHIS_Web.Models
{
    public class PageModel
    {
        public IEnumerable<SelectListItem> Years { get; set; }
        public IEnumerable<SelectListItem> Counties { get; set; }
        public IEnumerable<SelectListItem> Routes { get; set; }
        public IEnumerable<SelectListItem> Sections { get; set; }
        public IEnumerable<SelectListItem> Districts { get; set; }
        public IEnumerable<SelectListItem> Directions { get; set; }
        public string Year { get; set; }
        public string County { get; set; }
        public string Route { get; set; }
        public string Section { get; set; }
        public string Direction { get; set; }
        public long Lu { get; set; }
        public long Ld { get; set; }
        public string ImgURL { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Logmeter { get; set; }
        public int NumRows { get; set; }
        public List<string> Images { get; set; }

    }
}
