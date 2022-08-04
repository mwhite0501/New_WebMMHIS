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
            var diststring = bktstring.Distinct().ToList();
            return diststring;
        }
        public List<string> GetUniqueRoutes()
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var strList = roads.Select(r => r.Route).Distinct().ToList();
            var sortList = strList.Select(x => x).OrderBy(x => x).ToList();
            sortList = sortList.OrderBy(x => x.Length).ThenBy(o => o).ToList();
            var bktstring = sortList.ConvertAll<string>(x => x.ToString());
            
            return bktstring;
        }
        public List<string> GetUniqueSections()
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var sectionList = roads.Select(r => r.Section).Distinct().OrderBy(x => x.Length).ThenBy(o => o).ToList();
            return sectionList;

        }
        public List<string> GetUniqueDirections()//the direction of traffic for a given route (ex A, B, N, S, E, W)
        {
            var roads = from r in _context.MmhisDamus
                        select r;
            var directionList = roads.Select(r => r.MmhisDirection).Distinct().OrderBy(x => x.Length).ThenBy(o => o).ToList();
            return directionList;
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
            for (int i = 0; i < index; i++)             //need to fix this function so that I dont get an exception when the lists are different sizes
            {
                first.Add(list[i]);
            }
            for (int i = index-1; i < list.Count; i++)
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
                    variance += 0.0006;
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
            var getNotes = from i in _context.MmhisDamus
                          where i.Ld == getClosestPoint.FirstOrDefault()
                          select i.Note;
            var countyInfo = from i in getInfo
                             select i.County;

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
            RouteInformation.Add(getNotes.FirstOrDefault());
            RouteInformation.Add(countyInfo.FirstOrDefault());

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

            //if roadId is null, cause an exception, invalid route combination
            if (!roadId.Any())
            {
                //test
                return null;
            }

            var roadIdNote = from r in _context.MmhisDamus                                   //get notes on specific route, section, direction, year combo
                         where r.Route == p.Route
                         && r.MmhisDirection == p.Direction
                         && r.Section == p.Section
                         && r.TheYear == p.Year
                         select r.Note;

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
            dataModel.Notes = roadIdNote;
            
            return dataModel;
        }        
    }
}
