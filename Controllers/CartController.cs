using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Attributes;
using System.Security.Claims;
using Newtonsoft.Json;

namespace JollibeeClone.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        
        public CartController(AppDbContext context)
        {
            _context = context;
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

        // GET: /Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
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

                var viewModel = new CheckoutShippingViewModel();

                // Get user information if logged in
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                
                if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int userId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                    if (user != null)
                    {
                        viewModel.CurrentUser = user;
                        viewModel.IsUserLoggedIn = true;
                        viewModel.CustomerFullName = user.FullName;
                        viewModel.CustomerEmail = user.Email;
                        viewModel.CustomerPhoneNumber = user.PhoneNumber ?? "";

                        // Get user addresses
                        viewModel.UserAddresses = await _context.UserAddresses
                            .Where(ua => ua.UserID == userId)
                            .OrderByDescending(ua => ua.IsDefault)
                            .ToListAsync();
                    }
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
                viewModel.ShippingFee = 0; // Will be calculated based on delivery method
                viewModel.DiscountAmount = 0; // Will be applied if promotions exist
                viewModel.TotalAmount = viewModel.SubtotalAmount + viewModel.ShippingFee - viewModel.DiscountAmount;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Checkout: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang thanh toán.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Cart/ProcessCheckout
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(CheckoutShippingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload dropdown data
                    model.DeliveryMethods = await _context.DeliveryMethods
                        .Where(dm => dm.IsActive)
                        .ToListAsync();
                    
                    model.Stores = await _context.Stores
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.StoreName)
                        .ToListAsync();

                    var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                    var userIdFromSession = HttpContext.Session.GetString("UserId");
                    
                    if (isUserLoggedIn && !string.IsNullOrEmpty(userIdFromSession) && int.TryParse(userIdFromSession, out int userId))
                    {
                        model.UserAddresses = await _context.UserAddresses
                            .Where(ua => ua.UserID == userId)
                            .OrderByDescending(ua => ua.IsDefault)
                            .ToListAsync();
                    }

                    return View("Checkout", model);
                }

                // Store checkout data in session for the next step
                HttpContext.Session.SetString("CheckoutData", Newtonsoft.Json.JsonConvert.SerializeObject(model));

                // Redirect to payment page (will be implemented next)
                return RedirectToAction("Payment");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProcessCheckout: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thông tin thanh toán.";
                return RedirectToAction("Checkout");
            }
        }

        // GET: /Cart/Payment
        [HttpGet]
        public IActionResult Payment()
        {
            // TODO: Implement payment page
            var checkoutData = HttpContext.Session.GetString("CheckoutData");
            if (string.IsNullOrEmpty(checkoutData))
            {
                return RedirectToAction("Checkout");
            }

            TempData["Message"] = "Trang thanh toán sẽ được triển khai trong bước tiếp theo.";
            return RedirectToAction("Checkout");
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
    }
} 