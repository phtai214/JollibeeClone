using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JollibeeClone.Areas.Admin.Attributes
{
    public class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var isAdminLoggedIn = session.GetString("IsAdminLoggedIn") == "true";
            var isUserLoggedIn = session.GetString("IsUserLoggedIn") == "true";
            
            // Nếu user thông thường đã đăng nhập thì cũng không được truy cập admin
            if (isUserLoggedIn && !isAdminLoggedIn)
            {
                // Chuyển hướng user thông thường về trang chủ
                context.Result = new RedirectToActionResult("Index", "Home", new { area = "" });
            }
            else if (!isAdminLoggedIn)
            {
                // Chuyển hướng đến trang đăng nhập admin
                context.Result = new RedirectToActionResult("Login", "Auth", new { area = "Admin" });
            }
        }
    }
} 