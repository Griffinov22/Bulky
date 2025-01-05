using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe.Climate;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM Ovm { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            Ovm = new OrderVM
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, "Product")
            };


            return View(Ovm);
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == Ovm.OrderHeader.Id, "ApplicationUser");

            orderHeader.Name = Ovm.OrderHeader.Name;
            orderHeader.PhoneNumber = Ovm.OrderHeader.PhoneNumber;
            orderHeader.StreetAddress = Ovm.OrderHeader.StreetAddress;
            orderHeader.City = Ovm.OrderHeader.City;
            orderHeader.State = Ovm.OrderHeader.State;
            orderHeader.PostalCode = Ovm.OrderHeader.PostalCode;

            if (!String.IsNullOrEmpty(Ovm.OrderHeader.Carrier))
            {
                orderHeader.Carrier = Ovm.OrderHeader.Carrier;
            }
            if (!String.IsNullOrEmpty(Ovm.OrderHeader.TrackingNumber))
            {
                orderHeader.TrackingNumber = Ovm.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["success"] = "Order Details updated successfuly";

            return RedirectToAction("Details", new { orderId = orderHeader.Id });
        }

        #region API_CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                // employees and admins can see all orders
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            } else
            {
                // customers or companies can only see their orders
                ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
                string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                orderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId,includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(order => order.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(order => order.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(order => order.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(order => order.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }


            return Json(new { data = orderHeaders.ToList() });
        }
        #endregion
    }
}
