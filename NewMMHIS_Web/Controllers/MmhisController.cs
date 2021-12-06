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
        public List<string> GetUniqueDirections()//the direction of traffic for a given route (ex A, B, N, S, E, W)
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
        public List<string> GetUniqueYears() //all the years we have data on
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var strList = roads.Select(r => r.TheYear).Distinct().OrderByDescending(x => x).ToList();
            return strList;
        }
        public List<long> removePoints(List<long> Points, List<long> setDiff) //takes the set difference between two lists and removes it from the list
        {                                                                     //this is done to make sure both lists match up
            foreach (long point in setDiff)
            {
                Points.Remove(point);
            }
            return Points;
        }

        public List<string> PutFrontAtIndex(List<string> list, int index) //splits a string at a given index and places in front of the first half of the string
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

        public List<string> GetRouteInformation(double latitude, double longitude) //takes lat/long, finds the closest roadway point to it
        {
            List<string> RouteInformation = new List<string>();                    //should be a model instead of a list
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

            if (getClosestPoint.Any() == false) //if we cant find a close point
            {
                while(getClosestPoint.Any() == false) //loop and increase the variance until we do
                {
                    variance += 0.0005;
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

        public DataModel getData(PageModel p)
        {
            //MmhisDamus contains the specific routes
            //MmhisDians contains the route data a given interval
            //MmhisFens contains the location of the pictures at that given interval

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

        public string ModifyURLs(string imgPath)
        {
            //check the Startup.cs file to see how this works
            //look at the app.UseFileServer function calls
            //for some reason, we can't access the UNC with a direct call. That is why this was done. 
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
            if (imgPath.Substring(1, 19).Equals("\\san2\\MMHIS Pave24\\", StringComparison.CurrentCultureIgnoreCase))
                imgPath = "/MyPath41/" + imgPath.Substring(19);
            return imgPath;
        }
        
    }
}
