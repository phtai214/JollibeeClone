using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Attributes;
using JollibeeClone.Areas.Admin.Services;
using JollibeeClone.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json;

namespace JollibeeClone.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPromotionService _promotionService;
        private readonly OrderStatusHistoryService _statusHistoryService;
        private readonly ShippingService _shippingService;
        
        public CartController(AppDbContext context, IPromotionService promotionService, OrderStatusHistoryService statusHistoryService, ShippingService shippingService)
        {
            _context = context;
            _promotionService = promotionService;
            _statusHistoryService = statusHistoryService;
            _shippingService = shippingService;
        }

        // GET: /Cart/GetCart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                Console.WriteLine($"🔍 GetCart called - Session ID: {HttpContext.Session.Id}");
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                Console.WriteLine($"🔍 Session auth in GetCart - IsLoggedIn: {isUserLoggedIn}, UserID: {userIdFromSession}");
                
                var cart = await GetOrCreateCartAsync();
                Console.WriteLine($"🔍 Cart retrieved: ID={cart.CartID}, UserID={cart.UserID}, SessionID={cart.SessionID}");
                
                var cartViewModel = await MapCartToViewModelAsync(cart);
                Console.WriteLine($"🔍 Cart items count: {cartViewModel.CartItems?.Count ?? -1}");
                Console.WriteLine($"🔍 Total amount: {cartViewModel.TotalAmount}");
                
                // TEMPORARY FIX: Use Newtonsoft.Json explicitly
                var jsonResponse = JsonConvert.SerializeObject(new { success = true, data = cartViewModel });
                Console.WriteLine($"🔧 GetCart using Newtonsoft.Json for response");
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetCart: {ex.Message}");
                var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Lỗi khi tải giỏ hàng: " + ex.Message });
                return Content(errorResponse, "application/json");
            }
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                Console.WriteLine($"🛒 AddToCart called with ProductID: {request?.ProductID}, Quantity: {request?.Quantity}");
                Console.WriteLine($"🛒 Session ID in AddToCart: {HttpContext.Session.Id}");
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                Console.WriteLine($"🛒 Session auth in AddToCart - IsLoggedIn: {isUserLoggedIn}, UserID: {userIdFromSession}");
                Console.WriteLine($"🛒 Selected options count: {request?.SelectedOptions?.Count ?? 0}");
                
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("❌ ModelState is invalid");
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Lấy thông tin sản phẩm
                var product = await _context.Products
                    .Include(p => p.ProductConfigurationGroups)
                        .ThenInclude(g => g.ProductConfigurationOptions)
                            .ThenInclude(o => o.OptionProduct)
                    .Include(p => p.ProductConfigurationGroups)
                        .ThenInclude(g => g.ProductConfigurationOptions)
                            .ThenInclude(o => o.Variant)
                    .FirstOrDefaultAsync(p => p.ProductID == request.ProductID && p.IsAvailable);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc không có sẵn" });
                }

                // Lấy hoặc tạo giỏ hàng
                var cart = await GetOrCreateCartAsync();
                Console.WriteLine($"🛒 Cart retrieved/created: {cart.CartID}");

                // Tính giá sản phẩm
                decimal totalPrice = product.Price;
                var configurationSnapshot = new List<object>();

                // Xử lý configuration options cho combo
                if (product.IsConfigurable && request.SelectedOptions.Any())
                {
                    foreach (var selectedOption in request.SelectedOptions)
                    {
                        var configOption = await _context.ProductConfigurationOptions
                            .Include(o => o.OptionProduct)
                            .Include(o => o.Variant)
                            .Include(o => o.ConfigGroup)
                            .FirstOrDefaultAsync(o => o.ConfigOptionID == selectedOption.ConfigOptionID);

                        if (configOption != null)
                        {
                            totalPrice += configOption.PriceAdjustment;
                            
                            configurationSnapshot.Add(new
                            {
                                GroupName = configOption.ConfigGroup.GroupName,
                                OptionID = configOption.ConfigOptionID,
                                OptionProductID = configOption.OptionProductID,
                                OptionProductName = configOption.OptionProduct.ProductName,
                                OptionProductImage = configOption.CustomImageUrl ?? configOption.OptionProduct.ThumbnailUrl ?? configOption.OptionProduct.ImageUrl,
                                PriceAdjustment = configOption.PriceAdjustment,
                                Quantity = configOption.Quantity,
                                VariantID = configOption.VariantID,
                                VariantName = configOption.Variant?.VariantName,
                                VariantType = configOption.Variant?.VariantType
                            });
                        }
                    }
                }

                // Kiểm tra xem sản phẩm đã có trong giỏ với cùng configuration chưa
                var configurationJson = JsonConvert.SerializeObject(configurationSnapshot);
                Console.WriteLine($"🛒 Configuration JSON: {configurationJson}");
                
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartID == cart.CartID 
                                             && ci.ProductID == request.ProductID 
                                             && ci.SelectedConfigurationSnapshot == configurationJson);

                Console.WriteLine($"🛒 Existing cart item found: {existingCartItem != null}");

                if (existingCartItem != null)
                {
                    // Cập nhật số lượng
                    existingCartItem.Quantity += request.Quantity;
                    existingCartItem.UnitPrice = totalPrice;
                    _context.CartItems.Update(existingCartItem);
                    Console.WriteLine($"🛒 Updated existing cart item. New quantity: {existingCartItem.Quantity}");
                }
                else
                {
                    // Thêm mới
                    var cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = request.ProductID,
                        Quantity = request.Quantity,
                        UnitPrice = totalPrice,
                        SelectedConfigurationSnapshot = configurationJson,
                        DateAdded = DateTime.Now
                    };
                    
                    _context.CartItems.Add(cartItem);
                    Console.WriteLine($"🛒 Added new cart item. CartID: {cart.CartID}, ProductID: {request.ProductID}, Quantity: {request.Quantity}, UnitPrice: {totalPrice}");
                }

                // Cập nhật thời gian giỏ hàng
                cart.LastUpdatedDate = DateTime.Now;
                _context.Carts.Update(cart);

                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"✅ SaveChanges result: {saveResult} rows affected");

                // Kiểm tra CartItem đã được save vào database chưa
                var savedCartItems = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .Include(ci => ci.Product)
                    .ToListAsync();
                Console.WriteLine($"🔍 CartItems trong database sau save: {savedCartItems.Count} items");
                
                foreach (var savedItem in savedCartItems)
                {
                    Console.WriteLine($"🔍 SavedItem: ID={savedItem.CartItemID}, ProductID={savedItem.ProductID}, ProductName={savedItem.Product?.ProductName}, Quantity={savedItem.Quantity}");
                }

                // Lấy thông tin giỏ hàng cập nhật
                var updatedCart = await GetOrCreateCartAsync();
                var cartViewModel = await MapCartToViewModelAsync(updatedCart);

                Console.WriteLine($"✅ Cart updated successfully with {cartViewModel.CartItems?.Count ?? -1} items");

                // Debug JSON serialization
                try
                {
                    var jsonString = JsonConvert.SerializeObject(cartViewModel);
                    Console.WriteLine($"🔍 Serialized JSON length: {jsonString.Length}");
                    Console.WriteLine($"🔍 JSON contains 'CartItems': {jsonString.Contains("CartItems")}");
                    
                    // Test deserialization
                    var deserializedCart = JsonConvert.DeserializeObject<CartViewModel>(jsonString);
                    Console.WriteLine($"🔍 Deserialized CartItems count: {deserializedCart?.CartItems?.Count ?? -1}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ JSON serialization error: {ex.Message}");
                }

                // TEMPORARY FIX: Use Newtonsoft.Json explicitly
                var jsonResponse = JsonConvert.SerializeObject(new { 
                    success = true, 
                    message = "Đã thêm vào giỏ hàng thành công!",
                    data = cartViewModel 
                });
                
                Console.WriteLine($"🔧 Using Newtonsoft.Json for response");
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in AddToCart: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Lỗi khi thêm vào giỏ hàng: " + ex.Message });
            }
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                Console.WriteLine($"🔄 UpdateQuantity called - CartItemID: {request?.CartItemID}, Quantity: {request?.Quantity}");
                
                if (!ModelState.IsValid)
                {
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Dữ liệu không hợp lệ" });
                    return Content(errorResponse, "application/json");
                }

                var cart = await GetOrCreateCartAsync();
                Console.WriteLine($"🔄 Cart for update: ID={cart.CartID}");
                
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartItemID == request.CartItemID && ci.CartID == cart.CartID);

                if (cartItem == null)
                {
                    Console.WriteLine($"❌ CartItem not found: CartItemID={request.CartItemID}");
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                    return Content(errorResponse, "application/json");
                }

                Console.WriteLine($"🔄 Updating quantity from {cartItem.Quantity} to {request.Quantity}");
                cartItem.Quantity = request.Quantity;
                cart.LastUpdatedDate = DateTime.Now;

                _context.CartItems.Update(cartItem);
                _context.Carts.Update(cart);
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"🔄 Update SaveChanges result: {saveResult} rows affected");

                var cartViewModel = await MapCartToViewModelAsync(cart);

                var jsonResponse = JsonConvert.SerializeObject(new { 
                    success = true, 
                    message = "Cập nhật số lượng thành công!",
                    data = cartViewModel 
                });
                
                Console.WriteLine($"🔄 Update completed successfully");
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in UpdateQuantity: {ex.Message}");
                var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
                return Content(errorResponse, "application/json");
            }
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemRequest request)
        {
            try
            {
                Console.WriteLine($"🗑️ RemoveItem called - CartItemID: {request?.CartItemID}");
                
                if (!ModelState.IsValid)
                {
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Dữ liệu không hợp lệ" });
                    return Content(errorResponse, "application/json");
                }

                var cart = await GetOrCreateCartAsync();
                Console.WriteLine($"🗑️ Cart for remove: ID={cart.CartID}");
                
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartItemID == request.CartItemID && ci.CartID == cart.CartID);

                if (cartItem == null)
                {
                    Console.WriteLine($"❌ CartItem not found for removal: CartItemID={request.CartItemID}");
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                    return Content(errorResponse, "application/json");
                }

                Console.WriteLine($"🗑️ Removing CartItem: ID={cartItem.CartItemID}, ProductID={cartItem.ProductID}");
                _context.CartItems.Remove(cartItem);
                cart.LastUpdatedDate = DateTime.Now;
                _context.Carts.Update(cart);
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"🗑️ Remove SaveChanges result: {saveResult} rows affected");

                var cartViewModel = await MapCartToViewModelAsync(cart);

                var jsonResponse = JsonConvert.SerializeObject(new { 
                    success = true, 
                    message = "Đã xóa sản phẩm khỏi giỏ hàng!",
                    data = cartViewModel 
                });
                
                Console.WriteLine($"🗑️ Remove completed successfully");
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in RemoveItem: {ex.Message}");
                var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Lỗi khi xóa: " + ex.Message });
                return Content(errorResponse, "application/json");
            }
        }

        // POST: /Cart/ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                Console.WriteLine($"🧹 ClearCart called");
                
                var cart = await GetOrCreateCartAsync();
                Console.WriteLine($"🧹 Cart to clear: ID={cart.CartID}");
                
                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .ToListAsync();

                Console.WriteLine($"🧹 Found {cartItems.Count} items to clear");

                if (cartItems.Any())
                {
                    _context.CartItems.RemoveRange(cartItems);
                    cart.LastUpdatedDate = DateTime.Now;
                    _context.Carts.Update(cart);
                    var saveResult = await _context.SaveChangesAsync();
                    Console.WriteLine($"🧹 Clear SaveChanges result: {saveResult} rows affected");
                }

                var cartViewModel = await MapCartToViewModelAsync(cart);

                var jsonResponse = JsonConvert.SerializeObject(new { 
                    success = true, 
                    message = "Đã xóa tất cả sản phẩm khỏi giỏ hàng!",
                    data = cartViewModel 
                });
                
                Console.WriteLine($"🧹 Clear completed successfully");
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ClearCart: {ex.Message}");
                var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Lỗi khi xóa giỏ hàng: " + ex.Message });
                return Content(errorResponse, "application/json");
            }
        }

        // POST: /Cart/UpdateItemConfiguration
        [HttpPost]
        public async Task<IActionResult> UpdateItemConfiguration([FromBody] UpdateCartItemConfigurationRequest request)
        {
            try
            {
                Console.WriteLine($"✏️ UpdateItemConfiguration called - CartItemID: {request?.CartItemID}, Quantity: {request?.Quantity}");
                
                if (!ModelState.IsValid)
                {
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Dữ liệu không hợp lệ" });
                    return Content(errorResponse, "application/json");
                }

                var cart = await GetOrCreateCartAsync();
                Console.WriteLine($"✏️ Cart for update: ID={cart.CartID}");
                
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.CartItemID == request.CartItemID && ci.CartID == cart.CartID);

                if (cartItem == null)
                {
                    Console.WriteLine($"❌ CartItem not found: CartItemID={request.CartItemID}");
                    var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                    return Content(errorResponse, "application/json");
                }

                Console.WriteLine($"✏️ Updating cart item configuration for ProductID: {cartItem.ProductID}");

                // Calculate new configuration and price
                var configurationData = new List<dynamic>();
                decimal totalPriceAdjustment = 0;

                foreach (var selectedOption in request.SelectedOptions)
                {
                    // Get configuration option details
                    var configOption = await _context.ProductConfigurationOptions
                        .Include(pco => pco.ConfigGroup)
                        .Include(pco => pco.OptionProduct)
                        .Include(pco => pco.Variant)
                        .FirstOrDefaultAsync(pco => pco.ConfigOptionID == selectedOption.ConfigOptionID);

                    if (configOption != null)
                    {
                        // Add price adjustment
                        totalPriceAdjustment += configOption.PriceAdjustment;

                        // Store configuration data
                        configurationData.Add(new
                        {
                            GroupName = configOption.ConfigGroup.GroupName,
                            OptionID = configOption.ConfigOptionID,
                            OptionProductID = configOption.OptionProductID,
                            OptionProductName = configOption.OptionProduct.ProductName,
                            OptionProductImage = configOption.CustomImageUrl ?? configOption.OptionProduct.ThumbnailUrl ?? configOption.OptionProduct.ImageUrl,
                            PriceAdjustment = configOption.PriceAdjustment,
                            Quantity = configOption.Quantity,
                            VariantName = configOption.Variant?.VariantName,
                            VariantType = configOption.Variant?.VariantType
                        });

                        Console.WriteLine($"✏️ Added option: {configOption.OptionProduct.ProductName}, PriceAdj: {configOption.PriceAdjustment}");
                    }
                }

                // Update cart item
                cartItem.Quantity = request.Quantity;
                cartItem.UnitPrice = cartItem.Product.Price + totalPriceAdjustment;
                cartItem.SelectedConfigurationSnapshot = JsonConvert.SerializeObject(configurationData);
                cart.LastUpdatedDate = DateTime.Now;

                Console.WriteLine($"✏️ Updated UnitPrice: {cartItem.UnitPrice}, TotalPriceAdj: {totalPriceAdjustment}");

                _context.CartItems.Update(cartItem);
                _context.Carts.Update(cart);
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"✏️ UpdateItemConfiguration SaveChanges result: {saveResult} rows affected");

                var cartViewModel = await MapCartToViewModelAsync(cart);

                var jsonResponse = JsonConvert.SerializeObject(new { 
                    success = true, 
                    message = "Cập nhật sản phẩm thành công!",
                    data = cartViewModel 
                });
                
                Console.WriteLine($"✏️ UpdateItemConfiguration completed successfully");
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in UpdateItemConfiguration: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Lỗi khi cập nhật sản phẩm: " + ex.Message });
                return Content(errorResponse, "application/json");
            }
        }

        // GET: /Cart/TestJson - FOR DEBUGGING JSON SERIALIZATION
        [HttpGet]
        public JsonResult TestJson()
        {
            var testCart = new CartViewModel
            {
                CartID = Guid.NewGuid(),
                UserID = 123,
                SessionID = "test-session",
                CartItems = new List<CartItemViewModel>
                {
                    new CartItemViewModel
                    {
                        CartItemID = 1,
                        ProductID = 1,
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 50000,
                        TotalPrice = 100000
                    }
                },
                TotalAmount = 100000,
                TotalItems = 2,
                FinalAmount = 100000
            };

            Console.WriteLine($"🧪 TestJson - CartItems count: {testCart.CartItems.Count}");
            return Json(new { success = true, data = testCart });
        }

        // GET: /Cart/TestRawJson - RETURN RAW JSON STRING
        [HttpGet]
        public IActionResult TestRawJson()
        {
            var testCart = new CartViewModel
            {
                CartID = Guid.NewGuid(),
                UserID = 123,
                SessionID = "test-session",
                CartItems = new List<CartItemViewModel>
                {
                    new CartItemViewModel
                    {
                        CartItemID = 1,
                        ProductID = 1,
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 50000,
                        TotalPrice = 100000
                    }
                },
                TotalAmount = 100000,
                TotalItems = 2,
                FinalAmount = 100000
            };

            // Use Newtonsoft.Json explicitly
            var jsonString = JsonConvert.SerializeObject(new { success = true, data = testCart }, Formatting.Indented);
            Console.WriteLine($"🧪 TestRawJson - CartItems count: {testCart.CartItems.Count}");
            Console.WriteLine($"🧪 Raw JSON: {jsonString}");
            
            return Content(jsonString, "application/json");
        }

        // GET: /Cart/Debug - FOR DEBUGGING ONLY
        [HttpGet]
        public async Task<JsonResult> Debug()
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var isAuthenticated = User.Identity?.IsAuthenticated == true;
                
                var allCarts = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .ToListAsync();
                
                var debugInfo = new
                {
                    CurrentSessionId = sessionId,
                    IsAuthenticated = isAuthenticated,
                    TotalCartsInDb = allCarts.Count,
                    Carts = allCarts.Select(c => new
                    {
                        c.CartID,
                        c.UserID,
                        c.SessionID,
                        c.CreatedDate,
                        c.LastUpdatedDate,
                        ItemsCount = c.CartItems.Count,
                        Items = c.CartItems.Select(ci => new
                        {
                            ci.CartItemID,
                            ci.ProductID,
                            ProductName = ci.Product.ProductName,
                            ci.Quantity,
                            ci.UnitPrice,
                            ci.DateAdded,
                            HasConfiguration = !string.IsNullOrEmpty(ci.SelectedConfigurationSnapshot),
                            ConfigurationLength = ci.SelectedConfigurationSnapshot?.Length ?? 0
                        }).ToList()
                    }).ToList()
                };
                
                return Json(new { success = true, data = debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // GET: /Cart/GetCartSummary
        [HttpGet]
        public async Task<IActionResult> GetCartSummary()
        {
            try
            {
                var cart = await GetOrCreateCartAsync();
                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .ToListAsync();

                var summary = new CartSummaryViewModel
                {
                    TotalItems = cartItems.Sum(ci => ci.Quantity),
                    SubTotal = cartItems.Sum(ci => ci.UnitPrice * ci.Quantity),
                    TaxAmount = 0, // Tạm thời chưa có thuế
                    ShippingFee = 0, // Tạm thời miễn phí ship
                    DiscountAmount = 0, // Tạm thời chưa có discount
                };
                
                summary.TotalAmount = summary.SubTotal + summary.TaxAmount + summary.ShippingFee - summary.DiscountAmount;

                var jsonResponse = JsonConvert.SerializeObject(new { success = true, data = summary });
                return Content(jsonResponse, "application/json");
            }
            catch (Exception ex)
            {
                var errorResponse = JsonConvert.SerializeObject(new { success = false, message = "Lỗi khi tải tổng kết giỏ hàng: " + ex.Message });
                return Content(errorResponse, "application/json");
            }
        }

        // Private helper methods
        private async Task<Cart> GetOrCreateCartAsync()
        {
            // Ensure session is started
            await EnsureSessionStartedAsync();
            
            Cart? cart = null;
            var sessionId = HttpContext.Session.Id;
            
            Console.WriteLine($"🔍 GetOrCreateCartAsync - Session ID: {sessionId}");
            
            // Check session-based authentication (current system)
            var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
            var userIdFromSession = HttpContext.Session.GetString("UserId");
            
            Console.WriteLine($"🔍 Session-based auth - IsLoggedIn: {isUserLoggedIn}, UserID: {userIdFromSession}");

            // Nếu user đã login, tìm theo UserID từ session
            if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int userId))
            {
                Console.WriteLine($"🔍 Looking for cart by UserID: {userId}");
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == userId);
                Console.WriteLine($"🔍 Found cart by UserID: {cart != null}");
            }

            // Nếu chưa có cart hoặc user chưa login, tìm theo SessionID
            if (cart == null)
            {
                Console.WriteLine($"🔍 Looking for cart by SessionID: {sessionId}");
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.SessionID == sessionId);
                Console.WriteLine($"🔍 Found cart by SessionID: {cart != null}");
                
                if (cart != null)
                {
                    Console.WriteLine($"🔍 Cart found - ID: {cart.CartID}, Items count: {cart.CartItems.Count}");
                }
            }

            // Nếu vẫn chưa có, tạo mới
            if (cart == null)
            {
                Console.WriteLine($"🔍 Creating new cart");
                cart = new Cart();
                
                // Use session-based authentication for new cart too
                if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int newCartUserId))
                {
                    cart.UserID = newCartUserId;
                    Console.WriteLine($"🔍 New cart for UserID: {newCartUserId}");
                }
                else
                {
                    cart.SessionID = sessionId;
                    Console.WriteLine($"🔍 New cart for SessionID: {sessionId}");
                }

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                Console.WriteLine($"🔍 New cart created with ID: {cart.CartID}");

                // Reload cart với includes
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.CartID == cart.CartID);
            }

            return cart!;
        }

        private async Task EnsureSessionStartedAsync()
        {
            // Force session to start if not already started
            if (string.IsNullOrEmpty(HttpContext.Session.Id))
            {
                Console.WriteLine("🔧 Starting session...");
                await HttpContext.Session.LoadAsync();
                Console.WriteLine($"🔧 Session started: {HttpContext.Session.Id}");
            }
            else
            {
                Console.WriteLine($"🔧 Session already exists: {HttpContext.Session.Id}");
            }
        }

        private async Task<CartViewModel> MapCartToViewModelAsync(Cart cart)
        {
            Console.WriteLine($"🔍 MapCartToViewModelAsync - Cart ID: {cart.CartID}");
            
            var cartItems = await _context.CartItems
                .Where(ci => ci.CartID == cart.CartID)
                .Include(ci => ci.Product)
                .ToListAsync();

            Console.WriteLine($"🔍 Found {cartItems.Count} cart items in database");
            
            foreach (var item in cartItems)
            {
                Console.WriteLine($"🔍 CartItem ID: {item.CartItemID}, ProductID: {item.ProductID}, Quantity: {item.Quantity}, ConfigSnapshot: {!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot)}");
            }

            var cartItemViewModels = new List<CartItemViewModel>();

            foreach (var item in cartItems)
            {
                var configurations = new List<CartConfigurationViewModel>();

                // Parse configuration snapshot
                if (!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot))
                {
                    try
                    {
                        var configData = JsonConvert.DeserializeObject<List<dynamic>>(item.SelectedConfigurationSnapshot);
                        if (configData != null)
                        {
                            var groupedConfigs = configData.GroupBy(c => (string)c.GroupName);
                            
                            foreach (var group in groupedConfigs)
                            {
                                var configGroup = new CartConfigurationViewModel
                                {
                                    GroupName = group.Key,
                                    Options = group.Select(option => new CartOptionViewModel
                                    {
                                        ConfigOptionID = (int)option.OptionID,
                                        OptionProductID = (int)option.OptionProductID,
                                        OptionProductName = (string)option.OptionProductName,
                                        OptionProductImage = (string?)option.OptionProductImage,
                                        PriceAdjustment = (decimal)option.PriceAdjustment,
                                        Quantity = (int)option.Quantity,
                                        VariantName = (string?)option.VariantName,
                                        VariantType = (string?)option.VariantType
                                    }).ToList()
                                };
                                configurations.Add(configGroup);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue
                        Console.WriteLine($"Error parsing configuration: {ex.Message}");
                    }
                }

                cartItemViewModels.Add(new CartItemViewModel
                {
                    CartItemID = item.CartItemID,
                    CartID = item.CartID,
                    ProductID = item.ProductID,
                    ProductName = item.Product.ProductName,
                    ProductImage = item.Product.ImageUrl,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.UnitPrice * item.Quantity,
                    Quantity = item.Quantity,
                    DateAdded = item.DateAdded,
                    IsConfigurable = item.Product.IsConfigurable,
                    Configurations = configurations
                });
            }

            var totalAmount = cartItemViewModels.Sum(ci => ci.TotalPrice);

            Console.WriteLine($"🔍 CartViewModel created with {cartItemViewModels.Count} items, TotalAmount: {totalAmount}");

            var result = new CartViewModel
            {
                CartID = cart.CartID,
                UserID = cart.UserID,
                SessionID = cart.SessionID,
                CreatedDate = cart.CreatedDate,
                LastUpdatedDate = cart.LastUpdatedDate,
                CartItems = cartItemViewModels,
                TotalAmount = totalAmount,
                TotalItems = cartItemViewModels.Sum(ci => ci.Quantity),
                FinalAmount = totalAmount // Có thể áp dụng discount sau
            };

            // Debug serialization
            Console.WriteLine($"🔍 Final CartViewModel: CartID={result.CartID}, CartItems.Count={result.CartItems?.Count ?? -1}");
            Console.WriteLine($"🔍 CartItems is null: {result.CartItems == null}");
            
            return result;
        }

        // GET: /Cart/Shipping
        [HttpGet]
        public async Task<IActionResult> Shipping()
        {
            try
            {
                // Get current cart
                var cart = await GetOrCreateCartAsync();
                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .Include(ci => ci.Product)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["Message"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                    return RedirectToAction("Index", "Home");
                }

                // Check minimum order value
                const decimal MIN_ORDER_VALUE = 60000m;
                var subtotalAmount = await MapCartItemsToViewModelAsync(cartItems);
                var currentSubtotal = subtotalAmount.Sum(ci => ci.TotalPrice);
                
                if (currentSubtotal < MIN_ORDER_VALUE)
                {
                    var requiredAmount = MIN_ORDER_VALUE - currentSubtotal;
                    TempData["ErrorMessage"] = $"Đơn hàng tối thiểu {MIN_ORDER_VALUE:N0}₫ để có thể đặt hàng. Vui lòng thêm {requiredAmount:N0}₫ nữa.";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new CheckoutShippingViewModel();

                // Get user information if logged in
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                
                Console.WriteLine($"🚀 Shipping page load - Session check:");
                Console.WriteLine($"  - IsUserLoggedIn: {isUserLoggedIn}");
                Console.WriteLine($"  - UserIdFromSession: '{userIdFromSession}'");
                
                if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int userId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                    if (user != null)
                    {
                        Console.WriteLine($"✅ User found in database: {user.FullName} ({user.Email})");
                        
                        viewModel.CurrentUser = user;
                        viewModel.IsUserLoggedIn = true;
                        viewModel.CustomerFullName = user.FullName;
                        viewModel.CustomerEmail = user.Email;
                        viewModel.CustomerPhoneNumber = user.PhoneNumber ?? "";

                        Console.WriteLine($"📝 Setting viewModel data:");
                        Console.WriteLine($"  - CustomerFullName: '{viewModel.CustomerFullName}'");
                        Console.WriteLine($"  - CustomerEmail: '{viewModel.CustomerEmail}'");
                        Console.WriteLine($"  - CustomerPhoneNumber: '{viewModel.CustomerPhoneNumber}'");

                        // Get user addresses
                        viewModel.UserAddresses = await _context.UserAddresses
                            .Where(ua => ua.UserID == userId)
                            .OrderByDescending(ua => ua.IsDefault)
                            .ToListAsync();
                            
                        Console.WriteLine($"📍 User addresses loaded: {viewModel.UserAddresses.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ User not found in database for UserID: {userId}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ User not logged in or invalid session data");
                    viewModel.IsUserLoggedIn = false;
                }

                // Load delivery methods
                viewModel.DeliveryMethods = await _context.DeliveryMethods
                    .Where(dm => dm.IsActive)
                    .ToListAsync();

                // Load stores
                viewModel.Stores = await _context.Stores
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.StoreName)
                    .ToListAsync();

                // Map cart items to view model
                viewModel.CartItems = await MapCartItemsToViewModelAsync(cartItems);

                // Calculate totals
                viewModel.SubtotalAmount = viewModel.CartItems.Sum(ci => ci.TotalPrice);
                viewModel.DiscountAmount = 0; // Will be applied if promotions exist

                // Calculate shipping fee and freeship info
                var userIdForShipping = isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int shippingUserId) ? shippingUserId : (int?)null;
                
                // Get freeship promotion info to display
                var freeshipeInfo = await _shippingService.GetFreeshipePromotionInfoAsync(userIdForShipping, viewModel.SubtotalAmount);
                viewModel.IsFirstOrder = freeshipeInfo.IsFirstOrder;
                viewModel.FreeshipeMessage = freeshipeInfo.Message;
                viewModel.RequiredAmountForFreeship = freeshipeInfo.RequiredAmountForFreeship;
                
                // Default to delivery method 1 (giao hàng tận nơi) for initial calculation
                viewModel.DeliveryMethodID = 1; // Set default delivery method
                var shippingCalculation = await _shippingService.CalculateShippingFeeAsync(userIdForShipping, viewModel.SubtotalAmount, 1);
                viewModel.ShippingFee = shippingCalculation.ShippingFee;
                viewModel.IsFreeship = shippingCalculation.IsFreeship;
                
                viewModel.TotalAmount = viewModel.SubtotalAmount + viewModel.ShippingFee - viewModel.DiscountAmount;

                Console.WriteLine($"🚚 Shipping calculation for user {userIdForShipping}:");
                Console.WriteLine($"  - Subtotal: {viewModel.SubtotalAmount:N0}₫");
                Console.WriteLine($"  - Shipping: {viewModel.ShippingFee:N0}₫ (Freeship: {viewModel.IsFreeship})");
                Console.WriteLine($"  - Message: {viewModel.FreeshipeMessage}");
                Console.WriteLine($"  - Is First Order: {viewModel.IsFirstOrder}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Checkout: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thanh toán.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Cart/ProcessShipping
        [HttpPost]
        public async Task<IActionResult> ProcessShipping(CheckoutShippingViewModel model)
        {
            try
            {
                // Debug: Log all form data
                Console.WriteLine("📋 ProcessShipping called - Raw Form Data:");
                foreach (var formField in Request.Form)
                {
                    Console.WriteLine($"  Form[{formField.Key}] = '{formField.Value}'");
                    
                    // Special debug for date fields
                    if (formField.Key.Contains("PickupDate"))
                    {
                        var rawValue = formField.Value.ToString();
                        Console.WriteLine($"  📅 RAW PickupDate value: '{rawValue}'");
                        
                        if (DateTime.TryParse(rawValue, out DateTime parsedDate))
                        {
                            Console.WriteLine($"  📅 Parsed as DateTime: {parsedDate}");
                            Console.WriteLine($"  📅 Parsed Kind: {parsedDate.Kind}");
                            Console.WriteLine($"  📅 Local display: {parsedDate.ToString("dd/MM/yyyy")}");
                        }
                    }
                }
                
                // Debug: Log received model values
                Console.WriteLine("📋 ProcessShipping bound model data:");
                Console.WriteLine($"  - CustomerFullName: '{model.CustomerFullName}'");
                Console.WriteLine($"  - CustomerPhoneNumber: '{model.CustomerPhoneNumber}'");
                Console.WriteLine($"  - CustomerEmail: '{model.CustomerEmail}' (Length: {model.CustomerEmail?.Length ?? 0})");
                Console.WriteLine($"  - DeliveryMethodID: {model.DeliveryMethodID}");
                Console.WriteLine($"  - UserAddressID: {model.UserAddressID}");
                Console.WriteLine($"  - DeliveryAddress: '{model.DeliveryAddress}'");
                Console.WriteLine($"  - StoreID: {model.StoreID}");
                Console.WriteLine($"  - PickupDate: {model.PickupDate}");
                if (model.PickupDate.HasValue)
                {
                    Console.WriteLine($"    📅 PickupDate Details:");
                    Console.WriteLine($"    📅 Raw value: {model.PickupDate.Value}");
                    Console.WriteLine($"    📅 Kind: {model.PickupDate.Value.Kind}");
                    Console.WriteLine($"    📅 Display format: {model.PickupDate.Value.ToString("dd/MM/yyyy")}");
                    Console.WriteLine($"    📅 ISO format: {model.PickupDate.Value.ToString("yyyy-MM-dd")}");
                }
                Console.WriteLine($"  - PickupTimeSlot: {model.PickupTimeSlot}");
                Console.WriteLine($"  - NotesByCustomer: '{model.NotesByCustomer}'");
                Console.WriteLine($"  - IsUserLoggedIn: {model.IsUserLoggedIn}");
                
                // Clean up email and other fields
                if (!string.IsNullOrEmpty(model.CustomerEmail))
                {
                    // Trim whitespace
                    var originalEmail = model.CustomerEmail;
                    model.CustomerEmail = model.CustomerEmail.Trim();
                    
                    if (originalEmail != model.CustomerEmail)
                    {
                        Console.WriteLine($"  - Email trimmed from '{originalEmail}' to '{model.CustomerEmail}'");
                        // Clear existing validation errors for email since we've cleaned it
                        ModelState.Remove(nameof(model.CustomerEmail));
                    }
                    
                    // Simple email validation check
                    var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                    var isEmailValid = emailRegex.IsMatch(model.CustomerEmail);
                    Console.WriteLine($"  - Email validation result: {isEmailValid} for '{model.CustomerEmail}'");
                    
                    // If email is now valid after trimming, remove validation error
                    if (isEmailValid && ModelState.ContainsKey(nameof(model.CustomerEmail)))
                    {
                        ModelState.Remove(nameof(model.CustomerEmail));
                        Console.WriteLine($"  - Removed email validation error after successful cleanup");
                    }
                }
                
                // Trim other fields as well
                var fieldsChanged = false;
                if (!string.IsNullOrEmpty(model.CustomerFullName))
                {
                    var original = model.CustomerFullName;
                    model.CustomerFullName = model.CustomerFullName.Trim();
                    if (original != model.CustomerFullName) { fieldsChanged = true; ModelState.Remove(nameof(model.CustomerFullName)); }
                }
                if (!string.IsNullOrEmpty(model.CustomerPhoneNumber))
                {
                    var original = model.CustomerPhoneNumber;
                    model.CustomerPhoneNumber = model.CustomerPhoneNumber.Trim();
                    if (original != model.CustomerPhoneNumber) { fieldsChanged = true; ModelState.Remove(nameof(model.CustomerPhoneNumber)); }
                }
                if (!string.IsNullOrEmpty(model.DeliveryAddress))
                {
                    var original = model.DeliveryAddress;
                    model.DeliveryAddress = model.DeliveryAddress.Trim();
                    if (original != model.DeliveryAddress) { fieldsChanged = true; ModelState.Remove(nameof(model.DeliveryAddress)); }
                }
                
                // If any fields were trimmed, re-validate the entire model
                if (fieldsChanged)
                {
                    Console.WriteLine($"🧹 Fields were trimmed, clearing ModelState and re-validating");
                    ModelState.Clear();
                    TryValidateModel(model);
                    Console.WriteLine($"🧹 After cleanup validation - ModelState valid: {ModelState.IsValid}");
                }
                
                // Handle address priority logic: Manual address takes precedence over selected address
                if (!string.IsNullOrWhiteSpace(model.DeliveryAddress) && model.UserAddressID.HasValue)
                {
                    Console.WriteLine("⚠️ Both UserAddressID and DeliveryAddress provided - using manual address");
                    model.UserAddressID = null; // Clear selected address to prioritize manual entry
                }
                
                // If UserAddressID is provided, override customer info with address details
                if (model.UserAddressID.HasValue)
                {
                    var selectedUserAddress = await _context.UserAddresses
                        .FirstOrDefaultAsync(ua => ua.AddressID == model.UserAddressID.Value);
                    if (selectedUserAddress != null)
                    {
                        // Override customer information with selected address details
                        var originalCustomerInfo = $"{model.CustomerFullName} / {model.CustomerPhoneNumber}";
                        model.CustomerFullName = selectedUserAddress.FullName;
                        model.CustomerPhoneNumber = selectedUserAddress.PhoneNumber;
                        
                        Console.WriteLine($"🔄 Customer info overridden by selected address:");
                        Console.WriteLine($"   Original: {originalCustomerInfo}");
                        Console.WriteLine($"   New: {model.CustomerFullName} / {model.CustomerPhoneNumber}");
                        Console.WriteLine($"   Address: {selectedUserAddress.Address}");
                    }
                }
                
                // FORCE FIX: Clear ALL email validation errors completely
                var emailKeys = ModelState.Keys.Where(k => k.Contains("Email") || k.Contains("CustomerEmail")).ToList();
                foreach (var key in emailKeys)
                {
                    ModelState.Remove(key);
                    Console.WriteLine($"🔧 FORCE CLEARED validation error for key: {key}");
                }
                
                // Additional fix: If email is provided, consider it valid
                if (!string.IsNullOrEmpty(model.CustomerEmail?.Trim()))
                {
                    Console.WriteLine($"🔧 Email '{model.CustomerEmail}' is present - marking as valid");
                }
                
                // MANUAL DATE PARSING FIX: Try to fix PickupDate if it seems wrong
                if (model.PickupDate.HasValue && Request.Form.ContainsKey("PickupDate"))
                {
                    var rawDateValue = Request.Form["PickupDate"].ToString();
                    Console.WriteLine($"🔧 Manual date fix check:");
                    Console.WriteLine($"    Raw form value: '{rawDateValue}'");
                    Console.WriteLine($"    Bound model value: {model.PickupDate.Value}");
                    Console.WriteLine($"    Model display: {model.PickupDate.Value.ToString("dd/MM/yyyy")}");
                    
                    // Try to parse the raw value as intended date
                    if (DateTime.TryParseExact(rawDateValue, "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime fixedDate))
                    {
                        if (fixedDate.Date != model.PickupDate.Value.Date)
                        {
                            Console.WriteLine($"🔧 Date mismatch detected! Fixing:");
                            Console.WriteLine($"    Original: {model.PickupDate.Value.ToString("dd/MM/yyyy")}");
                            Console.WriteLine($"    Fixed to: {fixedDate.ToString("dd/MM/yyyy")}");
                            model.PickupDate = fixedDate;
                        }
                    }
                }
                
                // Check minimum order value before proceeding
                const decimal MIN_ORDER_VALUE = 60000m;
                if (model.SubtotalAmount < MIN_ORDER_VALUE)
                {
                    var requiredAmount = MIN_ORDER_VALUE - model.SubtotalAmount;
                    TempData["ErrorMessage"] = $"Đơn hàng tối thiểu {MIN_ORDER_VALUE:N0}₫ để có thể đặt hàng. Vui lòng thêm {requiredAmount:N0}₫ nữa.";
                    return RedirectToAction("Shipping");
                }

                // FORCE BYPASS: If basic fields are filled, override validation
                bool hasBasicInfo = !string.IsNullOrEmpty(model.CustomerFullName?.Trim()) &&
                                   !string.IsNullOrEmpty(model.CustomerPhoneNumber?.Trim()) &&
                                   !string.IsNullOrEmpty(model.CustomerEmail?.Trim()) &&
                                   model.DeliveryMethodID > 0 &&
                                   (!string.IsNullOrEmpty(model.DeliveryAddress?.Trim()) || model.UserAddressID.HasValue);
                
                if (hasBasicInfo)
                {
                    Console.WriteLine($"🚀 FORCE BYPASS: All basic fields present - proceeding to checkout");
                    // Store checkout data in session for the next step
                    HttpContext.Session.SetString("CheckoutData", Newtonsoft.Json.JsonConvert.SerializeObject(model));
                    // Redirect to checkout page
                    return RedirectToAction("Checkout");
                }
                
                // Debug: Log validation errors
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("❌ ProcessShipping ModelState validation failed:");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Any())
                        {
                            Console.WriteLine($"  - {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                        }
                    }

                    // ALWAYS reload all required data on validation failure
                    Console.WriteLine("🔄 Reloading all data for validation error...");
                    
                    // Reload dropdown data
                    model.DeliveryMethods = await _context.DeliveryMethods
                        .Where(dm => dm.IsActive)
                        .ToListAsync();
                    
                    model.Stores = await _context.Stores
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.StoreName)
                        .ToListAsync();

                    // Always check and reload user info
                    var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                    var userIdFromSession = HttpContext.Session.GetString("UserId");
                    
                    Console.WriteLine($"🔍 Session check - IsLoggedIn: {isUserLoggedIn}, UserID: {userIdFromSession}");
                    
                    if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int userId))
                    {
                        // Always reload user addresses
                        model.UserAddresses = await _context.UserAddresses
                            .Where(ua => ua.UserID == userId)
                            .OrderByDescending(ua => ua.IsDefault)
                            .ToListAsync();

                        // Always reload user info regardless of current state
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                        if (user != null)
                        {
                            Console.WriteLine($"🔄 Reloading user info: {user.FullName}, {user.Email}");
                            model.CurrentUser = user;
                            model.IsUserLoggedIn = true;
                            
                            // ALWAYS overwrite with correct user data on validation failure
                            model.CustomerFullName = user.FullName;
                            model.CustomerEmail = user.Email;
                            model.CustomerPhoneNumber = user.PhoneNumber ?? "";
                            
                            Console.WriteLine($"✅ User data restored: '{model.CustomerFullName}', '{model.CustomerEmail}', '{model.CustomerPhoneNumber}'");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ User not logged in or session data missing");
                        model.IsUserLoggedIn = false;
                        model.UserAddresses = new List<UserAddress>();
                    }

                    // Always reload cart items
                    var cart = await GetOrCreateCartAsync();
                    var cartItems = await _context.CartItems
                        .Where(ci => ci.CartID == cart.CartID)
                        .Include(ci => ci.Product)
                        .ToListAsync();

                    if (cartItems.Any())
                    {
                        model.CartItems = await MapCartItemsToViewModelAsync(cartItems);
                        model.SubtotalAmount = model.CartItems.Sum(ci => ci.TotalPrice);
                        model.DiscountAmount = 0;

                        // Recalculate shipping fee
                        var userIdForShipping = isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int shippingUserId) ? shippingUserId : (int?)null;
                        var shippingCalculation = await _shippingService.CalculateShippingFeeAsync(userIdForShipping, model.SubtotalAmount, model.DeliveryMethodID);
                        model.ShippingFee = shippingCalculation.ShippingFee;
                        model.IsFreeship = shippingCalculation.IsFreeship;
                        model.FreeshipeMessage = shippingCalculation.Message;

                        // Update freeship info
                        var freeshipeInfo = await _shippingService.GetFreeshipePromotionInfoAsync(userIdForShipping, model.SubtotalAmount);
                        model.IsFirstOrder = freeshipeInfo.IsFirstOrder;
                        model.RequiredAmountForFreeship = freeshipeInfo.RequiredAmountForFreeship;

                        model.TotalAmount = model.SubtotalAmount + model.ShippingFee - model.DiscountAmount;
                    }

                    Console.WriteLine($"🔄 Reloaded data summary:");
                    Console.WriteLine($"  - Cart items: {model.CartItems.Count}");
                    Console.WriteLine($"  - Delivery methods: {model.DeliveryMethods.Count}");
                    Console.WriteLine($"  - User addresses: {model.UserAddresses.Count}");
                    Console.WriteLine($"  - User logged in: {model.IsUserLoggedIn}");

                    // Force re-validation to clear any outdated errors
                    TryValidateModel(model);
                    Console.WriteLine($"🔄 Re-validation completed. ModelState valid: {ModelState.IsValid}");

                    return View("Shipping", model);
                }

                // Store checkout data in session for the next step
                HttpContext.Session.SetString("CheckoutData", Newtonsoft.Json.JsonConvert.SerializeObject(model));

                // Redirect to checkout page
                return RedirectToAction("Checkout");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProcessShipping: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thông tin thanh toán.";
                return RedirectToAction("Shipping");
            }
        }

        // GET: /Cart/Checkout - Final payment page
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                // Get checkout data from session
                var checkoutDataJson = HttpContext.Session.GetString("CheckoutData");
                Console.WriteLine($"🔍 Checkout() - CheckoutData from session: {(checkoutDataJson?.Length > 200 ? checkoutDataJson.Substring(0, 200) + "..." : checkoutDataJson ?? "null")}");
                
                if (string.IsNullOrEmpty(checkoutDataJson))
                {
                    Console.WriteLine($"❌ No CheckoutData in session, redirecting to Shipping");
                    TempData["ErrorMessage"] = "Thông tin đơn hàng không hợp lệ. Vui lòng thử lại.";
                    return RedirectToAction("Shipping");
                }

                var shippingData = JsonConvert.DeserializeObject<CheckoutShippingViewModel>(checkoutDataJson);
                if (shippingData == null)
                {
                    Console.WriteLine($"❌ Failed to deserialize CheckoutData, redirecting to Shipping");
                    return RedirectToAction("Shipping");
                }
                
                Console.WriteLine($"🔍 Shipping data loaded:");
                Console.WriteLine($"   - UserAddressID: {shippingData.UserAddressID}");
                Console.WriteLine($"   - DeliveryAddress: '{shippingData.DeliveryAddress}'");
                Console.WriteLine($"   - DeliveryMethodID: {shippingData.DeliveryMethodID}");
                Console.WriteLine($"   - StoreID: {shippingData.StoreID}");
                Console.WriteLine($"   - PickupDate: {shippingData.PickupDate}");
                if (shippingData.PickupDate.HasValue)
                {
                    Console.WriteLine($"     📅 Shipping PickupDate Details:");
                    Console.WriteLine($"     📅 Raw value: {shippingData.PickupDate.Value}");
                    Console.WriteLine($"     📅 Kind: {shippingData.PickupDate.Value.Kind}");
                    Console.WriteLine($"     📅 Display format: {shippingData.PickupDate.Value.ToString("dd/MM/yyyy")}");
                }
                Console.WriteLine($"   - PickupTimeSlot: {shippingData.PickupTimeSlot}");
                Console.WriteLine($"   - Customer: {shippingData.CustomerFullName} / {shippingData.CustomerPhoneNumber}");

                // Create checkout view model
                var viewModel = new CheckoutViewModel
                {
                    CustomerFullName = shippingData.CustomerFullName,
                    CustomerEmail = shippingData.CustomerEmail,
                    CustomerPhoneNumber = shippingData.CustomerPhoneNumber,
                    DeliveryAddress = shippingData.DeliveryAddress,
                    UserAddressID = shippingData.UserAddressID,
                    DeliveryMethodID = shippingData.DeliveryMethodID,
                    StoreID = shippingData.StoreID,
                    PickupDate = shippingData.PickupDate,
                    PickupTimeSlot = shippingData.PickupTimeSlot,
                    NotesByCustomer = shippingData.NotesByCustomer,
                    SubtotalAmount = shippingData.SubtotalAmount,
                    ShippingFee = shippingData.ShippingFee,
                    DiscountAmount = shippingData.DiscountAmount,
                    TotalAmount = shippingData.TotalAmount
                };

                // RELOAD cart items from database to ensure full configuration data
                var cart = await GetOrCreateCartAsync();
                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .Include(ci => ci.Product)
                    .ToListAsync();

                if (cartItems.Any())
                {
                    viewModel.CartItems = await MapCartItemsToViewModelAsync(cartItems);
                    Console.WriteLine($"🔍 Reloaded {viewModel.CartItems.Count} cart items with full configuration data");
                    
                    // Check minimum order value
                    const decimal MIN_ORDER_VALUE = 60000m;
                    var currentSubtotal = viewModel.CartItems.Sum(ci => ci.TotalPrice);
                    if (currentSubtotal < MIN_ORDER_VALUE)
                    {
                        var requiredAmount = MIN_ORDER_VALUE - currentSubtotal;
                        Console.WriteLine($"❌ Order below minimum: {currentSubtotal:N0}₫ < {MIN_ORDER_VALUE:N0}₫");
                        TempData["ErrorMessage"] = $"Đơn hàng tối thiểu {MIN_ORDER_VALUE:N0}₫ để có thể đặt hàng. Vui lòng thêm {requiredAmount:N0}₫ nữa.";
                        return RedirectToAction("Index", "Home");
                    }
                    
                    // Debug: Log configuration data for each item
                    foreach (var item in viewModel.CartItems)
                    {
                        Console.WriteLine($"🔍 Item: {item.ProductName}, ConfigOptions: {item.ConfigurationOptions?.Count ?? 0}");
                        if (item.ConfigurationOptions != null)
                        {
                            foreach (var config in item.ConfigurationOptions)
                            {
                                Console.WriteLine($"    - {config.GroupName}: {config.OptionName} ({config.VariantName})");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"❌ No cart items found, redirecting to home");
                    TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Home");
                }

                // Get full address information and customer details
                if (viewModel.UserAddressID.HasValue)
                {
                    var userAddress = await _context.UserAddresses
                        .FirstOrDefaultAsync(ua => ua.AddressID == viewModel.UserAddressID.Value);
                    if (userAddress != null)
                    {
                        // Override customer information with selected address details
                        viewModel.CustomerFullName = userAddress.FullName;
                        viewModel.CustomerPhoneNumber = userAddress.PhoneNumber;
                        viewModel.FullDeliveryAddress = userAddress.Address;
                        
                        Console.WriteLine($"🏠 Loaded user address from AddressID {viewModel.UserAddressID}:");
                        Console.WriteLine($"   - Customer: {viewModel.CustomerFullName}");
                        Console.WriteLine($"   - Phone: {viewModel.CustomerPhoneNumber}"); 
                        Console.WriteLine($"   - Address: {viewModel.FullDeliveryAddress}");
                    }
                }
                else if (!string.IsNullOrEmpty(viewModel.DeliveryAddress))
                {
                    viewModel.FullDeliveryAddress = viewModel.DeliveryAddress;
                    Console.WriteLine($"🏠 Using manual address: {viewModel.FullDeliveryAddress}");
                    Console.WriteLine($"🏠 Customer info: {viewModel.CustomerFullName} - {viewModel.CustomerPhoneNumber}");
                }
                
                Console.WriteLine($"🔍 Final viewModel summary:");
                Console.WriteLine($"   - Customer: {viewModel.CustomerFullName} / {viewModel.CustomerPhoneNumber}");
                Console.WriteLine($"   - UserAddressID: {viewModel.UserAddressID}");
                Console.WriteLine($"   - FullDeliveryAddress: '{viewModel.FullDeliveryAddress}'");
                Console.WriteLine($"   - DeliveryMethodID: {viewModel.DeliveryMethodID} ({viewModel.DeliveryMethodName})");

                // Get delivery method details
                var deliveryMethod = await _context.DeliveryMethods
                    .FirstOrDefaultAsync(dm => dm.DeliveryMethodID == shippingData.DeliveryMethodID);
                if (deliveryMethod != null)
                {
                    viewModel.DeliveryMethodName = deliveryMethod.MethodName;
                    Console.WriteLine($"🔍 Delivery method loaded: ID={deliveryMethod.DeliveryMethodID}, Name='{deliveryMethod.MethodName}'");
                }
                else
                {
                    Console.WriteLine($"❌ Delivery method not found for ID: {shippingData.DeliveryMethodID}");
                }

                // Get store details if pickup
                if (shippingData.StoreID.HasValue)
                {
                    var store = await _context.Stores
                        .FirstOrDefaultAsync(s => s.StoreID == shippingData.StoreID.Value);
                    if (store != null)
                    {
                        viewModel.StoreName = store.StoreName;
                        viewModel.StoreAddress = $"{store.StreetAddress}, {store.District}, {store.City}";
                        Console.WriteLine($"🏪 Loaded store info: {viewModel.StoreName} - {viewModel.StoreAddress}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Store not found for ID: {shippingData.StoreID.Value}");
                    }
                }

                // Get user information
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                
                if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int userId))
                {
                    viewModel.UserID = userId;
                    viewModel.IsUserLoggedIn = true;

                    // Get available vouchers for user
                    viewModel.AvailableVouchers = await GetAvailableVouchersForUserAsync(userId, viewModel.SubtotalAmount);
                }

                // Load payment methods
                viewModel.PaymentMethods = await _context.PaymentMethods
                    .Where(pm => pm.IsActive)
                    .ToListAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Checkout: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thanh toán.";
                return RedirectToAction("Shipping");
            }
        }

        // POST: /Cart/ProcessCheckout - Complete the order
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload necessary data for view
                    model.PaymentMethods = await _context.PaymentMethods
                        .Where(pm => pm.IsActive)
                        .ToListAsync();

                    if (model.IsUserLoggedIn && model.UserID.HasValue)
                    {
                        model.AvailableVouchers = await GetAvailableVouchersForUserAsync(model.UserID.Value, model.SubtotalAmount);
                    }

                    return View("Checkout", model);
                }

                // Process payment and create order
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    Console.WriteLine($"🛒 ProcessCheckout - Creating order for customer: {model.CustomerFullName}");
                    
                    // Get current cart
                    var cart = await GetOrCreateCartAsync();
                    var cartItems = await _context.CartItems
                        .Where(ci => ci.CartID == cart.CartID)
                        .Include(ci => ci.Product)
                        .ToListAsync();

                    if (!cartItems.Any())
                    {
                        throw new InvalidOperationException("Giỏ hàng trống, không thể tạo đơn hàng");
                    }

                    // Final check minimum order value
                    const decimal MIN_ORDER_VALUE = 60000m;
                    if (model.SubtotalAmount < MIN_ORDER_VALUE)
                    {
                        throw new InvalidOperationException($"Đơn hàng tối thiểu {MIN_ORDER_VALUE:N0}₫");
                    }

                    // Generate unique order code
                    var orderCode = await GenerateOrderCodeAsync();
                    Console.WriteLine($"🛒 Generated order code: {orderCode}");

                    // Parse pickup time slot
                    TimeSpan? pickupTimeSlot = model.PickupTimeSlot;

                    // Calculate final shipping fee
                    var finalShippingCalculation = await _shippingService.CalculateShippingFeeAsync(model.UserID, model.SubtotalAmount, model.DeliveryMethodID);
                    model.ShippingFee = finalShippingCalculation.ShippingFee;
                    model.TotalAmount = model.SubtotalAmount + model.ShippingFee - model.DiscountAmount;

                    Console.WriteLine($"🚚 Final shipping calculation for order:");
                    Console.WriteLine($"  - UserID: {model.UserID}");
                    Console.WriteLine($"  - Subtotal: {model.SubtotalAmount:N0}₫");
                    Console.WriteLine($"  - Shipping: {model.ShippingFee:N0}₫");
                    Console.WriteLine($"  - Total: {model.TotalAmount:N0}₫");
                    Console.WriteLine($"  - Message: {finalShippingCalculation.Message}");

                    // Create order
                    var order = new Orders
                    {
                        OrderCode = orderCode,
                        UserID = model.UserID,
                        CustomerFullName = model.CustomerFullName,
                        CustomerEmail = model.CustomerEmail,
                        CustomerPhoneNumber = model.CustomerPhoneNumber,
                        UserAddressID = model.UserAddressID,
                        DeliveryMethodID = model.DeliveryMethodID,
                        StoreID = model.StoreID,
                        PickupDate = model.PickupDate,
                        PickupTimeSlot = pickupTimeSlot,
                        OrderDate = GetVietnamLocalTime(),
                        SubtotalAmount = model.SubtotalAmount,
                        ShippingFee = model.ShippingFee,
                        DiscountAmount = model.DiscountAmount,
                        TotalAmount = model.TotalAmount,
                        OrderStatusID = 1, // Chờ xác nhận
                        PaymentMethodID = model.PaymentMethodID,
                        PromotionID = model.AppliedPromotionID,
                        NotesByCustomer = model.NotesByCustomer
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"🛒 Order created with ID: {order.OrderID}");

                    // Ghi lại lịch sử trạng thái đầu tiên cho đơn hàng mới
                    await _statusHistoryService.LogStatusChangeAsync(
                        orderId: order.OrderID,
                        statusId: 1, // "Chờ xác nhận"
                        updatedBy: "System",
                        note: "Đơn hàng được tạo"
                    );

                    // Create order items from cart items
                    foreach (var cartItem in cartItems)
                    {
                        var orderItem = new OrderItems
                        {
                            OrderID = order.OrderID,
                            ProductID = cartItem.ProductID,
                            ProductNameSnapshot = cartItem.Product.ProductName,
                            Quantity = cartItem.Quantity,
                            UnitPrice = cartItem.UnitPrice,
                            Subtotal = cartItem.UnitPrice * cartItem.Quantity,
                            SelectedConfigurationSnapshot = cartItem.SelectedConfigurationSnapshot
                        };

                        _context.OrderItems.Add(orderItem);
                        Console.WriteLine($"🛒 Added order item: {orderItem.ProductNameSnapshot} x{orderItem.Quantity}");
                    }

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"🛒 Order items saved successfully");

                    // Mark promotion as used if applicable
                    if (model.AppliedPromotionID.HasValue && model.UserID.HasValue)
                    {
                        var userPromotion = new UserPromotion
                        {
                            UserID = model.UserID.Value,
                            PromotionID = model.AppliedPromotionID.Value,
                            OrderID = order.OrderID,
                            UsedDate = DateTime.Now
                        };
                        _context.UserPromotions.Add(userPromotion);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"🛒 Promotion {model.AppliedPromotionID} marked as used");
                    }

                    // Clear cart after successful order creation
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"🛒 Cart cleared after successful order");

                    // Commit transaction
                    await transaction.CommitAsync();
                    Console.WriteLine($"✅ Order {orderCode} created successfully!");

                    // Store order ID in session for success page
                    HttpContext.Session.SetInt32("LastOrderID", order.OrderID);
                    
                    TempData["SuccessMessage"] = $"Đơn hàng {orderCode} đã được đặt thành công!";
                    return RedirectToAction("OrderSuccess", new { orderId = order.OrderID });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ Error creating order: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProcessCheckout: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý đơn hàng.";
                return View("Checkout", model);
            }
        }

        // API: Apply voucher
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherRequest request)
        {
            try
            {
                var result = await _promotionService.ValidatePromotionForUserAsync(
                    request.UserID ?? 0, request.VoucherCode, request.OrderAmount);

                if (result.IsValid && result.Promotion != null)
                {
                    var response = new ApplyVoucherResponse
                    {
                        Success = true,
                        Message = "Voucher được áp dụng thành công",
                        PromotionID = result.Promotion.PromotionID,
                        PromotionName = result.Promotion.PromotionName,
                        DiscountAmount = result.DiscountAmount,
                        NewTotalAmount = request.OrderAmount - result.DiscountAmount
                    };

                    return Json(response);
                }
                else
                {
                    return Json(new ApplyVoucherResponse
                    {
                        Success = false,
                        Message = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error applying voucher: {ex.Message}");
                return Json(new ApplyVoucherResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi áp dụng voucher"
                });
            }
        }

        // GET: Order success page
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int? orderId)
        {
            try
            {
                // Get order ID from parameter or session
                int orderIdToLoad = orderId ?? HttpContext.Session.GetInt32("LastOrderID") ?? 0;
                
                if (orderIdToLoad == 0)
                {
                    Console.WriteLine($"❌ No order ID found for success page");
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // Load order details
                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.Store)
                    .Include(o => o.UserAddress)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == orderIdToLoad);

                if (order == null)
                {
                    Console.WriteLine($"❌ Order {orderIdToLoad} not found");
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // Map order items to view model
                var orderItems = new List<UserOrderItemViewModel>();
                foreach (var item in order.OrderItems)
                {
                    var orderItemViewModel = new UserOrderItemViewModel
                    {
                        OrderItemID = item.OrderItemID,
                        ProductID = item.ProductID,
                        ProductName = item.ProductNameSnapshot,
                        ProductImage = item.Product?.ImageUrl,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        ConfigurationOptions = new List<OrderItemConfigurationViewModel>()
                    };

                    // Parse configuration if exists
                    if (!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot))
                    {
                        try
                        {
                            var configData = JsonConvert.DeserializeObject<List<dynamic>>(item.SelectedConfigurationSnapshot);
                            if (configData != null)
                            {
                                orderItemViewModel.ConfigurationOptions = configData.Select(config => new OrderItemConfigurationViewModel
                                {
                                    GroupName = (string)config.GroupName,
                                    OptionName = (string)config.OptionProductName,
                                    OptionImage = (string?)config.OptionProductImage,
                                    Quantity = (int)config.Quantity,
                                    PriceAdjustment = (decimal)config.PriceAdjustment,
                                    VariantName = (string?)config.VariantName,
                                    VariantType = (string?)config.VariantType
                                }).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing order item configuration: {ex.Message}");
                        }
                    }

                    orderItems.Add(orderItemViewModel);
                }

                // Calculate estimated delivery time
                var estimatedDeliveryTime = "";
                var isDelivery = order.DeliveryMethod?.MethodName?.Contains("giao hàng") == true || 
                                order.DeliveryMethod?.MethodName?.Contains("ship") == true;
                var isPickup = !isDelivery;

                if (isPickup && order.PickupDate.HasValue)
                {
                    estimatedDeliveryTime = $"Nhận hàng vào {order.PickupDate.Value:dd/MM/yyyy}";
                    if (order.PickupTimeSlot.HasValue)
                    {
                        estimatedDeliveryTime += $" lúc {order.PickupTimeSlot.Value:hh\\:mm}";
                    }
                }
                else if (isDelivery)
                {
                    var estimatedTime = order.OrderDate.AddMinutes(45); // 45 minutes for delivery
                    estimatedDeliveryTime = $"Giao hàng trong khoảng {estimatedTime:HH:mm} - {estimatedTime.AddMinutes(15):HH:mm}";
                }

                var viewModel = new OrderSuccessViewModel
                {
                    Order = order,
                    OrderItems = orderItems,
                    EstimatedDeliveryTime = estimatedDeliveryTime,
                    IsDelivery = isDelivery,
                    IsPickup = isPickup
                };

                Console.WriteLine($"✅ Order success page loaded for order {order.OrderCode}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading order success page: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<List<CheckoutCartItemViewModel>> MapCartItemsToViewModelAsync(List<CartItem> cartItems)
        {
            var result = new List<CheckoutCartItemViewModel>();

            foreach (var item in cartItems)
            {
                var cartItemViewModel = new CheckoutCartItemViewModel
                {
                    CartItemID = item.CartItemID,
                    ProductID = item.ProductID,
                    ProductName = item.Product.ProductName,
                    ProductImage = item.Product.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                // Parse configuration if exists
                if (!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot))
                {
                    try
                    {
                        var configData = JsonConvert.DeserializeObject<List<dynamic>>(item.SelectedConfigurationSnapshot);
                        if (configData != null && configData.Any())
                        {
                            cartItemViewModel.ConfigurationOptions = configData.Select(config => new ConfigurationOptionDisplay
                            {
                                GroupName = (string)config.GroupName,
                                OptionName = (string)config.OptionProductName,
                                OptionImage = (string?)config.OptionProductImage,
                                PriceAdjustment = (decimal)config.PriceAdjustment,
                                Quantity = (int)config.Quantity,
                                VariantName = (string?)config.VariantName
                            }).ToList();

                            cartItemViewModel.ConfigurationDescription = string.Join(", ", 
                                cartItemViewModel.ConfigurationOptions.Select(co => co.OptionName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing configuration: {ex.Message}");
                    }
                }

                result.Add(cartItemViewModel);
            }

            return result;
        }

        // Helper method to get available vouchers for user
        private async Task<List<AvailableVoucherViewModel>> GetAvailableVouchersForUserAsync(int userId, decimal orderAmount)
        {
            try
            {
                var availablePromotions = await _promotionService.GetAvailablePromotionsForUserAsync(userId);
                var voucherViewModels = new List<AvailableVoucherViewModel>();

                foreach (var promotion in availablePromotions)
                {
                    var canApply = true;
                    var cannotApplyReason = "";
                    var estimatedDiscount = 0m;

                    // Check if order meets minimum value
                    if (promotion.MinOrderValue.HasValue && orderAmount < promotion.MinOrderValue.Value)
                    {
                        canApply = false;
                        cannotApplyReason = $"Đơn hàng tối thiểu {promotion.MinOrderValue.Value:N0} đ";
                    }
                    else if (promotion.IsActive && DateTime.Now >= promotion.StartDate && DateTime.Now <= promotion.EndDate)
                    {
                        estimatedDiscount = _promotionService.CalculateDiscountAmount(promotion, orderAmount);
                    }
                    else
                    {
                        canApply = false;
                        cannotApplyReason = "Voucher đã hết hạn hoặc chưa có hiệu lực";
                    }

                    voucherViewModels.Add(new AvailableVoucherViewModel
                    {
                        PromotionID = promotion.PromotionID,
                        PromotionName = promotion.PromotionName,
                        Description = promotion.Description,
                        CouponCode = promotion.CouponCode ?? "",
                        DiscountType = promotion.DiscountType,
                        DiscountValue = promotion.DiscountValue,
                        MinOrderValue = promotion.MinOrderValue,
                        EndDate = promotion.EndDate,
                        CanApply = canApply,
                        CannotApplyReason = cannotApplyReason,
                        EstimatedDiscount = estimatedDiscount
                    });
                }

                return voucherViewModels.OrderByDescending(v => v.CanApply).ThenByDescending(v => v.EstimatedDiscount).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting available vouchers: {ex.Message}");
                return new List<AvailableVoucherViewModel>();
            }
        }

        // GET: /Cart/CheckSession - Quick session check for debugging
        [HttpGet]
        public IActionResult CheckSession()
        {
            try
            {
                var sessionInfo = new
                {
                    SessionId = HttpContext.Session.Id,
                    IsUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true",
                    UserId = HttpContext.Session.GetString("UserId"),
                    SessionKeys = HttpContext.Session.Keys.ToArray(),
                    Timestamp = DateTime.Now
                };

                Console.WriteLine($"🔍 Session Check Result:");
                Console.WriteLine($"  - Session ID: {sessionInfo.SessionId}");
                Console.WriteLine($"  - Is Logged In: {sessionInfo.IsUserLoggedIn}");
                Console.WriteLine($"  - User ID: {sessionInfo.UserId}");
                Console.WriteLine($"  - Session Keys: [{string.Join(", ", sessionInfo.SessionKeys)}]");

                return Json(sessionInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error checking session: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // Helper method to get Vietnam local time
        private DateTime GetVietnamLocalTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        // API: Calculate shipping fee real-time
        [HttpPost]
        public async Task<IActionResult> CalculateShipping([FromBody] CalculateShippingRequest request)
        {
            try
            {
                // Get user info
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                var userId = isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int uid) ? uid : (int?)null;

                // Check minimum order value
                const decimal MIN_ORDER_VALUE = 60000m;
                if (request.OrderAmount < MIN_ORDER_VALUE)
                {
                    return Json(new CalculateShippingResponse
                    {
                        Success = false,
                        CanCheckout = false,
                        ShippingFee = 0,
                        TotalAmount = request.OrderAmount,
                        Message = $"Đơn hàng tối thiểu {MIN_ORDER_VALUE:N0}₫ để có thể đặt hàng",
                        RequiredAmount = MIN_ORDER_VALUE - request.OrderAmount,
                        IsMinimumOrder = false
                    });
                }

                // Calculate shipping fee
                var shippingResult = await _shippingService.CalculateShippingFeeAsync(userId, request.OrderAmount, request.DeliveryMethodId);
                var freeshipeInfo = await _shippingService.GetFreeshipePromotionInfoAsync(userId, request.OrderAmount);

                return Json(new CalculateShippingResponse
                {
                    Success = true,
                    CanCheckout = true,
                    ShippingFee = shippingResult.ShippingFee,
                    TotalAmount = request.OrderAmount + shippingResult.ShippingFee,
                    Message = shippingResult.Message,
                    IsFreeship = shippingResult.IsFreeship,
                    FreeshipeMessage = freeshipeInfo.Message,
                    IsFirstOrder = freeshipeInfo.IsFirstOrder,
                    RequiredAmount = 0,
                    IsMinimumOrder = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating shipping: {ex.Message}");
                return Json(new CalculateShippingResponse
                {
                    Success = false,
                    CanCheckout = false,
                    Message = "Có lỗi xảy ra khi tính phí giao hàng"
                });
            }
        }

        // Helper method to generate unique order code
        private async Task<string> GenerateOrderCodeAsync()
        {
            var today = DateTime.Now;
            var datePrefix = today.ToString("yyMMdd"); // Format: 241210 (for 2024-12-10)
            
            // Find highest order number for today
            var todayOrderCodes = await _context.Orders
                .Where(o => o.OrderCode.StartsWith(datePrefix))
                .Select(o => o.OrderCode)
                .ToListAsync();

            var maxOrderNumber = 0;
            foreach (var existingOrderCode in todayOrderCodes)
            {
                if (existingOrderCode.Length >= 11 && int.TryParse(existingOrderCode.Substring(6, 5), out int orderNumber))
                {
                    maxOrderNumber = Math.Max(maxOrderNumber, orderNumber);
                }
            }

            var nextOrderNumber = maxOrderNumber + 1;
            var orderCode = $"{datePrefix}{nextOrderNumber:D5}"; // Format: 24121000001

            return orderCode;
        }
    }

    // Request/Response models for shipping calculation
    public class CalculateShippingRequest
    {
        public decimal OrderAmount { get; set; }
        public int DeliveryMethodId { get; set; }
    }

    public class CalculateShippingResponse
    {
        public bool Success { get; set; }
        public bool CanCheckout { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Message { get; set; } = "";
        public bool IsFreeship { get; set; }
        public string FreeshipeMessage { get; set; } = "";
        public bool IsFirstOrder { get; set; }
        public decimal RequiredAmount { get; set; }
        public bool IsMinimumOrder { get; set; }
    }
}