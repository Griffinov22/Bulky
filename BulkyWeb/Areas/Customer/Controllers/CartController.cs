using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM? ShoppingCartVM { get; set; }


        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // finds signed in userId
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<ShoppingCart> userCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, "Product").ToList();

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = userCarts,
                OrderHeader = new OrderHeader { OrderTotal = 0 },
            };

            foreach (ShoppingCart cart in userCarts)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            // finds signed in userId
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<ShoppingCart> userCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, "Product").ToList();

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = userCarts,
                OrderHeader = new OrderHeader
                {
                    OrderTotal = 0,
                    ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId)
                },
            };

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


            foreach (ShoppingCart cart in userCarts)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(ShoppingCartVM);
        }
        [HttpPost]
        public IActionResult Summary(ShoppingCartVM scvm)
        {
            // finds signed in userId
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            string userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            scvm.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, "Product").ToList();

            scvm.OrderHeader.OrderDate = DateTime.Now;
            scvm.OrderHeader.ApplicationUserId = userId;
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            foreach (ShoppingCart cart in scvm.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                scvm.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // NOT a company account -> require payment now
                scvm.OrderHeader.OrderStatus = SD.StatusPending;
                scvm.OrderHeader.PaymentStatus = SD.PaymentStatusPending;

            }
            else
            {
                // company account -> no required payment for 30 days
                scvm.OrderHeader.OrderStatus = SD.StatusApproved;
                scvm.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            }

            _unitOfWork.OrderHeader.Add(scvm.OrderHeader);
            _unitOfWork.Save();

            foreach (ShoppingCart cart in scvm.ShoppingCartList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = scvm.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count,
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // regular customer account and need to capture payment now
                string domain = "https://localhost:5000";

                // stripe logic
                var options = new SessionCreateOptions
                {
                    SuccessUrl = $"{domain}/customer/cart/OrderConfirmation?id={scvm.OrderHeader.Id}",
                    CancelUrl = $"{domain}/customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                // adding line items
                foreach (ShoppingCart cart in scvm.ShoppingCartList)
                {
                    SessionLineItemOptions item = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(cart.Price * 100), // $20.50 -> 2050
                            Currency = "USD",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = cart.Product.Title,
                            }
                        },
                        Quantity = cart.Count

                    };
                    options.LineItems.Add(item);

                }

                var service = new SessionService();
                Session session = service.Create(options);

                // payment intent Id will be null because the client needs to be routed to Stripe. It will only be populated once the client has paid.
                _unitOfWork.OrderHeader.UpdateStripePaymentId(scvm.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303); // redirecting to new url
            }

            _unitOfWork.Save();
            return RedirectToAction("OrderConfirmation", new { id = scvm.OrderHeader.Id });
        }

        // order header id
        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader? orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, "ApplicationUser");

            if (orderHeader == null) return NotFound();

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // order from a customer
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeader.Id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            // remove all shopping carts from the user
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
        }

        #region PRIVATE METHODS
        private static decimal GetPriceBasedOnQuantity(ShoppingCart cart)
        {
            if (cart.Count >= 100)
            {
                return cart.Product.Price100;
            }
            else if (cart.Count >= 50)
            {
                return cart.Product.Price50;
            }
            else
            {
                return cart.Product.Price;
            }
        }
        #endregion

        #region HTTP Methods
        public IActionResult Plus(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cart.Count++;
            _unitOfWork.ShoppingCart.Update(cart);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }
        public IActionResult Minus(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cart.Count - 1 > 0)
            {
                cart.Count--;
                _unitOfWork.ShoppingCart.Update(cart);
            }
            else
            {
                _unitOfWork.ShoppingCart.Remove(cart);
            }
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }
        public IActionResult Remove(int cartId)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }
        #endregion
    }
}
