using Amazon.S3.Model;
using BuildBazaarCore.Models;
using BuildBazaarCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.ComponentModel.Design;
using System.IdentityModel.Tokens.Jwt;

namespace BuildBazaarCore.Controllers
{
    public class BuildDataController : BaseController
    {
        private readonly IAwsService _awsService;
        private readonly INoteService _noteService;
        private readonly IBuildUrlService _buildUrlService;
        private readonly IReferenceImageService _referenceImageService;
        public BuildDataController(
            IUserService userService, 
            IAwsService awsService, 
            INoteService noteservice,
            IBuildUrlService buildUrlService, 
            IReferenceImageService referenceImageService
            ) : base(userService)
        {
            _awsService = awsService;
            _noteService = noteservice;
            _buildUrlService = buildUrlService;
            _referenceImageService = referenceImageService;
        }

        [HttpPost]
        public async Task<IActionResult> GetNote(uint buildID)
        {
            JwtSecurityToken token = ValidateToken();
            return await _noteService.GetNote(buildID, token);
        }

        [HttpPost]
        public async Task<IActionResult> SetNote (int buildID, string noteContent)
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            return await _noteService.SetNote(buildID, noteContent, token);

        }

        [HttpPost]
        public async Task<IActionResult> GetBuildUrls(uint buildID)
        {
            JwtSecurityToken token = ValidateToken();
            return await _buildUrlService.GetBuildUrls(buildID, token);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBuildUrl(uint buildID, string buildUrl, string buildUrlName)
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            return await _buildUrlService.CreateBuildUrl(buildID, buildUrl, buildUrlName, token);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBuildUrl(uint? buildUrlID, uint buildID, string buildUrl, string buildUrlName)
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            return await _buildUrlService.UpdateBuildUrl(buildUrlID, buildID, buildUrl, buildUrlName, token);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBuildUrl(uint buildUrlID)
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            return await _buildUrlService.DeleteBuildUrl(buildUrlID, token);
        }

        [HttpPost]
        public async Task<IActionResult> GetReferenceImages(uint buildID)
        {
            JwtSecurityToken token = ValidateToken();

            return await _referenceImageService.GetReferenceImages(buildID, token);
        }

        [HttpPost]
        public async Task<IActionResult> UploadReferenceImage()
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }

            if (Request.Form.Files.Count <= 0)
            {
                return Json(new { success = false, errorMessage = "No Image submitted" });
            }

            IFormFile file = Request.Form.Files[0];
            if (file == null || file.Length <= 0)
            {
                return Json(new { success = false, errorMessage = "Invalid Image submitted" });
            }

            int buildID = int.Parse(Request.Form["buildID"]);

            return await _referenceImageService.UploadReferenceImage(file, buildID, token);
        }        

        [HttpPost]
        public async Task<IActionResult> SaveImageOrder([FromBody] ImageOrderRequest request)
        {
            return await _referenceImageService.SaveImageOrder(request);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReferenceImage(uint imageID)
        {
            JwtSecurityToken token = ValidateToken();
            if (token == null)
            {
                return Json(new { success = false, errorMessage = "Invalid Token." });
            }
            return await _referenceImageService.DeleteReferenceImage(imageID, token);
        }

        [HttpPost]
        public async Task<IActionResult> GetBulkImageUrls(List<string> imageIDs)
        {
            JwtSecurityToken token = ValidateToken();
            return await _awsService.GetBulkImageUrls(imageIDs, token);
        }
    }
}
