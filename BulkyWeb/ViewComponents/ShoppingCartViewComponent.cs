using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            string? userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) == null)
                {
                    int amountOfCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();

                    HttpContext.Session.SetInt32(SD.SessionCart, amountOfCarts);
                }

                return View(HttpContext.Session.GetInt32(SD.SessionCart));
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
