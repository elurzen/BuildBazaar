using BuildBazaarCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace BuildBazaarCore.Controllers
{
    public class BaseController : Controller
    {
        private readonly IUserService _userService;

        public BaseController(IUserService userService) {
            _userService = userService;
        }

        protected JwtSecurityToken ValidateToken()
        {
            var token = Request.Headers["Authorization"].ToString();//.Replace("Bearer ", "");
            return _userService.ValidateToken(token);
        }
    }
}
