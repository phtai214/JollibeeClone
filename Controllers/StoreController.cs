using Microsoft.AspNetCore.Mvc;
using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Controllers
{
    public class StoreController : Controller
    {
        private readonly AppDbContext _context;

        public StoreController(AppDbContext context)
        {
            _context = context;
        }

        [Route("Store/Index")]
        public IActionResult Index(string city = "", string district = "", string ward = "")
        {
            // Truyền search parameters từ home page qua view
            ViewBag.InitialCity = city;
            ViewBag.InitialDistrict = district;
            ViewBag.InitialWard = ward;
            return View();
        }

        // Debug API to check stores data
        [HttpGet]
        [Route("api/stores/debug")]
        public async Task<IActionResult> GetStoresDebug()
        {
            try
            {
                var totalStores = await _context.Stores.CountAsync();
                var activeStores = await _context.Stores.Where(s => s.IsActive).CountAsync();
                var cities = await _context.Stores.Select(s => s.City).Distinct().ToListAsync();
                var districts = await _context.Stores.Select(s => s.District).Distinct().ToListAsync();
                
                var sampleStores = await _context.Stores.Take(5).Select(s => new
                {
                    s.StoreID,
                    s.StoreName,
                    s.City,
                    s.District,
                    s.IsActive,
                    s.GoogleMapsUrl,
                    HasGoogleMapsUrl = !string.IsNullOrEmpty(s.GoogleMapsUrl)
                }).ToListAsync();

                return Json(new
                {
                    success = true,
                    totalStores = totalStores,
                    activeStores = activeStores,
                    cities = cities,
                    districts = districts,
                    sampleStores = sampleStores
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API endpoint to get cities and districts data
        [HttpGet]
        [Route("api/locations")]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                var cities = await _context.Stores
                    .Where(s => s.IsActive)
                    .Select(s => s.City)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                var districts = await _context.Stores
                    .Where(s => s.IsActive)
                    .GroupBy(s => s.City)
                    .ToDictionaryAsync(
                        g => g.Key, 
                        g => g.Select(s => s.District).Distinct().OrderBy(d => d).ToList()
                    );

                var wards = await _context.Stores
                    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.Ward))
                    .GroupBy(s => new { s.City, s.District })
                    .ToDictionaryAsync(
                        g => $"{g.Key.City}|{g.Key.District}",
                        g => g.Select(s => s.Ward).Distinct().OrderBy(w => w).ToList()
                    );

                return Json(new { 
                    success = true, 
                    cities = cities,
                    districts = districts,
                    wards = wards
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu địa điểm: " + ex.Message });
            }
        }

        // API endpoint to get wards by city and district
        [HttpGet]
        [Route("api/wards")]
        public async Task<IActionResult> GetWards(string city, string district)
        {
            try
            {
                if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district))
                {
                    return Json(new { success = false, message = "Vui lòng chọn tỉnh/thành phố và quận/huyện" });
                }

                var wards = await _context.Stores
                    .Where(s => s.IsActive && 
                               s.City == city && 
                               s.District == district && 
                               !string.IsNullOrEmpty(s.Ward))
                    .Select(s => s.Ward)
                    .Distinct()
                    .OrderBy(w => w)
                    .ToListAsync();

                return Json(new { 
                    success = true, 
                    wards = wards,
                    count = wards.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu phường/xã: " + ex.Message });
            }
        }

        // API endpoint to get stores data
        [HttpGet]
        [Route("api/stores")]
        public async Task<IActionResult> GetStores(string search = "", string city = "", string district = "", string ward = "")
        {
            try
            {
                // Log parameters for debugging
                Console.WriteLine($"Store API called with: search='{search}', city='{city}', district='{district}', ward='{ward}'");
                
                var query = _context.Stores.Where(s => s.IsActive);

                // Apply city filter - exact match for better accuracy
                if (!string.IsNullOrEmpty(city))
                {
                    // Use both exact match and contains for flexibility
                    query = query.Where(s => s.City.Equals(city) || s.City.Contains(city));
                    Console.WriteLine($"Applied city filter: {city}");
                }

                // Apply district filter - exact match for better accuracy
                if (!string.IsNullOrEmpty(district))
                {
                    query = query.Where(s => s.District.Equals(district) || s.District.Contains(district));
                    Console.WriteLine($"Applied district filter: {district}");
                }

                // Apply ward filter
                if (!string.IsNullOrEmpty(ward))
                {
                    query = query.Where(s => s.Ward != null && (s.Ward.Equals(ward) || s.Ward.Contains(ward)));
                    Console.WriteLine($"Applied ward filter: {ward}");
                }

                // Apply text search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => 
                        s.StoreName.Contains(search) ||
                        s.StreetAddress.Contains(search) ||
                        s.District.Contains(search) ||
                        s.City.Contains(search) ||
                        (s.Ward != null && s.Ward.Contains(search))
                    );
                    Console.WriteLine($"Applied text search filter: {search}");
                }

                var stores = await query
                    .OrderBy(s => s.StoreName)
                    .Select(s => new
                    {
                        s.StoreID,
                        s.StoreName,
                        s.StreetAddress,
                        s.Ward,
                        s.District,
                        s.City,
                        s.PhoneNumber,
                        s.OpeningHours,
                        s.ImageUrl,
                        s.GoogleMapsUrl,
                        FullAddress = s.StreetAddress + 
                                    (string.IsNullOrEmpty(s.Ward) ? "" : ", " + s.Ward) + 
                                    ", " + s.District + ", " + s.City
                    })
                    .ToListAsync();

                Console.WriteLine($"Found {stores.Count} stores matching criteria");
                
                return Json(new { 
                    success = true, 
                    data = stores,
                    totalCount = stores.Count,
                    searchCriteria = new { search, city, district, ward }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStores API: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi tải dữ liệu cửa hàng: " + ex.Message,
                    error = ex.Message 
                });
            }
        }

        // API to create sample stores with Google Maps URLs for testing
        [HttpPost]
        [Route("api/stores/create-samples")]
        public async Task<IActionResult> CreateSampleStores()
        {
            try
            {
                // Check if stores already exist
                var existingStoresCount = await _context.Stores.CountAsync();
                if (existingStoresCount > 0)
                {
                    return Json(new { 
                        success = true, 
                        message = "Stores already exist", 
                        count = existingStoresCount 
                    });
                }

                var sampleStores = new List<Store>
                {
                    new Store
                    {
                        StoreName = "Jollibee Trường Chinh",
                        StreetAddress = "225 Trường Chinh",
                        Ward = "Phường Tân Thới Nhất",
                        District = "Quận 12",
                        City = "Thành phố Hồ Chí Minh",
                        PhoneNumber = "19001533",
                        OpeningHours = "9:00-10:00",
                        GoogleMapsUrl = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3918.858493285859!2d106.62634587570625!3d10.854496158040435!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x31752b2c5b8ecf83%3A0x1e5b35567ea2fb30!2sJollibee%20Tr%C6%B0%E1%BB%9Dng%20Chinh!5e0!3m2!1sen!2s!4v1642093845123!5m2!1sen!2s",
                        IsActive = true
                    },
                    new Store
                    {
                        StoreName = "Jollibee Đồng Đen",
                        StreetAddress = "279 Đồng Đen",
                        Ward = "Phường 10",
                        District = "Quận Tân Bình",
                        City = "Thành phố Hồ Chí Minh",
                        PhoneNumber = "19001533",
                        OpeningHours = "8:00-22:00",
                        GoogleMapsUrl = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3919.064!2d106.65!3d10.80!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x317529c17c!2sJollibee!5e0!3m2!1sen!2s!4v1642093845124!5m2!1sen!2s",
                        IsActive = true
                    },
                    new Store
                    {
                        StoreName = "Jollibee SC Trần Phú Long Khánh",
                        StreetAddress = "Siêu thị CoopMart Biên Hòa",
                        Ward = "Phường Long Khánh",
                        District = "Thành phố Biên Hòa",
                        City = "Đồng Nai",
                        PhoneNumber = "19001533",
                        OpeningHours = "9:00-21:00",
                        GoogleMapsUrl = "", // This one intentionally empty to test fallback
                        IsActive = true
                    }
                };

                _context.Stores.AddRange(sampleStores);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Sample stores created successfully", 
                    count = sampleStores.Count 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Error creating sample stores: " + ex.Message 
                });
            }
        }

        
    }
} 