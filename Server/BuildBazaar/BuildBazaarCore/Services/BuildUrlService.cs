using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;

namespace BuildBazaarCore.Services
{
    public interface IBuildUrlService
    {
        Task <IActionResult> UpdateBuildUrl(uint? buildUrlID, uint buildID, string buildUrl, string buildUrlName);
        Task <IActionResult> GetBuildUrls(uint buildID, bool isPublic, JwtSecurityToken token);
        Task <IActionResult> DeleteBuildUrl(uint buildUrlID);
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

        public async Task<IActionResult> UpdateBuildUrl(uint? buildUrlID, uint buildID, string buildUrl, string buildUrlName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();
                    string query = string.Empty;

                    if (buildUrlID == null)
                    {
                        // Create new record
                        query = "INSERT INTO BuildUrls (buildID, buildUrl, buildUrlName) VALUES (@buildID, @buildUrl, @buildUrlName);";
                    }
                    else
                    {
                        // Update existing record
                        query = "UPDATE BuildUrls SET buildUrl = @buildUrl, buildUrlName = @buildUrlName WHERE buildUrlID = @buildUrlID;";
                    }

                    if (!buildUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !buildUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        buildUrl = "https://" + buildUrl;
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildUrl", buildUrl);
                        command.Parameters.AddWithValue("@buildUrlName", buildUrlName);

                        if (buildUrlID != null)
                        {
                            command.Parameters.AddWithValue("@buildUrlID", buildUrlID);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@buildID", buildID);
                        }

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            if (buildUrlID == null)
                            {
                                buildUrlID = Convert.ToUInt32(command.LastInsertedId);
                            }
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
                        else
                        {
                            return Json(new { success = false, errorMessage = "No records updated." });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> GetBuildUrls(uint buildID, bool isPublic, JwtSecurityToken token)
        {
            var (success, validBuildID, errorMessage) = await _buildService.getBuildID(buildID, isPublic, token);

            if (!success)
            {
                return Json(new { success = false, errorMessage });
            }
            try
            {
                List<BuildUrlModel> buildUrls = new List<BuildUrlModel>();

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT buildUrlID, buildID, buildUrlName, buildUrl FROM BuildUrls WHERE buildID = @buildID";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", validBuildID);

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
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
                }

                return Json(new { success = true, buildUrls });
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> DeleteBuildUrl(uint buildUrlID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();

                    // Delete the record from the database
                    string query = "DELETE FROM BuildUrls WHERE buildUrlID = @buildUrlID;";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildUrlID", buildUrlID);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            return Json(new { success = true });
                        }
                        else
                        {
                            return Json(new { success = false, errorMessage = "No records deleted." });
                        }
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
