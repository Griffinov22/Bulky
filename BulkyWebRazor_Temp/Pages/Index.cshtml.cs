using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages
{
    public class IndexModel : PageModel
    {
        public readonly BulkyDbContext _context;
        public List<Category>? Categories;

        public IndexModel(BulkyDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            Categories = _context.Categories.ToList();
        }
    }
}
