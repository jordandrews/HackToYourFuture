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
using System.Net;
using Newtonsoft.Json.Linq;

namespace HackToYourFuture.Controllers

{
    public class HomeController : Controller
    {
        /*
         * Index method simply retrieves all Place items from 
         * the database and adds them to the ViewModel, to be
         * displayed in the view.
         * 
         * All of the comments and other data is generated using
         * JSON Requests to avoid page loading.
         */
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

                if ((string)TempData["ErrorMessage"] != null)
                {
                    ViewBag.Message = TempData["ErrorMessage"].ToString();
                }
                return View(viewModel);

            }
        }

        /*
         * The GetComments method is only called through the
         * javascript on the page, when a 'Place' panel is 
         * clicked on. It retrieves the relevant comments 
         * from the database and displays them underneath.
         */

        public JsonResult GetComments(int? placeId)
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var comments = (from x in database.Comments
                    where x.PlaceID == placeId
                    select x).ToList();

                //Because of the way that .NET EF creates objects, 
                //we've had to create JSON-Friendly classes without
                //the relationship across classes. The following loop
                //is instantiating one of these classes for every 
                //standard class.

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

        /*
         * On post, the following method inserts a new comment into the database.  
         */
        [HttpPost]
        [ActionName("NewComment")]
        public ActionResult NewComment(IndexViewModel viewModel)
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                if (ModelState.IsValid)
                {
                    viewModel.NewComment.DateTime = DateTime.Now;
                    database.Comments.Add(viewModel.NewComment);
                    database.SaveChanges();
                    return RedirectToAction("Index");
                }
                TempData["ErrorMessage"] = "Please make sure you enter a valid comment!";
                return RedirectToAction("Index");
            }
        }
        /*
         * This method is called when a user clicks 'Add'.
         * It first inserts the new Place and then inserts
         * the Comment.
         */
        [HttpPost]
        [ActionName("NewPlace")]
        public ActionResult NewPlaceAndComment(IndexViewModel viewModel)
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                if (ModelState.IsValid)
                {
                    database.Places.Add(viewModel.NewPlace);
                    database.SaveChanges();

                    int lastId = database.Places.Max(item => item.PlaceID);
                    viewModel.NewComment.DateTime = DateTime.Now;
                    viewModel.NewComment.PlaceID = lastId;
                    database.Comments.Add(viewModel.NewComment);

                    database.SaveChanges();
                    return RedirectToAction("Index");
                }
                TempData["ErrorMessage"] = "Please make sure you enter a valid comment!";
                return RedirectToAction("Index");
            }
        }

        /*
         * This is our method of attempting to solve the final calculation. 
         * The premise behind this was to first find the closest two points, 
         * then to find the next shortest route to any third point, from either
         * of those two initial points. From the third point, it takes the
         * immediate shortest every time to the final point. 
         */

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
                    for (int j=0; j<places.Length;j++)
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

        /*
         * This method compares the distance between every set of two points and
         * finds the two points that are furthest apart. It returns an array
         * these two at the start and end, and the points in the middle are 
         * added arbitrarily.
         */

        private Place[] FarthestApart()
        {
            using (HackToYourFutureEntities2 database = new HackToYourFutureEntities2())
            {
                var places = (from x in database.Places
                         select x).ToArray();

                Place[] startEnd = new Place[places.Length];

                //Below for adds the first and last Places.
                double highestDoubleYet = 0;
                for(int i=0;i<places.Length;i++)
                {
                    for (int j=0; j<places.Length-1;j++)
                    {
                        if (j != i)
                        {
                            double tempDouble = Distance(places[i], places[j]);
                            if (tempDouble > highestDoubleYet)
                            {
                                highestDoubleYet = tempDouble;
                                Debug.WriteLine(tempDouble + places[i].PlaceName + places[j].PlaceName);
                                startEnd[0] = places[i];
                                startEnd[places.Length-1] = places[j];
                            }
                        }
                    }
                }

                //The below foreach loop adds in the remaining Places between.

                int counter = 1;
                foreach (var place in places)
                {
                    if (place.PlaceID != startEnd[0].PlaceID && place.PlaceID != startEnd[startEnd.Length-1].PlaceID)
                    {
                        startEnd[counter] = place;
                        counter++;
                    }
                }
                return startEnd;
            }
        }

        /*
         * This method uses the Google Maps API to calculate the optimal route between two places,
         * given a set of waypoints. It uses the Google Maps JSON to arrange the places and return
         * the result via javascript.
         */

        public JsonResult CalculateGoogle()
        {
            StringBuilder url = new StringBuilder();
            Place[] places = FarthestApart(); // returns array with correct start/end points and arbitrary remaining
            url.Append("http://maps.googleapis.com/maps/api/directions/json?origin=");
            url.Append(places[0].Latitude + "," + places[0].Longitude);
            url.Append("&destination=" + places[places.Length - 1].Latitude + "," + places[places.Length - 1].Longitude);
            url.Append("&waypoints=optimize:true");
            
            for (int i=1;i<places.Length-1;i++)
            {
                url.Append("|" + places[i].Latitude + "," + places[i].Longitude);
            }
            
            WebClient web = new WebClient();

            var data = web.DownloadString(url.ToString());
            JObject jObj = JObject.Parse(data);
            JArray jArray = (JArray) jObj["routes"];
            StringBuilder thisssss = new StringBuilder();

            JObject jjObj = (JObject)jArray.Last;
            JProperty last = (JProperty)jjObj.Last;
            JArray lastArray = (JArray) last.First;
            int[] placesToFix = lastArray.Select(jv => (int)jv).ToArray();

            Place[] finalPlaces = new Place[places.Length];
            finalPlaces[0] = places[0];
            finalPlaces[finalPlaces.Length-1] = places[finalPlaces.Length -1];
            
            for (int i = 0; i < placesToFix.Length; i++)
            {
                finalPlaces[i+1] = places[placesToFix[i] + 1];
            }

            JsonPlace[] jsonPlaces = new JsonPlace[places.Length];

            for (int i=0;i<finalPlaces.Length;i++)
            {
                var jsonPlace = new JsonPlace
                {
                    PlaceName = finalPlaces[i].PlaceName,
                    Latitude = finalPlaces[i].Latitude,
                    Longitude = finalPlaces[i].Longitude
                };
                jsonPlaces[i] = jsonPlace;
            }

            JavaScriptSerializer newSerializer = new JavaScriptSerializer();
            String jsonList = newSerializer.Serialize(jsonPlaces);

            return Json(jsonList, JsonRequestBehavior.AllowGet);
        }



        /*
         * Adapted from http://www.geodatasource.com/developers/c-sharp 
         * Calculates the distance from any two places on Earth using lat/long.
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