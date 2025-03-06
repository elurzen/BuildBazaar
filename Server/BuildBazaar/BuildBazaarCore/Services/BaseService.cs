using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BuildBazaarCore.Services
{
    public abstract class BaseService
    {
        public virtual JsonResult Json(object? data)
        {
            return new JsonResult(data);
        }

        public virtual uint getUserIDFromToken(JwtSecurityToken token)
        {
            if (token == null)
            {
                return 0;
            }

            var userIdClaim = token.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return 0; // or handle the error differently
            }

            return Convert.ToUInt32(userIdClaim);
        }
    }
}
