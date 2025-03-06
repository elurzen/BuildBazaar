using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using System.ComponentModel.Design;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;

namespace BuildBazaarCore.Services
{
    public interface IReferenceImageService
    {
        Task<IActionResult> UploadReferenceImage(IFormFile file, int buildID, JwtSecurityToken token);
        Task<IActionResult> GetReferenceImages(uint buildID, JwtSecurityToken token);
        Task<IActionResult> SaveImageOrder(ImageOrderRequest request);
        Task<IActionResult> DeleteReferenceImage(uint imageID, JwtSecurityToken token);
    }
    public class ReferenceImageService : BaseService, IReferenceImageService
    {
        private readonly IConfigService _configService;
        private readonly IBuildService _buildService;
        private readonly IAwsService _awsService;
        private readonly IImageProcessingService _imageProcessingService;
        public ReferenceImageService(IConfigService configService, IBuildService buildService, IAwsService awsService, IImageProcessingService imageProcessingService)
        {
            _configService = configService;
            _buildService = buildService;
            _awsService = awsService;
            _imageProcessingService = imageProcessingService;
        }

        public async Task<IActionResult> UploadReferenceImage(IFormFile file, int buildID, JwtSecurityToken token)
        {
            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    var s3ImageFilePath = "";
                    try
                    {
                        uint userIDClaim = getUserIDFromToken(token);
                        bool isPublic = false;

                        string buildQuery = "SELECT UserID, isPublic FROM Builds WHERE BuildID = @buildID";
                        using (MySqlCommand command = new MySqlCommand(buildQuery, connection))
                        {
                            command.Parameters.AddWithValue("@buildID", buildID);
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (!await reader.ReadAsync())
                                {
                                    return Json(new { success = false, errorMessage = "Build not found." });
                                }

                                uint userID = Convert.ToUInt32(reader["UserID"]);
                                isPublic = Convert.ToBoolean(reader["isPublic"]);

                                if (userID != userIDClaim)
                                {
                                    return Json(new { success = false, errorMessage = "Unauthorized access." });
                                }
                            }
                        }

                        var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        s3ImageFilePath = Path.Combine("Users", userIDClaim.ToString(), buildID.ToString(), imageFileName);

                        int maxOrder;
                        var maxOrderQuery = "SELECT IFNULL(MAX(imageOrder), 0) FROM Images WHERE buildID = @buildID";
                        using (MySqlCommand command = new MySqlCommand(maxOrderQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildID", buildID);
                            maxOrder = Convert.ToInt32(command.ExecuteScalar());
                        }

                        var imageQuery = "INSERT INTO Images (filePath, imageOrder, typeID, buildID, userID, isPublic) VALUES (@filePath, @imageOrder, 2, @buildID, @userID, isPublic)";
                        using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@filePath", s3ImageFilePath);
                            command.Parameters.AddWithValue("@imageOrder", maxOrder + 1);
                            command.Parameters.AddWithValue("@buildID", buildID);
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                            command.Parameters.AddWithValue("@isPublic", isPublic);
                            
                            command.ExecuteNonQuery();
                        }

                        using (var memoryStream = await _imageProcessingService.StripExifData(file))
                        {
                            if (_configService.GetEnvironment() != "local")
                            {
                                // Upload the clean image to S3
                                await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath);
                            }
                            else
                            {
                                using (FileStream localFile = new FileStream("localImages/" + imageFileName, FileMode.Create))
                                {
                                    memoryStream.CopyTo(localFile);
                                }
                            }
                        }

                        await transaction.CommitAsync();
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        if (_configService.GetEnvironment() != "local")
                        {
                            await _awsService.DeleteFileFromS3(s3ImageFilePath);
                        }
                        return Json(new { success = false, errorMessage = ex.Message });
                    }
                }
            }
        }

        public async Task<IActionResult> GetReferenceImages(uint buildID, JwtSecurityToken token)
        {
            try
            {
                List<ImageModel> images = new List<ImageModel>();
                uint userIDClaim = getUserIDFromToken(token);

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Images WHERE Images.typeID = 2 AND buildID = @buildID";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bool isPublic = Convert.ToBoolean(reader["isPublic"]);
                                if (!isPublic && userIDClaim != Convert.ToUInt32(reader["userID"]))
                                {
                                    continue;
                                }
                                ImageModel image = new ImageModel();
                                image.imageID = Convert.ToUInt32(reader["imageID"]);
                                image.buildID = Convert.ToUInt32(reader["buildID"]);
                                image.imageOrder = Convert.ToInt32(reader["imageOrder"]);
                                image.filePath = reader["filePath"].ToString();

                                images.Add(image);
                            }
                        }
                    }
                }
                return Json(new { success = true, images });
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> SaveImageOrder(ImageOrderRequest request)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    foreach (var item in request.newOrder)
                    {
                        var query = "UPDATE Images SET imageOrder = @imageOrder WHERE imageID = @imageID AND buildID = @buildID";
                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@imageOrder", item.imageOrder);
                            command.Parameters.AddWithValue("@imageID", item.imageID);
                            command.Parameters.AddWithValue("@buildID", request.buildID);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteReferenceImage(uint imageID, JwtSecurityToken token)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    uint userIDClaim = getUserIDFromToken(token);
                    string filePath;
                    await connection.OpenAsync();

                    // Retrieve the file path from the database based on the imageID
                    string filePathQuery = "SELECT filePath FROM Images WHERE imageID = @imageID AND userID = @userID;";
                    using (MySqlCommand command = new MySqlCommand(filePathQuery, connection))
                    {
                        command.Parameters.AddWithValue("@imageID", imageID);
                        command.Parameters.AddWithValue("@userID", userIDClaim);
                        filePath = (string)await command.ExecuteScalarAsync();
                    }

                    // Delete the file from the server
                    if (string.IsNullOrEmpty(filePath))
                    {
                        return Json(new { success = false, errorMessage = "File not found" });
                    }

                    bool isDeleted = true;
                    if (_configService.GetEnvironment() != "local")
                    {
                        isDeleted = await _awsService.DeleteFileFromS3(filePath);
                    }

                    if (!isDeleted)
                    {
                        return Json(new { success = false, errorMessage = "Error deleting file" });
                    }
                    // Delete the record from the database
                    string query = "DELETE FROM Images WHERE imageID = @imageID AND userID = @userID;";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@imageID", imageID);
                        command.Parameters.AddWithValue("@userID", userIDClaim);

                        await command.ExecuteNonQueryAsync();

                        return Json(new { success = true });
                    }
                }
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }
    }
}
