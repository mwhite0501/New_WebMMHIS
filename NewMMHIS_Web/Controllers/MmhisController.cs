using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewMMHIS_Web.Models;

namespace NewMMHIS_Web.Controllers
{
    [Route("api/[controller]/[action]")]
    public class MmhisController : Controller
    {
        private readonly mmhisContext _context;
        public MmhisController(mmhisContext context)
        {
            this._context = context;
        }
        
        PageModel pageModel = new PageModel();

        public List<string> GetUniqueCounties()
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var strList = roads.Select(r => r.County).Distinct().ToList();
            var sortList = strList.Select(int.Parse).OrderBy(x => x).ToList();
            var bktstring = sortList.ConvertAll<string>(x => x.ToString());
            return bktstring;
        }
        public List<string> GetUniqueRoutes()
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var strList = roads.Select(r => r.Route).Distinct().ToList();
            var sortList = strList.Select(int.Parse).OrderBy(x => x).ToList();
            var bktstring = sortList.ConvertAll<string>(x => x.ToString());
            return bktstring;
        }
        public List<string> GetUniqueDirections()//need to pass in a route selection to find directions
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var directionList = roads.Select(r => r.MmhisDirection).Distinct().ToList();
            return directionList;
        }
        public List<string> GetUniqueSections()
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var sectionList = roads.Select(r => r.Section).Distinct().OrderBy(x => x).ToList();
            return sectionList;

        }
        public List<string> GetUniqueYears()
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var strList = roads.Select(r => r.TheYear).Distinct().OrderByDescending(x => x).ToList();
            return strList;
        }
        public List<string>ImageListLoader(string route, string direction, string section, string year, double logmeter)
        {
            pageModel.Route = route;
            pageModel.Direction = direction;
            pageModel.Section = section;
            pageModel.Year = year;
            pageModel.Logmeter = logmeter;

            //if logmile > 0 ... search for specific logmile
            List<string> Images = new List<string>();
            List<long> Lus = new List<long>();

            var Ld = from r in _context.MmhisDamus             //get the Ld of the specific route, direction, section and year combination
                     where r.Route == route
                     && r.MmhisDirection == direction
                     && r.Section == section
                     && r.TheYear == year
                     select r.Ld;
            
            if (logmeter > 0)
            {
                List<long> LU1 = new List<long>();
                List<long> LU2 = new List<long>();

                var Lu = from r in _context.MmhisDians            //get the points on the route at ~5 meter increments
                         where r.Lu == Ld.FirstOrDefault()
                         select r; //r.Ld
                var Logmeters = from r in _context.MmhisDians            //get all the logmeteres
                                where r.Lu == Ld.FirstOrDefault()
                                select r.Logmeter0;

                List<float> lgm = Logmeters.ToList();

                var nearestNumber = lgm.OrderBy(x => Math.Abs(x - logmeter)).First();

                var Lu2 = from r in Lu
                          where r.Logmeter0 < nearestNumber
                          select r;

               var Lu1 = from r in Lu
                     where r.Logmeter0 >= nearestNumber
                     select r;
                

                LU1 = Lu1.Select(x => x.Ld).ToList();
                LU2 = Lu2.Select(x => x.Ld).ToList();
                var LU3 = LU1.Concat(LU2);
                pageModel.Ld = Ld.FirstOrDefault();
                pageModel.Lu = LU1.FirstOrDefault(); //dain's ld
                Lus = LU3.ToList();
                LU1.Clear();
                LU2.Clear();

            }
            else
            {
                var Lu = from r in _context.MmhisDians            //get the points on the route at ~5 meter increments
                         where r.Lu == Ld.FirstOrDefault()
                         select r.Ld; //r.Ld
                pageModel.Ld = Ld.FirstOrDefault();
                pageModel.Lu = Lu.FirstOrDefault(); //dain's ld
                Lus = Lu.ToList();
            }
            var pointData = from r in _context.MmhisFens
                            where r.Lt == pageModel.Ld
                            select r;
            foreach(var l in Lus) //for each point in all the points
            {               
               var p = from r in pointData                               //query a new table
                            where r.Lu == l && r.FieldName == "f"                       //find the URL for the forward facing camera('f)
                            select r;
                if (pointData.Any())
                {
                    string imgPath = p.Select(r => r.FieldValue).FirstOrDefault(); //store them in a list
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data1\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath1/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data2\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath2/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data3\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath3/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data4\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath4/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data5\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath5/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data6\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath6/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data7\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath7/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data8\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath8/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data9\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath9/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat10\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath10/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat11\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath11/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat12\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath12/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat13\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath13/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat14\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath14/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat15\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath15/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat16\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath16/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat17\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath17/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Dat18\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath18/" + imgPath.Substring(18);

                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave1\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath19/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave2\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath20/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave3\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath21/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave4\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath22/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave5\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath23/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave6\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath24/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave7\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath25/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave8\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath26/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Pave9\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath27/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave10\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath28/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave11\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath29/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave12\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath30/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave13\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath31/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave14\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath32/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave15\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath33/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave16\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath34/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave17\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath35/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave18\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath36/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave19\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath37/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave20\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath38/" + imgPath.Substring(18);
                    if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave21\\", StringComparison.CurrentCultureIgnoreCase))
                        imgPath = "/MyPath39/" + imgPath.Substring(18);

                    Images.Add(imgPath);
                }
            }


            return Images;
        }
        
    }
}
