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
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
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

        public JsonResult GetComments()
        {
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {
                var comments = (from x in database.Comments
                    where x.PlaceID == 1
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
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {
                viewModel.NewComment.DateTime = DateTime.Now;

                database.Comments.Add(viewModel.NewComment);
                database.SaveChanges();
                return RedirectToAction("Index");
            }
        }

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

    }
}