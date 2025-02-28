using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;

namespace BuildBazaarCore.Services
{
    public interface INoteService
    {
        Task<IActionResult> GetNote(uint buildID, bool isPublic, JwtSecurityToken token);
        Task<IActionResult> SetNote(int buildID, string noteContent);
    }
    public class NoteService : BaseService, INoteService
    {
        private readonly IConfigService _configService;
        private readonly IBuildService _buildService;
        private readonly IAwsService _awsService;
        
        public NoteService(IConfigService configService, IBuildService buildService, IAwsService awsService)
        {
            _configService = configService;
            _buildService = buildService;
            _awsService = awsService;
        }

        public async Task<IActionResult> GetNote(uint buildID, bool isPublic, JwtSecurityToken token)
        {
            //Access validation done in getBuildID
            var (success, validBuildID, errorMessage) = await _buildService.getBuildID(buildID, isPublic, token);

            if (!success)
            {
                return Json(new { success = false, errorMessage });
            }

            try
            {
                NoteModel note = new NoteModel();
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Notes WHERE Notes.buildID = @buildID LIMIT 1;";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", validBuildID);
                        using (var reader = await command.ExecuteReaderAsync())
                            if (await reader.ReadAsync())
                            {
                                note.noteID = Convert.ToUInt32((reader["noteID"]));
                                note.buildID = Convert.ToUInt32(reader["buildID"]);
                                note.filePath = reader["filePath"]?.ToString();
                            }
                    }
                }

                //Your CloudFront distribution needs a behavior to not cache */notes.txt files
                var presignedNoteFileUrl = _awsService.GenerateCloudFrontSignedUrl(note.filePath) as JsonResult;
                return Json(new { success = true, noteFileUrl = presignedNoteFileUrl.Value });
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> SetNote(int buildID, string noteContent)
        {
            try
            {
                string filePath = "";
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT * FROM Notes WHERE Notes.buildID = @buildID LIMIT 1;";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildID", buildID);
                        using (var reader = command.ExecuteReader())
                            while (reader.Read())
                            {
                                filePath = reader["filePath"]?.ToString();
                            }
                    }
                }
                if (!string.IsNullOrEmpty(filePath))
                {
                    try
                    {
                        using (var stream = new MemoryStream())
                        {
                            var writer = new StreamWriter(stream);
                            writer.Write(noteContent);
                            writer.Flush();
                            stream.Position = 0;

                            await _awsService.UploadStreamToS3(stream, filePath);

                            return Json(new { success = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, errorMessage = ex.Message });
                    }
                }
                else
                {
                    return Json(new { success = false, errorMessage = "File path not found." });
                }
            }
            catch (MySqlException ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }
    }
}
