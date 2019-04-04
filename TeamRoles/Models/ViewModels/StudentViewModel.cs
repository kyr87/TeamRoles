using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TeamRoles.Models.ViewModels
{
    public class StudentViewModel
    {
        public ApplicationUser student { get; set; }
        public Double AverageGrade { get; set; }
        public int TotalAbsences { get; set; }
        public int Age { get; set; }
    }
}