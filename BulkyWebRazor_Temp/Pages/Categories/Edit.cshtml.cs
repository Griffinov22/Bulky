using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    public class EditModel : PageModel
    {
        public readonly BulkyDbContext _context;
        [BindProperty]
        public Category? Category { get; set; }
        public EditModel(BulkyDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int? id)
        {
            if (id == null && id != 0)
            {
                return NotFound();
            }

            Category = _context.Categories.Find(id);

            if (Category == null) 
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                _context.Update(Category!);
                await _context.SaveChangesAsync();
            }

            TempData["success"] = "Successfully updated category.";
            return RedirectToPage("Index");
        }


    }
}
