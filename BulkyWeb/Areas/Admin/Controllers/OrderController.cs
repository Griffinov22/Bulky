using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
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

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(Ovm.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            TempData["success"] = "Order Details updated successfuly";

            return RedirectToAction("Details", new { orderId = Ovm.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            // update tracking number and carrier on shipped status
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == Ovm.OrderHeader.Id);
            orderHeader.TrackingNumber = Ovm.OrderHeader.TrackingNumber;
            orderHeader.Carrier = Ovm.OrderHeader.Carrier;

            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            // if company, then payment is due +30 days from shipment
            if (orderHeader.OrderStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();

            TempData["success"] = "Order Shipped successfuly";

            return RedirectToAction("Details", new { orderId = Ovm.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == Ovm.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                // customer paid -> we need to refund using Stripe
                RefundCreateOptions options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId,
                };

                // refund completed
                RefundService service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            } else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();

            TempData["success"] = "Order Cancelled successfuly";
            return RedirectToAction("Details", new { orderId = Ovm.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult PayNow()
        {
            // company paying (companies have net30)

            Ovm.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == Ovm.OrderHeader.Id, "ApplicationUser");
            Ovm.OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == Ovm.OrderHeader.Id, "Product");

            // stripe logic
            string domain = $"{Request.Scheme}://{Request.Host.Value}/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = $"{domain}/admin/order/PaymentConfirmation?orderHeaderId={Ovm.OrderHeader.Id}",
                CancelUrl = $"{domain}/admin/order/details?orderId={Ovm.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            // adding line items
            foreach (OrderDetail orderDetail in Ovm.OrderDetails)
            {
                SessionLineItemOptions item = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(orderDetail.Price * 100), // $20.50 -> 2050
                        Currency = "USD",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = orderDetail.Product.Title,
                        }
                    },
                    Quantity = orderDetail.Count

                };
                options.LineItems.Add(item);

            }

            var service = new SessionService();
            Session session = service.Create(options);

            // payment intent Id will be null because the client needs to be routed to Stripe. It will only be populated once the client has paid.
            _unitOfWork.OrderHeader.UpdateStripePaymentId(Ovm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303); // redirecting to new url
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader? orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);

            if (orderHeader == null) return NotFound();

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // order from a company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            return View(orderHeaderId);
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
