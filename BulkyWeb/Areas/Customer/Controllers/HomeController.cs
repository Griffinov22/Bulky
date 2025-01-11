using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            string? userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null) 
            {
                int amountOfCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                HttpContext.Session.SetInt32(SD.SessionCart, amountOfCarts);
            }


            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(productList);
        }

        public IActionResult Details(int productId)
        {
            ShoppingCart userCart = new ShoppingCart
            {
                Product = _unitOfWork.Product.Get(obj => obj.Id == productId, "Category"),
                Count = 1,
                ProductId = productId
            };

            return View(userCart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            // finds signed in userId
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cart.ApplicationUserId = userId;

            ShoppingCart prevCart = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == cart.ProductId);

            if (prevCart is { }) 
            {
                prevCart.Count = cart.Count;
                _unitOfWork.ShoppingCart.Update(prevCart);
                _unitOfWork.Save();
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(cart);
                _unitOfWork.Save();

                int amountOfCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();
                HttpContext.Session.SetInt32(SD.SessionCart, amountOfCarts);
            }

            TempData["success"] = "Cart updated successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
