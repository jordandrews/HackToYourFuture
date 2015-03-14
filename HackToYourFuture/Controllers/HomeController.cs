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

        public JsonResult GetComments(int? placeId)
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

        public JsonResult Calculate()
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var places = (from x in database.Places
                             select x).ToArray();
                Place[] finishedPlaces = new Place[places.Length];

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

                /*
                 * Finally, from the third point, select each of the shortest points.
                 */

                finishedPlaces[0] = firstPlace1;
                finishedPlaces[1] = firstPlace2;
                finishedPlaces[2] = firstPlace3;
                Place[] revisedPlaces = places.Where(val => val.PlaceID != firstPlace1.PlaceID && val.PlaceID != firstPlace2.PlaceID && val.PlaceID != firstPlace3.PlaceID).ToArray();
                for (int i=3; i<places.Length;i++)
                {
                    Place placeToAdd = null;
                    double lowestDoubleYet = 100000;
                    foreach (var item in revisedPlaces)
                    {
                        double tempDouble = Distance(finishedPlaces[i - 1], item);
                        if (tempDouble < lowestDoubleYet)
                        {
                            lowestDoubleYet = tempDouble;
                            placeToAdd = item;
                        }
                    }
                    revisedPlaces = revisedPlaces.Where(val => val.PlaceID != placeToAdd.PlaceID).ToArray();
                    finishedPlaces[i] = placeToAdd;
                }

                /*
                 * Finally, create Json Objects and then return them.   
                 */

                List<JsonPlace> myJson = new List<JsonPlace>();
                foreach (var item in finishedPlaces)
                {
                    JsonPlace place = new JsonPlace
                    {
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        PlaceName = item.PlaceName
                    };

                    myJson.Add(place);

                }

                JavaScriptSerializer newSerializer = new JavaScriptSerializer();
                String jsonList = newSerializer.Serialize(myJson);

                return Json(jsonList, JsonRequestBehavior.AllowGet);
            }
            
        }

        public Place[] FarthestApart()
        {
            
            Place[] startEnd = new Place[2];

            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var places = (from x in database.Places
                         select x).ToArray();

                for(int i=0;i<places.Length-1;i++)
                {
                    double highestDoubleYet = 0;
                    for (int j=0; j<places.Length-1;j++)
                    {
                        if (j != i)
                        {
                            double tempDouble = Distance(places[i], places[j]);
                            if (tempDouble > highestDoubleYet)
                            {
                                highestDoubleYet = tempDouble;
                                startEnd[0] = places[i];
                                startEnd[1] = places[j];
                            }
                        }
                    }
                }
                return startEnd;
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