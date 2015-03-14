using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackToYourFuture.Models;

namespace HackToYourFuture.ViewModels
{
    public class IndexViewModel
    {
        public List<Comment> Comments { get; set; }
        public List<Place> Places {get; set;}
        public Comment NewComment { get; set; }
        public Place NewPlace { get; set; }
    }
}
