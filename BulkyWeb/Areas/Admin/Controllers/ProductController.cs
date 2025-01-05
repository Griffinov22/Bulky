using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            return View(objProductList);
        }

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category
            .GetAll()
            .Select(e => new SelectListItem { Text = e.Name, Value = e.Id.ToString() });
            
            ProductVM pvm = new()
            {
                CategoryList = CategoryList,
                Product = new Product()
            };

            if (id == null || id == 0) 
            {
                // create
                return View(pvm);
            }

            // retreive product from id and update
            Product product = _unitOfWork.Product.Get(u => u.Id == id);
            if (product == null) { return NotFound(); }

            pvm.Product = product;
            return View(pvm);
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM pvm, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    // need to delete old file
                    if (!String.IsNullOrEmpty(pvm.Product.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(wwwRootPath, pvm.Product.ImageUrl.Trim('\\'));
                        if (System.IO.File.Exists(oldImagePath)) 
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // upload new image
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    pvm.Product.ImageUrl = @"\images\product\" + fileName;

                }
                
                if (pvm.Product.Id != 0)
                {
                    // update
                    _unitOfWork.Product.Update(pvm.Product);
                }
                else
                {
                    // create
                    _unitOfWork.Product.Add(pvm.Product);
                }
                _unitOfWork.Save();
                TempData["success"] = "Product created successfully";
                return RedirectToAction("Index");
            } else
            {
                // re-instantiate vm

                pvm.CategoryList = _unitOfWork.Category
                .GetAll(includeProperties: "Category")
                .Select(e => new SelectListItem { Text = e.Name, Value = e.Id.ToString() });

                return View(pvm); // invalid Product submitted
            }

        }

        #region APICALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product? objToDelete = _unitOfWork.Product.Get(u => u.Id == id);

            if (objToDelete == null)
            {
                return Json(new { success = false, message = "Error deleting product" });
            }

            //remove image
            if (!String.IsNullOrEmpty(objToDelete.ImageUrl))
            {
                string imagePathToDelete = Path.Combine(_webHostEnvironment.WebRootPath,
                objToDelete.ImageUrl.TrimStart('\\'));

                if (!String.IsNullOrEmpty(imagePathToDelete))
                {
                    if (System.IO.File.Exists(imagePathToDelete))
                    {
                        System.IO.File.Delete(imagePathToDelete);
                    }
                }
            }

            _unitOfWork.Product.Remove(objToDelete);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete successful" });
            

        }
        #endregion


    }
}
