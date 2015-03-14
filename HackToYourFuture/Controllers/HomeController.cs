﻿using System;
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
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {
                viewModel.NewComment.DateTime = DateTime.Now;

                database.Comments.Add(viewModel.NewComment);
                database.Places.Add(viewModel.NewPlace);

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
                List<int> placeIds = new List<int>();

                foreach(var place in places)
                {
                    Distance(place, places[place])
                }


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