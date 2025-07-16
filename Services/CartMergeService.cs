using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;

namespace JollibeeClone.Services
{
    public interface ICartMergeService
    {
        Task<bool> MergeAnonymousCartToUserAsync(int userId, string sessionId);
    }

    public class CartMergeService : ICartMergeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartMergeService> _logger;

        public CartMergeService(AppDbContext context, ILogger<CartMergeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> MergeAnonymousCartToUserAsync(int userId, string sessionId)
        {
            try
            {
                _logger.LogInformation("🔄 MergeAnonymousCartToUserAsync - UserID: {userId}, SessionID: {sessionId}", userId, sessionId);
                
                // Find anonymous cart by session ID
                var anonymousCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.SessionID == sessionId && c.UserID == null);
                
                if (anonymousCart == null || !anonymousCart.CartItems.Any())
                {
                    _logger.LogInformation("📭 No anonymous cart found or cart is empty - nothing to merge");
                    return true; // No cart to merge, but not an error
                }
                
                _logger.LogInformation("🛒 Found anonymous cart with {count} items", anonymousCart.CartItems.Count);
                
                // Find existing user cart
                var userCart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == userId);
                
                if (userCart == null)
                {
                    // No existing user cart, just transfer ownership of anonymous cart
                    _logger.LogInformation("🎯 No existing user cart - transferring ownership of anonymous cart");
                    anonymousCart.UserID = userId;
                    anonymousCart.SessionID = null; // Clear session ID since it's now user-owned
                    anonymousCart.LastUpdatedDate = DateTime.Now;
                    
                    _context.Carts.Update(anonymousCart);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("✅ Successfully transferred cart ownership to user {userId}", userId);
                    return true;
                }
                
                // User already has a cart, need to merge items
                _logger.LogInformation("🔀 User already has cart - merging {count} items", anonymousCart.CartItems.Count);
                
                foreach (var anonymousItem in anonymousCart.CartItems)
                {
                    _logger.LogInformation("🔍 Processing item: {productName} x{quantity}", anonymousItem.Product.ProductName, anonymousItem.Quantity);
                    
                    // Check if user cart already has this exact item with same configuration
                    var existingUserItem = userCart.CartItems.FirstOrDefault(ui => 
                        ui.ProductID == anonymousItem.ProductID && 
                        ui.SelectedConfigurationSnapshot == anonymousItem.SelectedConfigurationSnapshot);
                    
                    if (existingUserItem != null)
                    {
                        // Merge quantities
                        _logger.LogInformation("📝 Merging quantities: {existing} + {anonymous}", existingUserItem.Quantity, anonymousItem.Quantity);
                        existingUserItem.Quantity += anonymousItem.Quantity;
                        existingUserItem.UnitPrice = anonymousItem.UnitPrice; // Use latest price
                        _context.CartItems.Update(existingUserItem);
                    }
                    else
                    {
                        // Add new item to user cart
                        _logger.LogInformation("➕ Adding new item to user cart");
                        var newCartItem = new CartItem
                        {
                            CartID = userCart.CartID,
                            ProductID = anonymousItem.ProductID,
                            Quantity = anonymousItem.Quantity,
                            UnitPrice = anonymousItem.UnitPrice,
                            SelectedConfigurationSnapshot = anonymousItem.SelectedConfigurationSnapshot,
                            DateAdded = anonymousItem.DateAdded
                        };
                        _context.CartItems.Add(newCartItem);
                    }
                }
                
                // Update user cart timestamp
                userCart.LastUpdatedDate = DateTime.Now;
                _context.Carts.Update(userCart);
                
                // Remove anonymous cart items and cart
                _context.CartItems.RemoveRange(anonymousCart.CartItems);
                _context.Carts.Remove(anonymousCart);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Successfully merged anonymous cart to user cart");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error merging anonymous cart: {message}", ex.Message);
                return false;
            }
        }
    }
} 