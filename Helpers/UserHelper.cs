using IdentityManagementSample.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace IdentityManagementSample.Helpers
{
    public class UserHelper
    {
        public static ClaimsPrincipal Convert(User user)
        {
            var claims = new List<Claim>()
            {
                new Claim("username", user.Name),
            };

            claims.AddRange(user.Claims.Select(x => new Claim(x.Type, x.Value)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return new ClaimsPrincipal(identity);
        }
    }
}
