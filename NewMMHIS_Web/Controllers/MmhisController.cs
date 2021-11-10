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
        DataModel dataModel = new DataModel();

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
        public List<long> removePoints(List<long> Points, List<long> setDiff)
        {
            foreach (long point in setDiff)
            {
                Points.Remove(point);
            }
            return Points;
        }

        public List<string> SplitReverse(List<string> list, int index)
        {
            List<string> first = new List<string>();
            List<string> second = new List<string>();
            for (int i = 0; i < index; i++)
            {
                first.Add(list[i]);
            }
            for (int i = index; i < list.Count; i++)
            {
                second.Add(list[i]);
            }
            return second.Concat(first).ToList();
        }

        //public static double GetDistance(double lat1, double lon1, double lat2, double lon2)
        //{
        //    var R = 6371; // Radius of the earth in km
        //    var dLat = Deg2Rad(lat2 - lat1);  // deg2rad below
        //    var dLon = Deg2Rad(lon2 - lon1);
        //    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
        //        Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
        //        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        //    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        //    var d = R * c; // Distance in km
        //    return d * 1000;  //distance in m
        //}

        //public static double Deg2Rad(double deg)
        //{
        //    return deg * (Math.PI / 180);
        //}

        public List<string> GetRouteInformation(double latitude, double longitude)
        {
            List<string> RouteInformation = new List<string>();
            double variance = 0.0001; //.001 is 111m                               

            var getClosestPoint = from ii in _context.MmhisDamus
                                  join t in _context.MmhisDians
                                  on ii.Ld equals t.Lu
                                  where t.Latitude >= (double)latitude - variance && t.Latitude <= (double)latitude + variance
                                  && t.Longitude >= (double)longitude - variance && t.Longitude <= (double)longitude + variance
                                  orderby ii.TheYear descending
                                  select t.Lu;

            var getClosestPointLd = from ii in _context.MmhisDamus
                                  join t in _context.MmhisDians
                                  on ii.Ld equals t.Lu
                                  where t.Latitude >= (double)latitude - variance && t.Latitude <= (double)latitude + variance
                                  && t.Longitude >= (double)longitude - variance && t.Longitude <= (double)longitude + variance
                                  orderby ii.TheYear descending
                                  select t.Ld;

            if (getClosestPoint.Any() == false)
            {
                while(getClosestPoint.Any() == false)
                {
                    variance += .005;
                    getClosestPoint = from ii in _context.MmhisDamus
                                      join t in _context.MmhisDians
                                      on ii.Ld equals t.Lu
                                      where t.Latitude >= (double)latitude - variance && t.Latitude <= (double)latitude + variance
                                      && t.Longitude >= (double)longitude - variance && t.Longitude <= (double)longitude + variance
                                      orderby ii.TheYear descending
                                      select t.Lu;

                    getClosestPointLd = from ii in _context.MmhisDamus
                                        join t in _context.MmhisDians
                                        on ii.Ld equals t.Lu
                                        where t.Latitude >= (double)latitude - variance && t.Latitude <= (double)latitude + variance
                                        && t.Longitude >= (double)longitude - variance && t.Longitude <= (double)longitude + variance
                                        orderby ii.TheYear descending
                                        select t.Ld;
                }
            }
            

            var getInfo = from i in _context.MmhisDamus               //find out what section/direction/route it is
                          where i.Ld == getClosestPoint.FirstOrDefault()
                          select i;
            var routeInfo = from i in getInfo
                            select i.Route;
            var sectInfo = from i in getInfo
                           select i.Section;
            var dirInfo = from i in getInfo
                          select i.MmhisDirection;
            var yearInfo = from i in getInfo
                           select i.TheYear;
            var latInfo = from i in _context.MmhisDians
                         where i.Ld == getClosestPointLd.FirstOrDefault()
                         select i.Latitude;
            var longInfo = from i in _context.MmhisDians
                          where i.Ld == getClosestPointLd.FirstOrDefault()
                          select i.Longitude;
            var logInfo = from i in _context.MmhisDians
                          where i.Ld == getClosestPointLd.FirstOrDefault()
                          select i.Logmeter0;// * 0.000621371;
            var latInfoP = latInfo.FirstOrDefault().ToString();
            var longInfoP = longInfo.FirstOrDefault().ToString();
            var logInfoP = logInfo.FirstOrDefault().ToString();
            var dainsLd = getClosestPointLd.FirstOrDefault().ToString();
            var dainsLu = getClosestPoint.FirstOrDefault().ToString();


            RouteInformation.Add(routeInfo.FirstOrDefault()); //should probably just be returning the page model
            RouteInformation.Add(sectInfo.FirstOrDefault());
            RouteInformation.Add(dirInfo.FirstOrDefault());
            RouteInformation.Add(yearInfo.FirstOrDefault());
            RouteInformation.Add(logInfoP);
            RouteInformation.Add(latInfoP);
            RouteInformation.Add(longInfoP);
            RouteInformation.Add(dainsLd);
            RouteInformation.Add(dainsLu);
            return RouteInformation;
        }
        //public List<string>MapImageLoader(double latitude, double longitude)
        //{
        //    string imgPath = "";

        //    List<string> Images = new List<string>();
        //    List<long> Lus = new List<long>();

        //    double variance = 0.001;
        //    var Lda = (from t in _context.MmhisDians
        //                where t.Latitude >= (double)latitude - variance && t.Latitude <= (double)latitude + variance
        //                && t.Longitude >= (double)longitude - variance && t.Longitude <= (double)longitude + variance
        //                select t).FirstOrDefault();

        //    var Ld = Lda.Lu;

        //    var getInfo = from i in _context.MmhisDamus
        //                  where i.Ld == Ld
        //                  select i;
        //    var routeInfo = from i in getInfo
        //                    select i.Route;
        //    var sectInfo = from i in getInfo
        //                    select i.Section;
        //    var dirInfo = from i in getInfo
        //                    select i.ArnoldDirection;

        //    var getLatest = from i in _context.MmhisDamus
        //                    where i.Route == routeInfo.FirstOrDefault()
        //                    && i.Section == sectInfo.FirstOrDefault()
        //                    && i.ArnoldDirection == dirInfo.FirstOrDefault()
        //                    orderby i.TheYear descending
        //                    select i;
        //    var latest = getLatest.FirstOrDefault(); 

        //    Ld = latest.Ld;
        //    long LD = Ld;

        //    Lda = (from t in _context.MmhisDians
        //     where t.Latitude >= (double)latitude - variance 
        //     && t.Latitude <= (double)latitude + variance
        //     && t.Longitude >= (double)longitude - variance 
        //     && t.Longitude <= (double)longitude + variance
        //     && t.Lu == Ld
        //     select t).FirstOrDefault();

        //    var strLat = Lda.Latitude;
        //    var strLong = Lda.Longitude;
            
        //    var closestPoint = from r in _context.MmhisDians //only 1 number
        //                     where r.Lu == LD //lastest route
        //                     && r.Latitude == strLat
        //                     && r.Longitude == strLong
        //                     select r;
        //    var drillDownLd = closestPoint.FirstOrDefault();
        //    long ld = drillDownLd.Ld;
            

        //    var allPoints = from r in _context.MmhisDians  //something going wrong with one of these. TheYear is changing
        //                       where r.Lu == LD
        //                       select r;

        //    var allPointsAfter = from r in allPoints
        //                         where r.Lu == LD
        //                         && r.Ld >= ld                                   
        //                           select r.Ld;

        //    var allPointsBefore = from r in allPoints
        //                          where r.Lu == LD
        //                          && r.Ld < ld
        //                          select r.Ld;

        //    Lus = allPointsAfter.ToList();
        //    Lus.AddRange(allPointsBefore.ToList());
        //    //Lus = allPoints.Select(p => p.Ld).ToList();
        //    foreach(var l in Lus)
        //    {
        //        var p = from r in _context.MmhisFens                                //query fens data for specific point
        //                where r.Lu == l && r.FieldName == "f"                       //find the URL for the forward facing camera('f)
        //                && r.Lt == LD
        //                select r;
        //        if (Lus.Any())                                                      //if there is any data
        //        {
        //            if (p.Select(r => r.FieldValue).FirstOrDefault() != null)            //make sure specific point it's not null
        //            {
        //                imgPath = p.Select(r => r.FieldValue).FirstOrDefault();         //get the URL for the point
        //            }
        //            //store them in a list
        //            if (imgPath != null)                                                 //probably unnecessary check
        //            {
        //                imgPath = ModifyURLs(imgPath);
        //                Images.Add(imgPath);
        //            }
        //        }
        //    }
        //    return Images;
        //}

        public DataModel getData(PageModel p)
        {

            var roadId = from r in _context.MmhisDamus                                   //get the specific route, section, direction, year combination
                     where r.Route == p.Route
                     && r.MmhisDirection == p.Direction
                     && r.Section == p.Section
                     && r.TheYear == p.Year
                     select r;

            var roadIdLt = roadId.FirstOrDefault();
            
            var points = from r in _context.MmhisDians                                  //get the points on the route at ~5 meter increments
                     where r.Lu == roadIdLt.Ld
                     select r; //r.Ld
            var pointData = from r in _context.MmhisFens                                //store all data points for a given increment
                            join z in points
                            on r.Lu equals z.Ld
                            where r.Lt == roadIdLt.Ld
                            orderby r.Lu ascending
                            select r;

            dataModel.Roadway = roadId;
            dataModel.Points = points;
            dataModel.PointData = pointData;
            
            return dataModel;
        }

        //public List<string>ImageListLoader(string route, string direction, string section, string year, double logmeter, double latitude, double longitude)
        //{
        //    pageModel.Route = route;
        //    pageModel.Direction = direction;
        //    pageModel.Section = section;
        //    pageModel.Year = year;
        //    pageModel.Logmeter = logmeter;
        //    //pageModel.Longitude = longitude;
        //    //pageModel.Latitude = latitude;
        //    string imgPath = "";

        //    List<string> Images = new List<string>();
        //    List<long> Lus = new List<long>();

        //    var Ld = from r in _context.MmhisDamus             //get the Ld of the specific route, direction, section and year combination
        //             where r.Route == route
        //             && r.MmhisDirection == direction
        //             && r.Section == section
        //             && r.TheYear == year
        //             select r.Ld;

        //    if (logmeter > 0)
        //    {
        //        List<long> LU1 = new List<long>();
        //        List<long> LU2 = new List<long>();

        //        var Lu = from r in _context.MmhisDians            //get the points on the route at ~5 meter increments
        //                 where r.Lu == Ld.FirstOrDefault()
        //                 select r; //r.Ld
        //        var Logmeters = from r in _context.MmhisDians            //get all the logmeteres
        //                        where r.Lu == Ld.FirstOrDefault()
        //                        select r.Logmeter0;
                
        //        List<float> lgm = Logmeters.ToList();
                

        //        var nearestNumber = lgm.OrderBy(x => Math.Abs(x - logmeter)).First();

        //        var Lu2 = from r in Lu
        //                  where r.Logmeter0 < nearestNumber
        //                  select r;

        //        var Lu1 = from r in Lu
        //             where r.Logmeter0 >= nearestNumber
        //             select r;

        //        LU1 = Lu1.Select(x => x.Ld).ToList();
        //        LU2 = Lu2.Select(x => x.Ld).ToList();

        //        var LU3 = LU1.Concat(LU2);
        //        pageModel.Ld = Ld.FirstOrDefault();
        //        pageModel.Lu = LU1.FirstOrDefault(); //dain's ld
        //        Lus = LU3.ToList();

        //        LU1.Clear();
        //        LU2.Clear();

        //    }
        //    else
        //    {
        //        var Lu = from r in _context.MmhisDians                                  //get the points on the route at ~5 meter increments
        //                 where r.Lu == Ld.FirstOrDefault()
        //                 select r.Ld; //r.Ld
        //        pageModel.Ld = Ld.FirstOrDefault();
        //        pageModel.Lu = Lu.FirstOrDefault(); //dain's ld
        //        Lus = Lu.ToList();
        //    }
        //    var pointData = from r in _context.MmhisFens                                //store all points for a given section
        //                    where r.Lt == pageModel.Ld                                  
        //                    select r;

        //    //pageModel.DamuResults=
        //    //pageModel.DianResults = Lus;
        //    //pageModel.FenResults = pointData;
        //    foreach(var l in Lus)                                                       //for each point in all the points
        //    {               
        //       var p = from r in pointData                                              //query fens data for specific point
        //                    where r.Lu == l && r.FieldName == "f"                       //find the URL for the forward facing camera('f)
        //                    select r;
        //        if (pointData.Any())                                                    //if there is any data
        //        {
        //            if(p.Select(r => r.FieldValue).FirstOrDefault() != null)            //make sure specific point it's not null
        //            {
        //                imgPath = p.Select(r => r.FieldValue).FirstOrDefault();         //get the URL for the point
        //            }
        //                 //store them in a list
        //            if(imgPath != null)                                                 //probably unnecessary check
        //            {
        //                imgPath = ModifyURLs(imgPath);
        //                Images.Add(imgPath);
        //            }   
        //        }
        //    }
        //    return Images;
        //}
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
