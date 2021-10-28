using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewMMHIS_Web.Models;
using System.Data.Entity.Spatial;

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
        public static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(lat2 - lat1);  // deg2rad below
            var dLon = Deg2Rad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d * 1000;  //distance in m
        }

        public static double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }
        public List<string>MapImageLoader(double latitude, double longitude)
        {
            string imgPath = "";

            List<string> Images = new List<string>();
            List<long> Lus = new List<long>();

            //var closest = _context.MmhisDians.OrderBy(x =>
            //   (latitude - (decimal)x.Latitude) * (latitude - (decimal)x.Latitude) + (longitude - (decimal)x.Longitude) * (longitude - (decimal)x.Longitude))
            // .First(); //worked for a second
            //var nearestLatitude = lats.OrderBy(x => Math.Abs((decimal)x - latitude)).First();
            //var nearestLongitude = longs.OrderBy(x => Math.Abs((decimal)x - longitude)).First();
            //var closest = _context.MmhisDians.Aggregate((x, y) => Math.Abs((decimal)x.Latitude - latitude) < Math.Abs((decimal)y.Longitude - longitude) ? x : y);
            //DbGeography searchLocation = DbGeography.FromText(String.Format("POINT({0} {1})", longitude, latitude));

            //var closest =
            //    (from location in _context.MmhisDians
            //         //where  // (Additional filtering criteria here...)
            //     select new
            //     {
            //         Latitude = location.Latitude,
            //         Longitude = location.Longitude,
            //         Distance = searchLocation.Distance(
            //             DbGeography.FromText("POINT(" + location.Longitude + " " + location.Latitude + ")"))
            //     })
            //    .OrderBy(location => location.Distance)
            //    .ToList();
            //var locations = _context.MmhisDians.Where(m => GetDistance((double)latitude, (double)longitude, (double)m.Latitude, (double)m.Longitude) < 50);

            double variance = 0.001;
            var Lda = (from t in _context.MmhisDians
                        where t.Latitude >= (double)latitude - variance && t.Latitude <= (double)latitude + variance
                        && t.Longitude >= (double)longitude - variance && t.Longitude <= (double)longitude + variance
                        select t).FirstOrDefault();

            //need to check and make sure this is the latest year

            var Ld = Lda.Lu;

            var getInfo = from i in _context.MmhisDamus
                          where i.Ld == Ld
                          select i;
            var routeInfo = from i in getInfo
                            select i.Route;
            var sectInfo = from i in getInfo
                            select i.Section;
            var dirInfo = from i in getInfo
                            select i.ArnoldDirection;
            var yearInfo = from i in getInfo
                           select i.TheYear;
            var getLatest = from i in _context.MmhisDamus
                            where i.Route == routeInfo.FirstOrDefault()
                            && i.Section == sectInfo.FirstOrDefault()
                            && i.ArnoldDirection == dirInfo.FirstOrDefault()
                            orderby i.TheYear descending
                            select i;
            var latest = getLatest.FirstOrDefault(); 

            Ld = latest.Ld;

            Lda = (from t in _context.MmhisDians
             where t.Latitude >= (double)latitude - variance 
             && t.Latitude <= (double)latitude + variance
             && t.Longitude >= (double)longitude - variance 
             && t.Longitude <= (double)longitude + variance
             && t.Lu == Ld
             select t).FirstOrDefault();

            var strLat = Lda.Latitude;
            var strLong = Lda.Longitude;
            
            var closestPointLu = from r in _context.MmhisDians //only 1 number
                             where r.Lu == Ld
                             && r.Latitude == strLat
                             && r.Longitude == strLong
                             select r.Lu;

            var closestPointLd = from r in _context.MmhisDians //only 1 number
                                 where r.Lu == Ld
                                 && r.Latitude == strLat
                                 && r.Longitude == strLong
                                 select r.Ld;

            var allPointsLd = from r in _context.MmhisDians
                             where r.Lu == closestPointLu.FirstOrDefault()
                             select r.Ld;

            var allPoints = from r in _context.MmhisDians
                               where r.Lu == closestPointLu.FirstOrDefault()
                               select r.Ld;

            var allPointsAfter = from r in allPoints
                                   where r >= closestPointLd.FirstOrDefault()
                                   select r;

            var allPointsBefore = from r in allPoints
                                 where r < closestPointLd.FirstOrDefault()
                                 select r;

            Lus = allPointsAfter.ToList();
            Lus.AddRange(allPointsBefore.ToList());

            foreach(var l in Lus)
            {
                var p = from r in _context.MmhisFens                                //query fens data for specific point
                        where r.Lu == l && r.FieldName == "f"                       //find the URL for the forward facing camera('f)
                        select r;
                if (Lus.Any())                                                      //if there is any data
                {
                    if (p.Select(r => r.FieldValue).FirstOrDefault() != null)            //make sure specific point it's not null
                    {
                        imgPath = p.Select(r => r.FieldValue).FirstOrDefault();         //get the URL for the point
                    }
                    //store them in a list
                    if (imgPath != null)                                                 //probably unnecessary check
                    {
                        imgPath = ModifyURLs(imgPath);
                        Images.Add(imgPath);
                    }
                }
            }
            return Images;
        }
        public List<string>ImageListLoader(string route, string direction, string section, string year, double logmeter, double latitude, double longitude)
        {
            pageModel.Route = route;
            pageModel.Direction = direction;
            pageModel.Section = section;
            pageModel.Year = year;
            pageModel.Logmeter = logmeter;
            //pageModel.Longitude = longitude;
            //pageModel.Latitude = latitude;
            string imgPath = "";

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
            //else if(longitude != 0.0M || latitude != 0.0M)
            //{
            //    //var Latitudes = from r in _context.MmhisDians
            //    //                where r.Lu == Ld.FirstOrDefault()
            //    //                select r.Latitude;

            //    //var Longitudes = from r in _context.MmhisDians
            //    //                 where r.Lu == Ld.FirstOrDefault()
            //    //                 select r.Longitude;

            //    var LatLongs = from r in _context.MmhisDians
            //                   where r.Lu == Ld.FirstOrDefault()
            //                   select new { r.Latitude, r.Longitude };

            //    //List<float?> lats = Latitudes.ToList();
            //    //List<float?> longs = Longitudes.ToList();

            //    //var nearestLatitude = lats.OrderBy(x => Math.Abs((decimal)x - latitude)).First();
            //    //var nearestLongitude = longs.OrderBy(x => Math.Abs((decimal)x - longitude)).First();

            //    var closestLatLong = LatLongs.Select(x => new { x.Latitude,x.Longitude, delta = Math.Abs((decimal)x.Latitude - latitude) + Math.Abs((decimal)x.Longitude - longitude) })
            //        .OrderBy(x => x.delta)
            //        .FirstOrDefault();

            //    var firstPoint = from r in _context.MmhisDians
            //                     where r.Lu == Ld.FirstOrDefault()
            //                     select r.Ld;

            //    //get difference between current point and first point to determine size. Then compare with total count.
                
            //    var Lu = from r in _context.MmhisDians
            //             where r.Lu == Ld.FirstOrDefault()
            //             && r.Latitude == closestLatLong.Latitude
            //             && r.Longitude == closestLatLong.Longitude
            //             select r.Ld;

            //    var difference = Lu.FirstOrDefault() - firstPoint.FirstOrDefault();
            //    var count = firstPoint.Count() - difference;
            //    var fp = firstPoint.FirstOrDefault();

            //    pageModel.Ld = Ld.FirstOrDefault();
            //    pageModel.Lu = Lu.FirstOrDefault();

            //    List<long> diffs = new List<long>();
            //    for(int i = 0; i< difference; i++)
            //    {
            //        diffs.Add(fp);
            //        fp+=1;
            //    }

            //    Lus = Lu.ToList();

            //    for(int i = 0; i < count; i++)
            //    {
            //        pageModel.Lu += 1;
            //        Lus.Add(pageModel.Lu);
            //    }

            //    Lus.AddRange(diffs);
            //}
            else
            {
                var Lu = from r in _context.MmhisDians                                  //get the points on the route at ~5 meter increments
                         where r.Lu == Ld.FirstOrDefault()
                         select r.Ld; //r.Ld
                pageModel.Ld = Ld.FirstOrDefault();
                pageModel.Lu = Lu.FirstOrDefault(); //dain's ld
                Lus = Lu.ToList();
            }
            var pointData = from r in _context.MmhisFens                                //store all points for a given section
                            where r.Lt == pageModel.Ld                                  
                            select r;
            foreach(var l in Lus)                                                       //for each point in all the points
            {               
               var p = from r in pointData                                              //query fens data for specific point
                            where r.Lu == l && r.FieldName == "f"                       //find the URL for the forward facing camera('f)
                            select r;
                if (pointData.Any())                                                    //if there is any data
                {
                    if(p.Select(r => r.FieldValue).FirstOrDefault() != null)            //make sure specific point it's not null
                    {
                        imgPath = p.Select(r => r.FieldValue).FirstOrDefault();         //get the URL for the point
                    }
                         //store them in a list
                    if(imgPath != null)                                                 //probably unnecessary check
                    {
                        imgPath = ModifyURLs(imgPath);
                        Images.Add(imgPath);
                    }   
                }
            }
            return Images;
        }
        public string ModifyURLs(string imgPath)
        {
            if (imgPath.Substring(1, 18).Equals("\\san2\\MMHIS Data1\\", StringComparison.CurrentCultureIgnoreCase)) //select the correct path on network drive (pretty ugly)
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
                imgPath = "/MyPath28/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave11\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath29/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave12\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath30/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave13\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath31/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave14\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath32/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave15\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath33/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave16\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath34/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave17\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath35/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave18\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath36/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave19\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath37/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave20\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath38/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave21\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath39/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave22\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath40/" + imgPath.Substring(19);
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave23\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath41/" + imgPath.Substring(19);
            return imgPath;
        }
        
    }
}
