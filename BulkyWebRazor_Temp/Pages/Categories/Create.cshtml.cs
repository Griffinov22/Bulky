using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    [BindProperties]
    public class CreateModel : PageModel
    {
        private readonly BulkyDbContext _context;
        public Category Category { get; set;}
        public CreateModel(BulkyDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
        }
        public async Task<IActionResult> OnPost() 
        {
            if (ModelState.IsValid) 
            {
                _context.Categories.Add(Category);
                await _context.SaveChangesAsync();
            }
            TempData["success"] = "Successfully created category.";
            return RedirectToPage("Index");


        }
    }
}
