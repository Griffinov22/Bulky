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
    public class CompanyController: Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index() 
        {
            List<Company> companies = _unitOfWork.Company.GetAll().ToList();
            return View(companies); 
        }

        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            // if id present -> update
            // else insert
            if (id == null || id == 0)
            {
                return View(new Company { });
            } else
            {
                Company company = _unitOfWork.Company.Get(obj => obj.Id == id);

                if (company == null) return NotFound();

                return View(company);

            }
        }
        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id != 0)
                {
                    // update
                    _unitOfWork.Company.Update(company);

                }
                else
                {
                    // create
                    _unitOfWork.Company.Add(company);
                }
                _unitOfWork.Save();
                TempData["success"] = "Company created successfully";
                return RedirectToAction("Index");
            }
            else
            {

                return View();
            }
        }

        #region APICALLS

        [HttpGet]
        public JsonResult GetAll() 
        {
            return Json(new { data = _unitOfWork.Company.GetAll().ToList() });
        }

        [HttpDelete]
        public IActionResult Delete(int? id) 
        {
            Company? objToDelete = _unitOfWork.Company.Get(obj => obj.Id == id);
            if (objToDelete != null) 
            {
                _unitOfWork.Company.Remove(objToDelete);
                _unitOfWork.Save();
            }

            string message = objToDelete is { } ? "Successfully deleted company" : "Error deleting company";

            return Json(new { success = objToDelete is { }, message});

        }
        
        #endregion
    }
}
