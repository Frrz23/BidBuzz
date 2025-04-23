using Microsoft.AspNetCore.Http;
using System.Security.Claims;
namespace BidBuzz.Helper
{
    public static class UserAccessHelper
    {
        public static bool IsBuyer(HttpContext context, ClaimsPrincipal user)
        {
            var userType = context.Session.GetString("_UserRole");
            return userType == "Buyer" || user.IsInRole("Buyer");
        }

        public static bool IsSeller(HttpContext context, ClaimsPrincipal user)
        {
            var userType = context.Session.GetString("_UserRole");
            return userType == "Seller" || user.IsInRole("Seller");
        }

        public static bool IsAdmin(ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }
    }

}
