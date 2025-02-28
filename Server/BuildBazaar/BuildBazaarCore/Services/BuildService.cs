using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Hosting;
using ZstdSharp.Unsafe;


namespace BuildBazaarCore.Services
{
    public interface IBuildService
    {
        Task<IActionResult> CreateBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token);
        Task CreateExampleBuild(int userID, MySqlConnection connection);
        Task<IActionResult> EditBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token);
        Task<IActionResult> CopyBuild(int originalBuildID, JwtSecurityToken token);
        Task<IActionResult> DeleteBuild(int buildID, JwtSecurityToken token);
        Task<IActionResult> GetBuilds(JwtSecurityToken token);
        Task<IActionResult> GetPublicBuilds(string userName, JwtSecurityToken token);
        Task<(bool success, int buildID, string errorMessage)> getBuildID(uint buildID, bool isPublic, JwtSecurityToken token);
    }

    public class BuildService : BaseService, IBuildService
    {   
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfigService _configService;
        private readonly IAwsService _awsService;
        private readonly IAmazonS3 _s3Client;
        

        public BuildService(IWebHostEnvironment webHostEnvironment,
                           IConfigService configService,
                           IAwsService awsService,
                           IAmazonS3 s3Client)
        {
            _webHostEnvironment = webHostEnvironment;
            _configService = configService;
            _awsService = awsService;
            _s3Client = s3Client;
                      
        }

        public async Task<IActionResult> CreateBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token)
        {
            List<String> s3Objects = new List<String>();

            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        int buildID = -1;
                        var buildQuery = "INSERT INTO Builds (buildName, userID, isPublic) VALUES (@buildName, @userID, @isPublic); SELECT LAST_INSERT_ID()";
                        using (MySqlCommand command = new MySqlCommand(buildQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildName", build.buildName);
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                            command.Parameters.AddWithValue("@isPublic", build.isPublic);
                            buildID = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        string s3ImageFilePath = "";

                        if (file != null && file.Length > 0)
                        {

                            var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            s3ImageFilePath = Path.Combine("Users", userIDClaim, "Thumbnails", imageFileName);
                            using (var memoryStream = new MemoryStream())
                            {
                                await file.CopyToAsync(memoryStream);
                                memoryStream.Position = 0; // Reset stream position after copy
                                await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath);
                                s3Objects.Add(s3ImageFilePath);
                            }
                        }
                        else if (!string.IsNullOrEmpty(thumbnail))
                        {
                            string selectedThumbnailPath = Path.Combine(_webHostEnvironment.WebRootPath, "media", "thumbnails", thumbnail);

                            if (System.IO.File.Exists(selectedThumbnailPath))
                            {
                                // Generate a unique file name for the thumbnail copy
                                var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(selectedThumbnailPath);
                                s3ImageFilePath = Path.Combine("Users", userIDClaim, "Thumbnails", imageFileName);

                                // Copy the default thumbnail and upload it to the user's S3 folder
                                using (var memoryStream = new MemoryStream(await System.IO.File.ReadAllBytesAsync(selectedThumbnailPath)))
                                {
                                    memoryStream.Position = 0;
                                    await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath); // S3 upload method
                                    s3Objects.Add(s3ImageFilePath);
                                }
                            }
                        }
                        else
                        {
                            return Json(new { success = false, errorMessage = "Selected thumbnail not found." });
                        }


                        var imageQuery = "INSERT INTO Images (filePath, typeID, buildID, imageOrder, userID) VALUES (@filePath, 1, @buildID, 0, @userID); SELECT LAST_INSERT_ID()";
                        using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@filePath", s3ImageFilePath);
                            command.Parameters.AddWithValue("@buildID", buildID);
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                            await command.ExecuteNonQueryAsync();
                        }

                        string defaultNoteContent = $"{build.buildName} notes here!";
                        var s3NoteFilePath = Path.Combine("Users", userIDClaim, buildID.ToString(), "notes.txt");
                        var noteQuery = "INSERT INTO Notes (filePath, buildID) VALUES (@filePath, @buildID); SELECT LAST_INSERT_ID()";
                        using (MySqlCommand command = new MySqlCommand(noteQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@filePath", s3NoteFilePath);
                            command.Parameters.AddWithValue("@buildID", buildID);
                            await command.ExecuteNonQueryAsync();
                        }

                        using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(defaultNoteContent)))
                        {
                            memoryStream.Position = 0; // Reset stream position after copy
                            await _awsService.UploadStreamToS3(memoryStream, s3NoteFilePath);
                            s3Objects.Add(s3NoteFilePath);
                        }
                        await transaction.CommitAsync();
                        return Json(new { success = true, message = buildID });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        await _awsService.DeleteFilesFromS3(s3Objects);
                        return Json(new { success = false, errorMessage = ex.Message });
                    }
                }
            }
        }
        
        public async Task CreateExampleBuild(int userID, MySqlConnection connection)
        {
            List<String> s3Objects = new List<String>();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    var buildName = "Example Build";
                    var buildQuery = "INSERT INTO Builds (buildName, userID, isPublic) VALUES (@buildName, @userID, 0); SELECT LAST_INSERT_ID()";
                    int buildID;
                    using (MySqlCommand command = new MySqlCommand(buildQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@buildName", buildName);
                        command.Parameters.AddWithValue("@userID", userID);
                        buildID = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // Paths and filenames
                    var exampleImageFileName = "kirac.png";
                    var exampleUrlsFileName = "urls.csv";
                    var userImageFileName = $"{Guid.NewGuid()}.png"; // Generate a unique filename for the user's copy
                    var localExampleThumbnailFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", exampleImageFileName);  //TODO: Store example build assets on S3
                    var localExampleUrlsFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", exampleUrlsFileName);
                    var fullExampleNotesFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", "notes.txt");
                    var s3UserThumbnailFilePath = $"Users/{userID}/Thumbnails/{userImageFileName}";
                    var s3NoteFilePath = $"Users/{userID}/{buildID}/notes.txt";

                    var s3UserThumbnailUrl = await _awsService.UploadFileToS3(localExampleThumbnailFilePath, s3UserThumbnailFilePath);
                    s3Objects.Add(s3UserThumbnailUrl);

                    var imageQuery = "INSERT INTO Images (filePath, imageOrder, typeID, buildID, userID) VALUES (@filePath, 0, 1, @buildID, @userID)";
                    using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@filePath", s3UserThumbnailFilePath);
                        command.Parameters.AddWithValue("@buildID", buildID);
                        command.Parameters.AddWithValue("@userID", userID);
                        await command.ExecuteNonQueryAsync();
                    }

                    var s3NotesUrl = await _awsService.UploadFileToS3(fullExampleNotesFilePath, s3NoteFilePath);
                    s3Objects.Add(s3NoteFilePath);
                    var noteQuery = "INSERT INTO Notes (filePath, buildID) VALUES (@filePath, @buildID)";
                    using (MySqlCommand command = new MySqlCommand(noteQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@filePath", s3NoteFilePath);
                        command.Parameters.AddWithValue("@buildID", buildID);
                        await command.ExecuteNonQueryAsync();
                    }

                    if (!System.IO.File.Exists(localExampleUrlsFilePath))
                    {
                        throw new FileNotFoundException("Example URLs file not found.");
                    }
                    var csvLines = await System.IO.File.ReadAllLinesAsync(localExampleUrlsFilePath);
                    foreach (var line in csvLines.Skip(1)) // Skip the header row
                    {
                        var columns = line.Split(',', 2); // Split each line into 2 fields based on 1st comma locations (dont use a , in the name lol)
                        var name = columns[0].Trim();
                        var url = columns[1].Trim();

                        // Insert into database
                        var query = "INSERT INTO BuildUrls (buildUrlName, buildUrl, buildID) VALUES (@buildUrlName, @buildUrl, @buildID)";
                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildUrlName", name);
                            command.Parameters.AddWithValue("@buildUrl", url);
                            command.Parameters.AddWithValue("@buildID", buildID);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    var fullExampleImagesPath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", "images");
                    var shortUserBuildImagesFilePath = $"Users/{userID}/{buildID}/";
                    int orderCounter = 0;
                    foreach (var filePath in Directory.GetFiles(fullExampleImagesPath))
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(filePath)}";
                        var s3ImageFilePath = $"{shortUserBuildImagesFilePath}{fileName}";
                        var s3ImageUrl = await _awsService.UploadFileToS3(filePath, s3ImageFilePath);
                        s3Objects.Add(s3ImageFilePath);

                        imageQuery = "INSERT INTO Images (filePath, imageOrder, typeID, buildID, userID) VALUES (@filePath, @imageOrder, 2, @buildID, @userID)";
                        using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@filePath", s3ImageFilePath);
                            command.Parameters.AddWithValue("@imageOrder", orderCounter);
                            command.Parameters.AddWithValue("@buildID", buildID);
                            command.Parameters.AddWithValue("@userID", userID);

                            await command.ExecuteNonQueryAsync();
                        }

                        orderCounter++;
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _awsService.DeleteFilesFromS3(s3Objects);
                    throw;
                }
            }
        }

        public async Task<IActionResult> EditBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token)
        {
            List<String> s3Objects = new List<String>();
            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Check if the build exists and belongs to the user
                        var checkBuildQuery = "SELECT COUNT(*) FROM Builds WHERE buildID = @buildID AND userID = @userID";
                        int buildCount;
                        using (MySqlCommand command = new MySqlCommand(checkBuildQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildID", build.buildID);
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                            buildCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        if (buildCount == 1)
                        {
                            // Update build name if provided)
                            var updateBuildQuery = "UPDATE Builds SET buildName = @buildName, isPublic = @isPublic WHERE buildID = @buildID AND userID = @userID";
                            using (MySqlCommand command = new MySqlCommand(updateBuildQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildName", build.buildName);
                                command.Parameters.AddWithValue("@isPublic", build.isPublic);
                                command.Parameters.AddWithValue("@buildID", build.buildID);
                                command.Parameters.AddWithValue("@userID", userIDClaim);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Update build image if provided
                            if ((file != null && file.Length > 0) || (!string.IsNullOrEmpty(thumbnail)))
                            {
                                string s3ImageFilePath = "";
                                string oldThumbnailPath;
                                var oldThumbnailQuery = "SELECT filePath FROM Images WHERE buildID = @buildID AND userID = @userID AND typeID = 1";
                                using (MySqlCommand command = new MySqlCommand(oldThumbnailQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@buildID", build.buildID);
                                    command.Parameters.AddWithValue("@userID", userIDClaim);
                                    oldThumbnailPath = (string)await command.ExecuteScalarAsync();
                                }
                                if (file != null && file.Length > 0)
                                {
                                    var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                    s3ImageFilePath = Path.Combine("Users", userIDClaim, "Thumbnails", imageFileName);

                                    using (var memoryStream = new MemoryStream())
                                    {
                                        await file.CopyToAsync(memoryStream);
                                        memoryStream.Position = 0; // Reset stream position after copy
                                        await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath);
                                        s3Objects.Add(s3ImageFilePath);
                                    }

                                    await _awsService.DeleteFileFromS3(oldThumbnailPath);
                                }
                                else if (!string.IsNullOrEmpty(thumbnail))
                                {
                                    string selectedThumbnailPath = Path.Combine(_webHostEnvironment.WebRootPath, "media", "thumbnails", thumbnail);
                                    string thumbnailFilePath;

                                    if (System.IO.File.Exists(selectedThumbnailPath))
                                    {
                                        // Generate a unique file name for the thumbnail copy
                                        var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(selectedThumbnailPath);
                                        s3ImageFilePath = Path.Combine("Users", userIDClaim, "Thumbnails", imageFileName);

                                        // Copy the default thumbnail and upload it to the user's S3 folder
                                        using (var memoryStream = new MemoryStream(await System.IO.File.ReadAllBytesAsync(selectedThumbnailPath)))
                                        {
                                            memoryStream.Position = 0;
                                            await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath); // S3 upload method
                                            s3Objects.Add(s3ImageFilePath);
                                        }
                                    }
                                }
                                else
                                {
                                    return Json(new { success = false, errorMessage = "Selected thumbnail not found." });
                                }

                                var updateImageQuery = "UPDATE Images SET filePath = @filePath WHERE buildID = @buildID AND userID = @userID AND typeID = 1";
                                using (MySqlCommand command = new MySqlCommand(updateImageQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@filePath", s3ImageFilePath);
                                    command.Parameters.AddWithValue("@buildID", build.buildID);
                                    command.Parameters.AddWithValue("@userID", userIDClaim);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            await transaction.CommitAsync();
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, errorMessage = "Build not found or you do not have permission to edit this build." });
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        await _awsService.DeleteFilesFromS3(s3Objects);
                        return Json(new { success = false, errorMessage = ex.Message });
                    }
                }
            }
        }

        public async Task<IActionResult> CopyBuild(int originalBuildID, JwtSecurityToken token)
        {
            List<String> s3Objects = new List<String>();
            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        int newBuildID;
                        string originalBuildName;
                        bool isPublic = false;

                        var originalBuildQuery = "SELECT buildName, isPublic FROM Builds WHERE buildID = @originalBuildID";
                        using (MySqlCommand command = new MySqlCommand(originalBuildQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@originalBuildID", originalBuildID);
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    originalBuildName = reader["buildName"]?.ToString();
                                    isPublic = Convert.ToBoolean(reader["isPublic"]);
                                }
                                else
                                {
                                    return Json(new { success = false, errorMessage = "Build not found" });
                                }
                            }
                        }
                        if (!isPublic)
                        {
                            return Json(new { success = false, errorMessage = "Build is not Public" });
                        }

                        var newBuildQuery = "INSERT INTO Builds (buildName, userID, isPublic) VALUES (@buildName, @userID, 0); SELECT LAST_INSERT_ID()";
                        using (MySqlCommand command = new MySqlCommand(newBuildQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildName", originalBuildName);
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                            newBuildID = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        string originalNotePath = "";
                        string originalNotesQuery = "SELECT filePath FROM Notes WHERE buildID = @buildID";
                        using (MySqlCommand command = new MySqlCommand(originalNotesQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildID", originalBuildID);
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    originalNotePath = reader["filePath"]?.ToString();
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(originalNotePath))
                        {
                            string newNotePath = Path.Combine("Users", userIDClaim, newBuildID.ToString(), "notes.txt");
                            var noteQuery = "INSERT INTO Notes (filePath, buildID) VALUES (@filePath, @buildID)";
                            using (MySqlCommand command = new MySqlCommand(noteQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@filePath", newNotePath);
                                command.Parameters.AddWithValue("@buildID", newBuildID);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Copy Notes Content
                            if (!await _awsService.CopyS3ObjectAsync(originalNotePath, newNotePath))
                            {
                                await transaction.RollbackAsync();
                                await _awsService.DeleteFilesFromS3(s3Objects);
                                return Json(new { success = false, errorMessage = "Error copying note" });
                            }
                            s3Objects.Add(newNotePath);
                        }

                        List<BuildUrlModel> newUrlList = new List<BuildUrlModel>();
                        string urlQuery = "SELECT buildUrl, buildUrlName FROM BuildUrls WHERE buildID = @buildID";
                        using (MySqlCommand command = new MySqlCommand(urlQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildID", originalBuildID);
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    BuildUrlModel newUrl = new BuildUrlModel();
                                    newUrl.buildID = Convert.ToUInt32(newBuildID);
                                    newUrl.buildUrl = reader["buildUrl"]?.ToString();
                                    newUrl.buildUrlName = reader["buildUrlName"].ToString();
                                    newUrlList.Add(newUrl);
                                }
                            }
                        }
                        foreach (var newUrl in newUrlList)
                        {
                            var newUrlQuery = "INSERT INTO BuildUrls (buildID, buildUrl, buildUrlName) VALUES (@buildID, @buildUrl, @buildUrlName)";
                            using (MySqlCommand copyImageCommand = new MySqlCommand(newUrlQuery, connection, transaction))
                            {
                                copyImageCommand.Parameters.AddWithValue("@buildID", newUrl.buildID);
                                copyImageCommand.Parameters.AddWithValue("@buildUrl", newUrl.buildUrl);
                                copyImageCommand.Parameters.AddWithValue("@buildUrlName", newUrl.buildUrlName);

                                await copyImageCommand.ExecuteNonQueryAsync();
                            }
                        }

                        List<ImageModel> newImageList = new List<ImageModel>();
                        string imageQuery = "SELECT filePath, typeID, imageOrder FROM Images WHERE buildID = @buildID";
                        using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildID", originalBuildID);
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string originalFilePath = reader["filePath"]?.ToString();
                                    var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFilePath);
                                    string newFilePath = Path.Combine("Users", userIDClaim, "Images", newBuildID.ToString(), imageFileName);

                                    // Insert new image record
                                    ImageModel newImage = new ImageModel();
                                    newImage.buildID = Convert.ToUInt32(newBuildID);
                                    newImage.filePath = newFilePath;
                                    newImage.typeID = Convert.ToUInt32(reader["typeID"]);
                                    newImage.imageOrder = Convert.ToInt32(reader["imageOrder"]);
                                    newImageList.Add(newImage);

                                    if (!await _awsService.CopyS3ObjectAsync(originalFilePath, newFilePath))
                                    {
                                        await transaction.RollbackAsync();
                                        await _awsService.DeleteFilesFromS3(s3Objects);
                                        return Json(new { success = false, errorMessage = "Error copying note" });
                                    }
                                    s3Objects.Add(newFilePath);
                                }
                            }
                        }
                        foreach (var newImage in newImageList)
                        {
                            var newImageQuery = "INSERT INTO Images (buildID, filePath, typeID, imageOrder, userID) VALUES (@buildID, @filePath, @typeID, @imageOrder, @userID)";
                            using (MySqlCommand command = new MySqlCommand(newImageQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildID", newImage.buildID);
                                command.Parameters.AddWithValue("@filePath", newImage.filePath);
                                command.Parameters.AddWithValue("@typeID", newImage.typeID);
                                command.Parameters.AddWithValue("@imageOrder", newImage.imageOrder);
                                command.Parameters.AddWithValue("@userID", userIDClaim);

                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        // Commit the transaction
                        await transaction.CommitAsync();
                        return Json(new { success = true, newBuildID });

                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        await _awsService.DeleteFilesFromS3(s3Objects);
                        return Json(new { success = false, errorMessage = ex.Message });
                    }
                }
            }
        }

        public async Task<IActionResult> DeleteBuild(int buildID, JwtSecurityToken token)
        {
            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Check if the build exists and belongs to the user
                        var checkBuildQuery = "SELECT COUNT(*) FROM Builds WHERE buildID = @buildID AND userID = @userID";
                        int buildCount;
                        using (MySqlCommand command = new MySqlCommand(checkBuildQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@buildID", buildID);
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                            buildCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        if (buildCount == 1)
                        {
                            string ThumbnailPath;
                            var ThumbnailQuery = "SELECT filePath FROM Images WHERE buildID = @buildID AND userID = @userID AND typeID = 1";
                            using (MySqlCommand command = new MySqlCommand(ThumbnailQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildID", buildID);
                                command.Parameters.AddWithValue("@userID", userIDClaim);
                                ThumbnailPath = (string)await command.ExecuteScalarAsync();
                            }

                            // Delete from Images table
                            var deleteImagesQuery = "DELETE FROM Images WHERE buildID = @buildID AND userID = @userID";
                            using (MySqlCommand command = new MySqlCommand(deleteImagesQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildID", buildID);
                                command.Parameters.AddWithValue("@userID", userIDClaim);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete from BuildUrls table
                            var deleteBuildUrlsQuery = "DELETE FROM BuildUrls WHERE buildID = @buildID";
                            using (MySqlCommand command = new MySqlCommand(deleteBuildUrlsQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildID", buildID);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete from Notes table
                            var deleteNotesQuery = "DELETE FROM Notes WHERE buildID = @buildID";
                            using (MySqlCommand command = new MySqlCommand(deleteNotesQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildID", buildID);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete from Builds table
                            var deleteBuildQuery = "DELETE FROM Builds WHERE buildID = @buildID AND userID = @userID";
                            using (MySqlCommand command = new MySqlCommand(deleteBuildQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@buildID", buildID);
                                command.Parameters.AddWithValue("@userID", userIDClaim);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Commit the transaction

                            string prefix = Path.Combine("Users", userIDClaim, buildID.ToString());
                            await _awsService.BulkDeleteFromS3(prefix);
                            await _awsService.DeleteFileFromS3(ThumbnailPath);
                            await transaction.CommitAsync();
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, errorMessage = "Build not found or you do not have permission to delete this build." });
                        }
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { success = false, errorMessage = ex.Message });
                    }
                }
            }
        }

        public async Task<IActionResult> GetBuilds(JwtSecurityToken token)
        {
            try
            {
                List<BuildModel> builds = new List<BuildModel>();
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                    connection.Open();
                    string query = "SELECT * FROM Images Join Builds ON Images.buildID = Builds.buildID WHERE Images.userID = @userID AND Images.typeID = 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userID", userIDClaim);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                            while (reader.Read())
                            {
                                BuildModel build = new BuildModel();
                                build.buildID = Convert.ToUInt32(reader["buildID"]);
                                build.userID = Convert.ToUInt32(reader["userID"]);
                                build.imageID = Convert.ToUInt32(reader["imageID"]);
                                build.buildName = reader["buildName"].ToString();
                                build.isPublic = Convert.ToBoolean(reader["isPublic"]);
                                build.filePath = reader["filePath"].ToString();

                                builds.Add(build);
                            }
                    }
                }
                return Json(new { success = true, builds });
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> GetPublicBuilds(string userName, JwtSecurityToken token)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(userName) && !Regex.IsMatch(userName, "^[a-zA-Z0-9_.-]+$"))
                {
                    return Json(new { success = false, errorMessage = "Invalid username format" });
                }
                int userID = -1;
                List<BuildModel> builds = new List<BuildModel>();
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();

                    if (!string.IsNullOrEmpty(userName))
                    {
                        string userQuery = "SELECT userID FROM Users WHERE Users.Username = @userName LIMIT 1";
                        using (MySqlCommand command = new MySqlCommand(userQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userName", userName);
                            var result = await command.ExecuteScalarAsync();

                            if (result == null)
                            {
                                return Json(new { success = false, errorMessage = "Invalid request" });
                            }
                            userID = Convert.ToInt32(result);
                        }
                    }
                    else
                    {
                        if (token == null)
                        {
                            return Json(new { success = false, errorMessage = "Log in or specify a user" });
                        }
                        userID = Convert.ToInt32(token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
                        string userQuery = "SELECT userName FROM Users WHERE Users.userID = @userID LIMIT 1";
                        using (MySqlCommand command = new MySqlCommand(userQuery, connection))
                        {
                            command.Parameters.AddWithValue("@userID", userID);
                            var result = await command.ExecuteScalarAsync();

                            if (result == null)
                            {
                                return Json(new { success = false, errorMessage = "Invalid request" });
                            }
                            userName = result.ToString();
                        }
                    }

                    string imageQuery = "SELECT * FROM Images Join Builds ON Images.buildID = Builds.buildID WHERE Images.userID = @userID AND Images.typeID = 1 AND Builds.isPublic = 1";
                    using (MySqlCommand command = new MySqlCommand(imageQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userID", userID);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                            while (reader.Read())
                            {
                                BuildModel build = new BuildModel();
                                build.buildID = Convert.ToUInt32(reader["buildID"]);
                                build.userID = Convert.ToUInt32(reader["userID"]);
                                build.imageID = Convert.ToUInt32(reader["imageID"]);
                                build.buildName = reader["buildName"].ToString();
                                build.isPublic = Convert.ToBoolean(reader["isPublic"]);
                                build.filePath = reader["filePath"].ToString();

                                builds.Add(build);
                            }
                    }
                }
                return Json(new { success = true, builds, userName = userName });
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<(bool success, int buildID, string errorMessage)> getBuildID(uint buildID, bool isPublic, JwtSecurityToken token)
        {
            if (isPublic)
            {
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = "SELECT BuildID FROM Builds WHERE BuildID = @buildID AND IsPublic = 1";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            return (false, -1, "Build not found or not public.");
                        }

                        return (true, Convert.ToInt32(result), null);
                    }
                }
            }
            else
            {
                if (token == null)
                {
                    return (false, -1, "Invalid token.");
                }
                
                var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    string query = "SELECT BuildID FROM Builds WHERE BuildID = @buildID AND UserID = @userID";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        command.Parameters.AddWithValue("@userID", userIDClaim);
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            return (false, -1, "Build not found or user does not own it.");
                        }

                        return (true, Convert.ToInt32(result), null);
                    }
                }
            }

        }
    }
}
