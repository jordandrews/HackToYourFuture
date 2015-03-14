using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HackToYourFuture.Models;
using HackToYourFuture.ViewModels;
using System.Text;
using System.Web.Script.Serialization;

namespace HackToYourFuture.Controllers

{
    public class HomeController : Controller
    {
        public ActionResult Index(int placeId = 0)
        {
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {
                var comments = (from x in database.Comments
                               where x.PlaceID == placeId
                               select x).ToList();

                var places = (from x in database.Places
                             select x).ToList();
                IndexViewModel viewModel = new IndexViewModel()
                {
                    Comments = comments,
                    Places = places
                };
                return View(viewModel);
            }
        }

        public JsonResult GetComments(int placeId)
        {
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {
                var comments = (from x in database.Comments
                    where x.PlaceID == placeId
                    select x).ToList(); 
                

                JavaScriptSerializer newSerializer = new JavaScriptSerializer();
                String jsonList = newSerializer.Serialize(comments);

                return Json(jsonList, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult NewComment(IndexViewModel viewModel)
        {
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {


                return RedirectToAction("Index");
            }
        }

        public ActionResult NewPlaceAndComment(IndexViewModel viewModel)
        {
            using (HackToYourFutureEntities database = new HackToYourFutureEntities())
            {

            }
            return RedirectToAction("Index");
        }

    }
}