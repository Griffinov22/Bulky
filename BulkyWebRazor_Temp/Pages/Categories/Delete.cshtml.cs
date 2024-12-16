using BulkyWebRazor_Temp.Data;
using BulkyWebRazor_Temp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BulkyWebRazor_Temp.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        public readonly BulkyDbContext _context;
        [BindProperty]
        public Category? Category { get; set; }
        public DeleteModel(BulkyDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int? id)
        {
            if (id == null) { return NotFound(); }
            Category = _context.Categories.Find(id);
            if (Category == null) { return NotFound(); }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (Category == null || Category.Id == 0) return NotFound();
            Category? obj = await _context.Categories.FindAsync(Category.Id);
            if (obj == null) return NotFound();

            _context.Categories.Remove(obj); // Only need Id which is binded on Post with the data annotation!
            await _context.SaveChangesAsync();

            TempData["success"] = "Successfully deleted category.";
            return RedirectToPage("Index");
        }
    }
}
