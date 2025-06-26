using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JollibeeClone.Areas.Admin.Attributes
{
    public class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var isLoggedIn = session.GetString("IsAdminLoggedIn") == "true";
            
            if (!isLoggedIn)
            {
                // Chuyển hướng đến trang đăng nhập admin
                context.Result = new RedirectToActionResult("Login", "Auth", new { area = "Admin" });
            }
        }
    }
} 