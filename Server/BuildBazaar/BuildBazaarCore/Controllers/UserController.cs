using BuildBazaarCore.Models;
using BuildBazaarCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using Yarp.ReverseProxy.Forwarder;

namespace BuildBazaarCore.Controllers
{
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IAwsService _awsService;

        public UserController(IUserService userService, IAwsService awsService) : base(userService)
        {
            _userService = userService;
            _awsService = awsService;
        }

        [HttpPost]
        public ActionResult WebValidateToken()
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserModel user)
        {
            return await _userService.CreateUser(user);
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserModel user)
        {
            return await _userService.Login(user);
            
        }

        [HttpPost]
        public async Task<IActionResult> RequestPasswordReset(string email)
        {
            return await _userService.RequestPasswordReset(email, Request.Host.ToString());
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePasswordWithToken(string password, string token)
        {
            return await _userService.UpdatePasswordWithToken(password, token);
        }

    }
}
