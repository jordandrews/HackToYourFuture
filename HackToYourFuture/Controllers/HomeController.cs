using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HackToYourFuture.Models;
using HackToYourFuture.ViewModels;
using System.Text;
using System.Web.Script.Serialization;
using System.Data.Entity;
using System.Diagnostics;
using Newtonsoft.Json;

namespace HackToYourFuture.Controllers

{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var places = (from x in database.Places
                             select x).ToList();
                IndexViewModel viewModel = new IndexViewModel()
                {
                    Places = places
                };
                return View(viewModel);
            }
        }

        public JsonResult GetComments(int placeId)
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var comments = (from x in database.Comments
                    where x.PlaceID == placeId
                    select x).ToList();
                List<JsonComment> list = new List<JsonComment>();
                foreach (var comment in comments)
                {
                    JsonComment newComm = new JsonComment{
                        PlaceID = (int) comment.PlaceID,
                        DateTime = (DateTime) comment.DateTime,
                        Text = comment.Text
                    };
                    list.Add(newComm);
                }
                
                JavaScriptSerializer newSerializer = new JavaScriptSerializer();
                String jsonList = newSerializer.Serialize(list);

                return Json(jsonList, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult NewComment(IndexViewModel viewModel)
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                viewModel.NewComment.DateTime = DateTime.Now;

                database.Comments.Add(viewModel.NewComment);
                database.SaveChanges();
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ActionName("NewPlace")]
        public ActionResult NewPlaceAndComment(IndexViewModel viewModel)
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                viewModel.NewComment.DateTime = DateTime.Now;


                Place thisPlace = new Place
                {
                    Latitude = viewModel.NewPlace.Latitude,
                    Longitude = viewModel.NewPlace.Longitude,
                    PlaceName = viewModel.NewPlace.PlaceName
                };

                database.Places.Add(thisPlace);
                database.SaveChanges();

               

                int lastId = database.Places.Max(item => item.PlaceID);
                viewModel.NewComment.PlaceID = lastId;
                database.Comments.Add(viewModel.NewComment);
               

                database.SaveChanges();
                return RedirectToAction("Index");
            }
        }

        public ActionResult Calculate()
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var places = (from x in database.Places
                             select x).ToArray();
                int[] finishedPlaceIds = new int[places.Length];

                Place firstPlace1 = null;
                Place firstPlace2 = null;
                int firstPlace1Index = 0;
                int firstPlace2Index = 0;

                /*
                 * Firstly, find the two points with the shortest distance between them.
                 */

                for(int i=0;i<places.Length-1;i++)
                {
                    double lowestDoubleYet = 100000;
                    for (int j=0; j<places.Length-1;j++)
                    {
                        if (j != i)
                        {
                            double tempDouble = Distance(places[i], places[j]);

                            if (tempDouble < lowestDoubleYet)
                            {
                                lowestDoubleYet = tempDouble;
                                firstPlace1 = places[i];
                                firstPlace1Index = i;
                                firstPlace2 = places[j];
                                firstPlace2Index =j;
                            }
                        }
                    }
                }

                Place firstPlace3 = null;

                /*
                 * Then, find the shortest distance to another point
                 * from either the first or second point. 
                 */

                for (int i = 0; i < places.Length; i++ )
                {
                    double lowestDoubleYet = 100000;
                    double tempDouble;
                    if (i != firstPlace1Index)
                    {
                        tempDouble = Distance(firstPlace1, places[i]);
                        if (tempDouble < lowestDoubleYet)
                        {
                            lowestDoubleYet = tempDouble;
                            firstPlace3 = firstPlace2;
                            firstPlace2 = firstPlace1;
                            firstPlace1 = firstPlace3;
                            firstPlace3 = places[i];
                        }

                    }
                    if (i != firstPlace2Index)
                    {
                        tempDouble = Distance(firstPlace2, places[i]);
                        if (tempDouble < lowestDoubleYet)
                        {
                            lowestDoubleYet = tempDouble;
                            firstPlace3 = places[i];
                        }
                    }
                }

                finishedPlaceIds[0] = firstPlace1.PlaceID;
                finishedPlaceIds[1] = firstPlace2.PlaceID;



                return View();
            }
            
        }

        /*
         * Adapted from http://www.geodatasource.com/developers/c-sharp 
         */
        private double Distance(Place place1, Place place2)
        {
            double lat1 = (double) place1.Latitude;
            double lon1 = (double) place1.Longitude;
            double lat2 = (double) place2.Latitude;
            double lon2 = (double) place2.Longitude;
        
            double theta = lon1 - lon2;
            double dist = Math.Sin(DegreesToRadians(lat1)) * Math.Sin(DegreesToRadians(lat2)) + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) * Math.Cos(DegreesToRadians(theta));
            dist = Math.Acos(dist);
            dist = RadiansToDegrees(dist);
            dist = dist * 1.609344 * 60 * 1.1515;
            return dist;
        }

        private double DegreesToRadians(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        private double RadiansToDegrees(double rad)
        {
            return (rad / Math.PI * 180.0);
        }


    }
}