using Microsoft.AspNetCore.Mvc;
using DAL.Data.Models;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Art_Touch.Controllers
{
    public class ShopController : Controller
    {
        private readonly StoreDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopController(StoreDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var newArrivals = await _context.Products
                .Where(p => p.IsNewArrival && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Take(4)
                .ToListAsync();
                
            var bestSellers = await _context.Products
                .Where(p => p.IsBestseller && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Take(4)
                .ToListAsync();
                
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();
            
            ViewBag.NewArrivals = newArrivals;
            ViewBag.BestSellers = bestSellers;
            ViewBag.Categories = categories;
            
            return View();
        }

        public async Task<IActionResult> Products(int? categoryId, string sortBy = "newest")
        {
            IQueryable<Product> query = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images);
                
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            
            var products = sortBy switch
            {
                "price_low" => await query.OrderBy(p => p.DiscountPrice ?? p.OriginalPrice).ToListAsync(),
                "price_high" => await query.OrderByDescending(p => p.DiscountPrice ?? p.OriginalPrice).ToListAsync(),
                "name" => await query.OrderBy(p => p.Name).ToListAsync(),
                _ => await query.OrderByDescending(p => p.Id).ToListAsync() // newest first
            };
            
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSort = sortBy;
            
            return View(products);
        }

        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (product == null) return NotFound();
            
            // Get related products from same category
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Take(4)
                .ToListAsync();
                
            ViewBag.RelatedProducts = relatedProducts;
            
            return View(product);
        }

        public async Task<IActionResult> NewArrivals()
        {
            var newArrivals = await _context.Products
                .Where(p => p.IsNewArrival && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();
                
            return View(newArrivals);
        }

        public async Task<IActionResult> BestSellers()
        {
            var bestSellers = await _context.Products
                .Where(p => p.IsBestseller && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();
                
            return View(bestSellers);
        }

        public async Task<IActionResult> Category(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            
            var products = await _context.Products
                .Where(p => p.CategoryId == id && p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();
                
            ViewBag.Category = category;
            
            return View(products);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, string size, int quantity = 1)
        {
            // Get or create cart from session
            var cart = GetCartFromSession();
            
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    Size = size ?? "",
                    Quantity = quantity
                });
            }
            
            SaveCartToSession(cart);
            
            return Json(new { success = true, cartCount = cart.Sum(c => c.Quantity) });
        }

        public async Task<IActionResult> Cart()
        {
            var cart = GetCartFromSession();
            
            // Load product details for cart items
            foreach (var item in cart)
            {
                item.Product = await _context.Products.FindAsync(item.ProductId);
            }
            
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateCart(int productId, string size, int quantity)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCartToSession(cart);
            }
            
            return Json(new { success = true, cartCount = cart.Sum(c => c.Quantity) });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId, string size)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            
            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
            }
            
            return RedirectToAction("Cart");
        }

        public IActionResult Checkout()
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Shop") });
            }

            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                return RedirectToAction("Cart");
            }
            
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(OrderViewModel orderModel)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var cart = GetCartFromSession();
                if (!cart.Any())
                {
                    return RedirectToAction("Cart");
                }

                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // Create order
                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    ShippingAddress = $"{orderModel.FirstName} {orderModel.LastName}, {orderModel.Address}, {orderModel.City}",
                    TotalAmount = await CalculateCartTotal(cart)
                };
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                
                // Add order items
                foreach (var cartItem in cart)
                {
                    var product = await _context.Products.FindAsync(cartItem.ProductId);
                    if (product != null)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.ProductId,
                            Size = cartItem.Size,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.DiscountPrice ?? product.OriginalPrice,
                            Price = (product.DiscountPrice ?? product.OriginalPrice) * cartItem.Quantity
                        };
                        
                        _context.OrderItems.Add(orderItem);
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Clear cart
                HttpContext.Session.Remove("Cart");
                
                return View("OrderConfirmation", order);
            }
            
            return View("Checkout", GetCartFromSession());
        }

        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cartJson) 
                ? new List<CartItem>() 
                : System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            var cartJson = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        private async Task<decimal> CalculateCartTotal(List<CartItem> cart)
        {
            decimal total = 0;
            foreach (var item in cart)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    total += (product.DiscountPrice ?? product.OriginalPrice) * item.Quantity;
                }
            }
            return total;
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string Size { get; set; } = "";
        public int Quantity { get; set; }
        public Product? Product { get; set; }
    }

    public class OrderViewModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string Country { get; set; } = "";
    }
}

