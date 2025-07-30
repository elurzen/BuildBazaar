using Amazon.S3.Model;
using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;

namespace BuildBazaarCore.Services
{
    public interface IBuildUrlService
    {
        Task<IActionResult> GetBuildUrls(uint buildID, JwtSecurityToken token);
        Task<IActionResult> CreateBuildUrl(uint buildID, string buildUrl, string buildUrlName, JwtSecurityToken token);
        Task <IActionResult> UpdateBuildUrl(uint? buildUrlID, uint buildID, string buildUrl, string buildUrlName, JwtSecurityToken token);
        Task <IActionResult> DeleteBuildUrl(uint buildUrlID, JwtSecurityToken token);
    }
    public class BuildUrlService : BaseService, IBuildUrlService
    {
        private readonly IConfigService _configService;
        private readonly IBuildService _buildService;
        
        public BuildUrlService(IConfigService configService, IBuildService buildService)
        {
            _configService = configService;
            _buildService = buildService;
        }

        public async Task<IActionResult> GetBuildUrls(uint buildID, JwtSecurityToken token)
        {
            try
            {
                List<BuildUrlModel> buildUrls = new List<BuildUrlModel>();
                uint userIDClaim = getUserIDFromToken(token);
                bool isPublic = false;

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM BuildUrls WHERE buildID = @buildID";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                            while (await reader.ReadAsync())
                            {
                                isPublic = Convert.ToBoolean(reader["isPublic"]);
                                if (!isPublic && userIDClaim != Convert.ToUInt32(reader["userID"]))
                                {
                                    continue;
                                }
                                BuildUrlModel buildUrl = new BuildUrlModel
                                {
                                    buildUrlID = Convert.ToUInt32(reader["buildUrlID"]),
                                    buildID = Convert.ToUInt32(reader["buildID"]),
                                    buildUrlName = reader["buildUrlName"].ToString(),
                                    buildUrl = reader["buildUrl"].ToString()
                                };
                                buildUrls.Add(buildUrl);
                            }
                    }
                }

                return Json(new { success = true, buildUrls });
            }
            catch (MySqlException ex)
            {
				Console.WriteLine($"BuildService.cs : GetBuildUrls : Error - {ex.Message}");
                return Json(new { success = false, errorMessage = "Something went wrong" });
            }
        }

        public async Task<IActionResult> CreateBuildUrl(uint buildID, string buildUrl, string buildUrlName, JwtSecurityToken token)
        {
            try
            {
                uint userIDClaim = getUserIDFromToken(token);
                bool isPublic = false;

                if (!buildUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !buildUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    buildUrl = "https://" + buildUrl;
                }

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();

                    string query = "SELECT UserID, isPublic FROM Builds WHERE BuildID = @buildID";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
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

                    query = "INSERT INTO BuildUrls (buildID, buildUrl, buildUrlName, userID, isPublic) " +
                        "VALUES (@buildID, @buildUrl, @buildUrlName, @userID, @isPublic);";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        command.Parameters.AddWithValue("@buildUrl", buildUrl);
                        command.Parameters.AddWithValue("@buildUrlName", buildUrlName);
                        command.Parameters.AddWithValue("@userID", userIDClaim);
                        command.Parameters.AddWithValue("@isPublic", isPublic);

                        await command.ExecuteNonQueryAsync();

                        uint buildUrlID  = Convert.ToUInt32(command.LastInsertedId);
                        return Json(new
                        {
                            success = true,
                            buildUrl = new
                            {
                                buildUrlID,
                                buildID,
                                buildUrl,
                                buildUrlName
                            }
                        });
                    }

                }
            }
            catch (MySqlException ex)
            {
				Console.WriteLine($"BuildService.cs : CreateBuildUrl : Error - {ex.Message}");
                return Json(new { success = false, errorMessage = "Something went wrong" });
            }
        }

        public async Task<IActionResult> UpdateBuildUrl(uint? buildUrlID, uint buildID, string buildUrl, string buildUrlName, JwtSecurityToken token)
        {
            try
            {
                uint userIDClaim = getUserIDFromToken(token);

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();
                    string query = "UPDATE BuildUrls SET buildUrl = @buildUrl, buildUrlName = @buildUrlName WHERE buildUrlID = @buildUrlID AND userID = @userID;";

                    if (!buildUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !buildUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        buildUrl = "https://" + buildUrl;
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildUrlID", buildUrlID);
                        command.Parameters.AddWithValue("@buildUrl", buildUrl);
                        command.Parameters.AddWithValue("@buildUrlName", buildUrlName);
                        command.Parameters.AddWithValue("@userID", userIDClaim);

                        await command.ExecuteNonQueryAsync();                        
                        buildUrlID = Convert.ToUInt32(command.LastInsertedId);
                        
                        return Json(new
                        {
                            success = true,
                            buildUrl = new
                            {
                                buildUrlID,
                                buildID,
                                buildUrl,
                                buildUrlName
                            }
                        });
                    }
                }
            }
            catch (MySqlException ex)
            {
				Console.WriteLine($"BuildService.cs : UpdateBuildUrl : Error - {ex.Message}");
                return Json(new { success = false, errorMessage = "Something went wrong" });
            }
        }

        public async Task<IActionResult> DeleteBuildUrl(uint buildUrlID, JwtSecurityToken token)
        {
            try
            {
                uint userIDClaim = getUserIDFromToken(token);
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();

                    // Delete the record from the database
                    string query = "DELETE FROM BuildUrls WHERE buildUrlID = @buildUrlID AND userID = @userID;";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildUrlID", buildUrlID);
                        command.Parameters.AddWithValue("@userID", userIDClaim);

                        await command.ExecuteNonQueryAsync();

                        return Json(new { success = true });
                    }
                }
            }
            catch (MySqlException ex)
            {
				Console.WriteLine($"BuildService.cs : DeleteBuildUrl : Error - {ex.Message}");
                return Json(new { success = false, errorMessage = "Something went wrong" });
            }
        }
    }
}
