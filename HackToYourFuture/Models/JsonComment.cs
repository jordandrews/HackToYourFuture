using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HackToYourFuture.Models
{
    public class JsonComment
    {
        public DateTime DateTime { get; set; }
        public int PlaceID { get; set; }
        public String Text { get; set; }
    }
}