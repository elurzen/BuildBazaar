using BuildBazaarCore.Models;
using BuildBazaarCore.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace BuildBazaarCore.Controllers
{
    public class BuildController : BaseController
    {
        private readonly IBuildService _buildService;
        public BuildController(IUserService userService, IBuildService buildService) : base(userService)
        {
            _buildService = buildService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateBuild(BuildModel build)
        {
            var validatedToken = ValidateToken();
            if (validatedToken == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            IFormFile file = null;
            string thumbnail = "";

            if (Request.Form.Files.Count > 0)
            {
                file = Request.Form.Files[0];
            }
            if (!string.IsNullOrEmpty(Request.Form["selectedThumbnail"]))
            {
                thumbnail = Request.Form["selectedThumbnail"];
            }

            return await _buildService.CreateBuild(build, file, thumbnail, validatedToken);
        }

        [HttpPost]
        public async Task<IActionResult> EditBuild(BuildModel build)
        {
            var validatedToken = ValidateToken();
            if (validatedToken == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            IFormFile file = null;
            string thumbnail = "";

            if (Request.Form.Files.Count > 0)
            {
                file = Request.Form.Files[0];
            }
            if (!string.IsNullOrEmpty(Request.Form["selectedThumbnail"]))
            {
                thumbnail = Request.Form["selectedThumbnail"];
            }

            return await _buildService.EditBuild(build, file, thumbnail, validatedToken);
        }

        [HttpPost]
        public async Task<IActionResult> CopyBuild(int originalBuildID)
        {
            var validatedToken = ValidateToken();
            if (validatedToken == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            return await _buildService.CopyBuild(originalBuildID, validatedToken);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBuild(int buildID)
        {
            var validatedToken = ValidateToken();
            if (validatedToken == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            return await _buildService.DeleteBuild(buildID, validatedToken);
        }

        [HttpPost]
        public async Task<IActionResult> GetBuilds(bool isPublic)
        {
            var validatedToken = ValidateToken();
            if (validatedToken == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }
            return await _buildService.GetBuilds(validatedToken);
        }

        [HttpPost]
        public async Task<IActionResult> GetPublicBuilds(string userName)
        {
            var validatedToken = ValidateToken();
            return await _buildService.GetPublicBuilds(userName, validatedToken);
        }
    }
}
