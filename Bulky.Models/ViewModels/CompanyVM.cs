using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.ViewModels
{
    public class CompanyVM
    {
        public IEnumerable<SelectListItem> CompanyList { get; set; }
        public Company Company { get; set; }
    }
}
