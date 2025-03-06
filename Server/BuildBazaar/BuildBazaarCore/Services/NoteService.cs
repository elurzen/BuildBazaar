using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace BuildBazaarCore.Services
{
    public interface INoteService
    {
        Task<IActionResult> GetNote(uint buildID, JwtSecurityToken token);
        Task<IActionResult> SetNote(int buildID, string noteContent, JwtSecurityToken token);
    }
    public class NoteService : BaseService, INoteService
    {
        private readonly IConfigService _configService;
        private readonly IAwsService _awsService;
        
        public NoteService(IConfigService configService, IAwsService awsService)
        {
            _configService = configService;
            _awsService = awsService;
        }

        public async Task<IActionResult> GetNote(uint buildID, JwtSecurityToken token)
        {
            try
            {
                NoteModel note = new NoteModel();
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Notes WHERE buildID = @buildID LIMIT 1;";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return Json(new { success = false, errorMessage = "Note not found" });
                            }

                            bool isPublic = Convert.ToBoolean(reader["isPublic"]);
                            uint recordUserID = Convert.ToUInt32(reader["userID"]);
                            if (!isPublic && recordUserID != getUserIDFromToken(token))
                            {
                                return Json(new { success = false, errorMessage = "Unauthorized access." });
                            }

                            note.noteID = Convert.ToUInt32((reader["noteID"]));
                            note.buildID = Convert.ToUInt32(reader["buildID"]);
                            note.filePath = reader["filePath"]?.ToString();

                            JsonResult presignedNoteFileUrl;
                            if (_configService.GetEnvironment() != "local")
                            {
                                //Your CloudFront distribution needs a behavior to not cache */notes.txt files
                                presignedNoteFileUrl = _awsService.GenerateCloudFrontSignedUrl(note.filePath) as JsonResult;
                            }
                            else
                            {
                                presignedNoteFileUrl = Json("localtest");
                            }
                            return Json(new { success = true, noteFileUrl = presignedNoteFileUrl.Value });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> SetNote(int buildID, string noteContent, JwtSecurityToken token)
        {
            try
            {
                string filePath = "";
                string safeNoteContent = WebUtility.HtmlEncode(noteContent);
                uint userIDClaim = getUserIDFromToken(token);

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT filePath FROM Notes WHERE buildID = @buildID AND UserID = @userID LIMIT 1;";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        command.Parameters.AddWithValue("@userID", userIDClaim);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                return Json(new { success = false, errorMessage = "Note not found" });
                            }
                            filePath = reader["filePath"]?.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return Json(new { success = false, errorMessage = "File path not found." });
                }

                using (var stream = new MemoryStream())
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(safeNoteContent);
                    writer.Flush();
                    stream.Position = 0;

                    if (_configService.GetEnvironment() != "local")
                    {
                        await _awsService.UploadStreamToS3(stream, filePath);
                    }

                    return Json(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }
    }
}
