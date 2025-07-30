using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Hosting;
using ZstdSharp.Unsafe;
using System.Transactions;
using Amazon.CloudFront.Model;
using Org.BouncyCastle.Tls.Crypto;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Linq.Expressions;


namespace BuildBazaarCore.Services
{
	public interface IBuildService
	{
		Task<IActionResult> GetBuilds(JwtSecurityToken token);
		Task<IActionResult> GetPublicBuilds(string userName, JwtSecurityToken token);
		Task<IActionResult> CreateBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token);
		Task CreateExampleBuild(int userID, MySqlConnection connection);
		Task<IActionResult> EditBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token);
		Task<IActionResult> CopyBuild(int originalBuildID, JwtSecurityToken token);
		Task<IActionResult> DeleteBuild(int buildID, JwtSecurityToken token);
		Task<IActionResult> SearchBuilds(int page, string sortBy, IFormCollection request);
		Task<IActionResult> GetClassesAndTags(int lastTagId = 0, int lastClassId = 0);
	}

	public class BuildService : BaseService, IBuildService
	{
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly IConfigService _configService;
		private readonly IAwsService _awsService;
		private readonly IAmazonS3 _s3Client;
		private readonly IImageProcessingService _imageProcessingService;


		public BuildService(IWebHostEnvironment webHostEnvironment,
				IConfigService configService,
				IAwsService awsService,
				IAmazonS3 s3Client,
				IImageProcessingService imageProcessingService)
		{
			_webHostEnvironment = webHostEnvironment;
			_configService = configService;
			_awsService = awsService;
			_s3Client = s3Client;
			_imageProcessingService = imageProcessingService;

		}

		public async Task<IActionResult> GetBuilds(JwtSecurityToken token)
		{
			try
			{
				List<BuildModel> builds = new List<BuildModel>();
				using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
				{
					uint userIDClaim = getUserIDFromToken(token);
					connection.Open();

					string buildQuery = @"
						SELECT b.*, i.imageID, i.filePath, GROUP_CONCAT(t.tagName SEPARATOR ',') as tags
						FROM Builds b
						JOIN Images i ON i.buildID = b.buildID
						LEFT JOIN BuildTagLinks btl ON btl.buildID = b.buildID
						LEFT JOIN Tags t ON t.tagID = btl.tagID
						WHERE i.userID = @userID AND i.typeID = 1
						GROUP BY b.buildID, i.imageID, i.filePath";

					using (MySqlCommand command = new MySqlCommand(buildQuery, connection))
					{
						command.Parameters.AddWithValue("@userID", userIDClaim);
						using (DbDataReader reader = await command.ExecuteReaderAsync())
							while (reader.Read())
							{
								BuildModel build = new BuildModel();
								build.buildID = Convert.ToUInt32(reader["buildID"]);
								build.userID = Convert.ToUInt32(reader["userID"]);
								build.imageID = Convert.ToUInt32(reader["imageID"]);
								build.buildName = Convert.ToString(reader["buildName"]);
								build.isPublic = Convert.ToBoolean(reader["isPublic"]);
								build.filePath = Convert.ToString(reader["filePath"]);
								build.gameID = Convert.ToUInt32(reader["gameID"]);
								build.classID = Convert.ToUInt32(reader["classID"]);
								build.tags = Convert.ToString(reader["tags"]).Replace(",", ", ");

								builds.Add(build);
							}
					}
				}
				return Json(new { success = true, builds });
			}
			catch (MySqlException ex)
			{
				Console.WriteLine($"BuildService.cs : GetBuilds : Error - {ex.Message}");
				return Json(new { success = false, errorMessage = "Something went wrong" });
			}
		}

		public async Task<IActionResult> GetPublicBuilds(string userName, JwtSecurityToken token)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(userName) && !Regex.IsMatch(userName, "^[a-zA-Z0-9_.-]+$"))
				{

					Console.WriteLine($"BuildService.cs : GetPublicBuilds : Error - Invalid Username");
					return Json(new { success = false, errorMessage = "Invalid username format" });
				}
				uint userID;
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

								Console.WriteLine($"BuildService.cs : GetPublicBuilds : Error - Username Not Found");
								return Json(new { success = false, errorMessage = "Invalid request" });
							}
							userID = Convert.ToUInt32(result);
						}
					}
					else
					{
						if (token == null)
						{
							return Json(new { success = false, errorMessage = "Log in or specify a user" });
						}
						userID = getUserIDFromToken(token);
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
								build.buildName = Convert.ToString(reader["buildName"]);
								build.isPublic = Convert.ToBoolean(reader["isPublic"]);
								build.filePath = Convert.ToString(reader["filePath"]);
								build.gameID = Convert.ToUInt32(reader["gameID"]);
								build.classID = Convert.ToUInt32(reader["classID"]);

								builds.Add(build);
							}
					}
				}
				return Json(new { success = true, builds, userName = userName });
			}
			catch (MySqlException ex)
			{
				Console.WriteLine($"BuildService.cs : GetPublicBuilds : Error - {ex.Message}");
				return Json(new { success = false, errorMessage = "Something went wrong" });
			}
		}

		public async Task<IActionResult> CreateBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token)
		{
			List<String> s3Objects = new List<String>();

			using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
			{
				uint userIDClaim = getUserIDFromToken(token);
				await connection.OpenAsync();

				using (var transaction = await connection.BeginTransactionAsync())
				{
					try
					{
						int buildID = -1;
						var buildQuery = "INSERT INTO Builds (buildName, userID, isPublic, gameID, classID) VALUES (@buildName, @userID, @isPublic, @gameID, @classID); SELECT LAST_INSERT_ID()";
						using (MySqlCommand command = new MySqlCommand(buildQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildName", build.buildName);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							command.Parameters.AddWithValue("@isPublic", build.isPublic);
							command.Parameters.AddWithValue("@gameID", build.gameID);
							command.Parameters.AddWithValue("@classID", build.classID);
							buildID = Convert.ToInt32(await command.ExecuteScalarAsync());
						}

						string s3ImageFilePath = "";

						if (file != null && file.Length > 0)
						{
							var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
							s3ImageFilePath = Path.Combine("Users", userIDClaim.ToString(), "Thumbnails", imageFileName);

							using (var memoryStream = await _imageProcessingService.ResizeAndStripExif(file, 125, 125, true))
							{
								if (_configService.GetEnvironment() != "local")
								{
									// Upload the clean image to S3
									await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath);
								}
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
								s3ImageFilePath = Path.Combine("Users", userIDClaim.ToString(), "Thumbnails", imageFileName);

								// Copy the default thumbnail and upload it to the user's S3 folder
								using (var memoryStream = new MemoryStream(await System.IO.File.ReadAllBytesAsync(selectedThumbnailPath)))
								{
									memoryStream.Position = 0;
									if (_configService.GetEnvironment() != "local")
									{
										await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath); // S3 upload method
									}
									s3Objects.Add(s3ImageFilePath);
								}
							}
						}
						else
						{
							return Json(new { success = false, errorMessage = "Selected thumbnail not found." });
						}

						var imageQuery = "INSERT INTO Images (filePath, typeID, buildID, imageOrder, userID, isPublic) VALUES (@filePath, 1, @buildID, 0, @userID, @isPublic); SELECT LAST_INSERT_ID()";
						using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@filePath", s3ImageFilePath);
							command.Parameters.AddWithValue("@buildID", buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							command.Parameters.AddWithValue("@isPublic", build.isPublic);
							await command.ExecuteNonQueryAsync();
						}

						string defaultNoteContent = $"{build.buildName} notes here!";
						var s3NoteFilePath = Path.Combine("Users", userIDClaim.ToString(), buildID.ToString(), "notes.txt");
						var noteQuery = "INSERT INTO Notes (filePath, buildID, userID, isPublic) VALUES (@filePath, @buildID, @userID, @isPublic); SELECT LAST_INSERT_ID()";
						using (MySqlCommand command = new MySqlCommand(noteQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@filePath", s3NoteFilePath);
							command.Parameters.AddWithValue("@buildID", buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							command.Parameters.AddWithValue("@isPublic", build.isPublic);
							await command.ExecuteNonQueryAsync();
						}

						if (build.tags == null)
						{
							build.tags = "";
						}

						var processedTags = build.tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
							.Select(t => t.ToLower())
							.Distinct()
							.Take(5)
							.ToList();

						// var tagParamNames = processedTags.Select((tag, i) => $"@tag{i}").ToList();
						// var inClause = string.Join(",", tagParamNames);
						var insertTagQuery = "INSERT INTO Tags (tagName, gameID) VALUES (LOWER(@tagName), @gameID) ON DUPLICATE KEY UPDATE tagID = LAST_INSERT_ID(tagID); SELECT LAST_INSERT_ID();";

						var tagLinkValues = new List<string>();
						var parameters = new List<MySqlParameter>();
						int paramIndex = 0;
						foreach (string tag in processedTags)
						{
							int tagID;
							using (MySqlCommand command = new MySqlCommand(insertTagQuery, connection, transaction))
							{
								command.Parameters.AddWithValue("@tagName", tag);
								command.Parameters.AddWithValue("@gameID", build.gameID);
								tagID = Convert.ToInt32(await command.ExecuteScalarAsync());
							}

							var buildIDParam = $"@buildID{paramIndex}";
							var tagIDParam = $"@tagID{paramIndex}";
							tagLinkValues.Add($"({buildIDParam}, {tagIDParam})");

							parameters.Add(new MySqlParameter(buildIDParam, buildID));
							parameters.Add(new MySqlParameter(tagIDParam, tagID));

							paramIndex++;
						}

						if (tagLinkValues.Any())
						{
							var insertLinksQuery = $"INSERT INTO BuildTagLinks (buildID, tagID) VALUES {string.Join(", ", tagLinkValues)}";
							using (var command = new MySqlCommand(insertLinksQuery, connection, transaction))
							{
								command.Parameters.AddRange(parameters.ToArray());
								await command.ExecuteNonQueryAsync();
							}
						}

						using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(defaultNoteContent)))
						{
							memoryStream.Position = 0; // Reset stream position after copy
							if (_configService.GetEnvironment() != "local")
							{
								await _awsService.UploadStreamToS3(memoryStream, s3NoteFilePath);
							}
							s3Objects.Add(s3NoteFilePath);
						}
						await transaction.CommitAsync();
						return Json(new { success = true, message = buildID });
					}
					catch (Exception ex)
					{
						await transaction.RollbackAsync();
						if (_configService.GetEnvironment() != "local")
						{
							await _awsService.DeleteFilesFromS3(s3Objects);
						}

						Console.WriteLine($"BuildService.cs : Create Build : Error - {ex.Message}");
						return Json(new { success = false, errorMessage = "Something went wrong" });
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
					var buildQuery = "INSERT INTO Builds (buildName, userID, isPublic, gameID, classID) VALUES (@buildName, @userID, 0, 1, 7); SELECT LAST_INSERT_ID()";
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
					var localExampleThumbnailFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", exampleImageFileName);
					var localExampleUrlsFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", exampleUrlsFileName);
					var fullExampleNotesFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "ExampleBuild", "notes.txt");
					var s3UserThumbnailFilePath = $"Users/{userID}/Thumbnails/{userImageFileName}";
					var s3NoteFilePath = $"Users/{userID}/{buildID}/notes.txt";


					if (_configService.GetEnvironment() != "local")
					{
						var s3UserThumbnailUrl = await _awsService.UploadFileToS3(localExampleThumbnailFilePath, s3UserThumbnailFilePath);
						s3Objects.Add(s3UserThumbnailUrl);
					}


					var imageQuery = "INSERT INTO Images (filePath, imageOrder, typeID, buildID, userID, isPublic) VALUES (@filePath, 0, 1, @buildID, @userID, 0)";
					using (MySqlCommand command = new MySqlCommand(imageQuery, connection, transaction))
					{
						command.Parameters.AddWithValue("@filePath", s3UserThumbnailFilePath);
						command.Parameters.AddWithValue("@buildID", buildID);
						command.Parameters.AddWithValue("@userID", userID);
						await command.ExecuteNonQueryAsync();
					}

					if (_configService.GetEnvironment() != "local")
					{
						var s3NotesUrl = await _awsService.UploadFileToS3(fullExampleNotesFilePath, s3NoteFilePath);
					}

					s3Objects.Add(s3NoteFilePath);
					var noteQuery = "INSERT INTO Notes (filePath, buildID, userID, isPublic) VALUES (@filePath, @buildID, @userID, 0)";
					using (MySqlCommand command = new MySqlCommand(noteQuery, connection, transaction))
					{
						command.Parameters.AddWithValue("@filePath", s3NoteFilePath);
						command.Parameters.AddWithValue("@buildID", buildID);
						command.Parameters.AddWithValue("@userID", userID);
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
						var query = "INSERT INTO BuildUrls (buildUrlName, buildUrl, buildID, userID, isPublic) VALUES (@buildUrlName, @buildUrl, @buildID, @userID, 0)";
						using (var command = new MySqlCommand(query, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildUrlName", name);
							command.Parameters.AddWithValue("@buildUrl", url);
							command.Parameters.AddWithValue("@buildID", buildID);
							command.Parameters.AddWithValue("@userID", userID);
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
						if (_configService.GetEnvironment() != "local")
						{
							var s3ImageUrl = await _awsService.UploadFileToS3(filePath, s3ImageFilePath);
						}
						s3Objects.Add(s3ImageFilePath);

						imageQuery = "INSERT INTO Images (filePath, imageOrder, typeID, buildID, userID, isPublic) VALUES (@filePath, @imageOrder, 2, @buildID, @userID, 0)";
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
					if (_configService.GetEnvironment() != "local")
					{
						await _awsService.DeleteFilesFromS3(s3Objects);
					}
					throw;
				}
			}
		}

		public async Task<IActionResult> EditBuild(BuildModel build, IFormFile file, string thumbnail, JwtSecurityToken token)
		{
			List<String> s3Objects = new List<String>();
			using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
			{
				uint userIDClaim = getUserIDFromToken(token);
				await connection.OpenAsync();

				using (var transaction = await connection.BeginTransactionAsync())
				{
					try
					{
						// Check if the build exists and belongs to the user
						var checkBuildQuery = "SELECT isPublic FROM Builds WHERE buildID = @buildID AND userID = @userID";
						bool isPublic;

						using (MySqlCommand command = new MySqlCommand(checkBuildQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildID", build.buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							using (DbDataReader reader = await command.ExecuteReaderAsync())
							{
								if (!await reader.ReadAsync())
								{
									return Json(new { success = false, errorMessage = "Build not found or you do not have permission to edit this build." });
								}
								isPublic = Convert.ToBoolean(reader["isPublic"]);
							}
						}

						// Update build name if provided
						var updateBuildQuery = @"UPDATE Builds SET buildName = @buildName, isPublic = @isPublic, gameID = @gameID, classID = @classID 
							WHERE buildID = @buildID AND userID = @userID";
						using (MySqlCommand command = new MySqlCommand(updateBuildQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildName", build.buildName);
							command.Parameters.AddWithValue("@isPublic", build.isPublic);
							command.Parameters.AddWithValue("@gameID", build.gameID);
							command.Parameters.AddWithValue("@classID", build.classID);
							command.Parameters.AddWithValue("@buildID", build.buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							await command.ExecuteNonQueryAsync();
						}

						if (isPublic != build.isPublic)
						{
							var updateNotesQuery = "UPDATE Notes SET isPublic = @isPublic WHERE buildID = @buildID AND userID = @userID";
							using (MySqlCommand command = new MySqlCommand(updateNotesQuery, connection, transaction))
							{
								command.Parameters.AddWithValue("@isPublic", build.isPublic);
								command.Parameters.AddWithValue("@buildID", build.buildID);
								command.Parameters.AddWithValue("@userID", userIDClaim);
								await command.ExecuteNonQueryAsync();
							}

							var updateUrlsQuery = "UPDATE BuildUrls SET isPublic = @isPublic WHERE buildID = @buildID AND userID = @userID";
							using (MySqlCommand command = new MySqlCommand(updateUrlsQuery, connection, transaction))
							{
								command.Parameters.AddWithValue("@isPublic", build.isPublic);
								command.Parameters.AddWithValue("@buildID", build.buildID);
								command.Parameters.AddWithValue("@userID", userIDClaim);
								await command.ExecuteNonQueryAsync();
							}

							var updateImagesQuery = "UPDATE Images SET isPublic = @isPublic WHERE buildID = @buildID AND userID = @userID";
							using (MySqlCommand command = new MySqlCommand(updateImagesQuery, connection, transaction))
							{
								command.Parameters.AddWithValue("@isPublic", build.isPublic);
								command.Parameters.AddWithValue("@buildID", build.buildID);
								command.Parameters.AddWithValue("@userID", userIDClaim);
								await command.ExecuteNonQueryAsync();
							}
						}

						if (build.tags == null)
						{
							build.tags = "";
						}

						var processedTags = build.tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
							.Select(t => t.ToLower())
							.Distinct()
							.Take(5)
							.ToList();

						var insertTagQuery = "INSERT INTO Tags (tagName, gameID) VALUES (LOWER(@tagName), @gameID) ON DUPLICATE KEY UPDATE tagID = LAST_INSERT_ID(tagID); SELECT LAST_INSERT_ID();";

						var tagLinkValues = new List<string>();
						var parameters = new List<MySqlParameter>();
						int paramIndex = 0;
						foreach (string tag in processedTags)
						{
							int tagID;
							using (MySqlCommand command = new MySqlCommand(insertTagQuery, connection, transaction))
							{
								command.Parameters.AddWithValue("@tagName", tag);
								command.Parameters.AddWithValue("@gameID", build.gameID);
								tagID = Convert.ToInt32(await command.ExecuteScalarAsync());
							}

							var buildIDParam = $"@buildID{paramIndex}";
							var tagIDParam = $"@tagID{paramIndex}";
							tagLinkValues.Add($"({buildIDParam}, {tagIDParam})");
							//tagLinkRecords.Add(tagID);

							parameters.Add(new MySqlParameter(buildIDParam, build.buildID));
							parameters.Add(new MySqlParameter(tagIDParam, tagID));

							paramIndex++;
						}

						// Delete existing tags that are not in the new list
						var deleteTagsQuery = @"DELETE FROM BuildTagLinks WHERE buildID = @buildID;";
						using (MySqlCommand command = new MySqlCommand(deleteTagsQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildID", build.buildID);
							await command.ExecuteNonQueryAsync();
						}

						if (tagLinkValues.Any())
						{
							var insertLinksQuery = $"INSERT INTO BuildTagLinks (buildID, tagID) VALUES {string.Join(", ", tagLinkValues)}";
							using (var command = new MySqlCommand(insertLinksQuery, connection, transaction))
							{
								command.Parameters.AddRange(parameters.ToArray());
								await command.ExecuteNonQueryAsync();
							}
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
								s3ImageFilePath = Path.Combine("Users", userIDClaim.ToString(), "Thumbnails", imageFileName);

								using (var memoryStream = await _imageProcessingService.ResizeAndStripExif(file, 125, 125, true))
								{
									if (_configService.GetEnvironment() != "local")
									{
										// Upload the clean image to S3
										await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath);
									}
									s3Objects.Add(s3ImageFilePath);
								}

								if (_configService.GetEnvironment() != "local")
								{
									await _awsService.DeleteFileFromS3(oldThumbnailPath);
								}
							}
							else if (!string.IsNullOrEmpty(thumbnail))
							{
								string selectedThumbnailPath = Path.Combine(_webHostEnvironment.WebRootPath, "media", "thumbnails", thumbnail);

								if (System.IO.File.Exists(selectedThumbnailPath))
								{
									// Generate a unique file name for the thumbnail copy
									var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(selectedThumbnailPath);
									s3ImageFilePath = Path.Combine("Users", userIDClaim.ToString(), "Thumbnails", imageFileName);

									// Copy the default thumbnail and upload it to the user's S3 folder
									using (var memoryStream = new MemoryStream(await System.IO.File.ReadAllBytesAsync(selectedThumbnailPath)))
									{
										memoryStream.Position = 0;
										if (_configService.GetEnvironment() != "local")
										{
											await _awsService.UploadStreamToS3(memoryStream, s3ImageFilePath); // S3 upload method
										}
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
					catch (Exception ex)
					{
						await transaction.RollbackAsync();
						if (_configService.GetEnvironment() != "local")
						{
							await _awsService.DeleteFilesFromS3(s3Objects);
						}
						Console.WriteLine($"BuildService.cs : EditBuild : Error - {ex.Message}");
						return Json(new { success = false, errorMessage = "Something went wrong" });
					}
				}
			}
		}

		public async Task<IActionResult> CopyBuild(int originalBuildID, JwtSecurityToken token)
		{
			List<String> s3Objects = new List<String>();
			using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
			{
				uint userIDClaim = getUserIDFromToken(token);
				await connection.OpenAsync();

				using (var transaction = await connection.BeginTransactionAsync())
				{
					try
					{
						int newBuildID;
						string originalBuildName;
						uint originalGameID;
						uint originalClassID;
						bool isPublic = false;

						var originalBuildQuery = "SELECT buildName, isPublic, gameID, classID FROM Builds WHERE buildID = @originalBuildID";
						using (MySqlCommand command = new MySqlCommand(originalBuildQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@originalBuildID", originalBuildID);
							using (DbDataReader reader = await command.ExecuteReaderAsync())
							{
								if (await reader.ReadAsync())
								{
									originalBuildName = reader["buildName"]?.ToString();
									isPublic = Convert.ToBoolean(reader["isPublic"]);
									originalGameID = Convert.ToUInt32(reader["gameID"]);
									originalClassID = Convert.ToUInt32(reader["classID"]);
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

						var newBuildQuery = "INSERT INTO Builds (buildName, userID, isPublic, gameID, classID) VALUES (@buildName, @userID, 0, @gameID, @classID); SELECT LAST_INSERT_ID()";
						using (MySqlCommand command = new MySqlCommand(newBuildQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildName", originalBuildName);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							command.Parameters.AddWithValue("@gameID", originalGameID);
							command.Parameters.AddWithValue("@classID", originalClassID);
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
							string newNotePath = Path.Combine("Users", userIDClaim.ToString(), newBuildID.ToString(), "notes.txt");
							var noteQuery = "INSERT INTO Notes (filePath, buildID, userID, isPublic) VALUES (@filePath, @buildID, @userID, 0)";
							using (MySqlCommand command = new MySqlCommand(noteQuery, connection, transaction))
							{
								command.Parameters.AddWithValue("@filePath", newNotePath);
								command.Parameters.AddWithValue("@buildID", newBuildID);
								command.Parameters.AddWithValue("@userID", userIDClaim);

								await command.ExecuteNonQueryAsync();
							}

							if (_configService.GetEnvironment() != "local")
							{
								// Copy Notes Content
								if (!await _awsService.CopyS3ObjectAsync(originalNotePath, newNotePath))
								{
									await transaction.RollbackAsync();
									await _awsService.DeleteFilesFromS3(s3Objects);
									return Json(new { success = false, errorMessage = "Error copying note" });
								}
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
							var newUrlQuery = "INSERT INTO BuildUrls (buildID, buildUrl, buildUrlName, userID, isPublic) VALUES (@buildID, @buildUrl, @buildUrlName, @userID, 0)";
							using (MySqlCommand copyImageCommand = new MySqlCommand(newUrlQuery, connection, transaction))
							{
								copyImageCommand.Parameters.AddWithValue("@buildID", newUrl.buildID);
								copyImageCommand.Parameters.AddWithValue("@buildUrl", newUrl.buildUrl);
								copyImageCommand.Parameters.AddWithValue("@buildUrlName", newUrl.buildUrlName);
								copyImageCommand.Parameters.AddWithValue("@userID", userIDClaim);

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
									string imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(originalFilePath);
									string newFilePath = Path.Combine("Users", userIDClaim.ToString(), "Images", newBuildID.ToString(), imageFileName);

									// Insert new image record
									ImageModel newImage = new ImageModel();
									newImage.buildID = Convert.ToUInt32(newBuildID);
									newImage.filePath = newFilePath;
									newImage.typeID = Convert.ToUInt32(reader["typeID"]);
									newImage.imageOrder = Convert.ToInt32(reader["imageOrder"]);
									newImageList.Add(newImage);

									if (_configService.GetEnvironment() != "local")
									{
										if (!await _awsService.CopyS3ObjectAsync(originalFilePath, newFilePath))
										{
											await transaction.RollbackAsync();
											await _awsService.DeleteFilesFromS3(s3Objects);
											return Json(new { success = false, errorMessage = "Error copying note" });
										}
									}
									s3Objects.Add(newFilePath);
								}
							}
						}

						foreach (var newImage in newImageList)
						{
							var newImageQuery = "INSERT INTO Images (buildID, filePath, typeID, imageOrder, userID, isPublic) VALUES (@buildID, @filePath, @typeID, @imageOrder, @userID, 0)";
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

						await transaction.CommitAsync();
						return Json(new { success = true, newBuildID });
					}
					catch (Exception ex)
					{
						await transaction.RollbackAsync();
						await _awsService.DeleteFilesFromS3(s3Objects);

						Console.WriteLine($"BuildService.cs : CopyBuild : Error - {ex.Message}");
						return Json(new { success = false, errorMessage = "Something went wrong" });
					}
				}
			}
		}

		public async Task<IActionResult> DeleteBuild(int buildID, JwtSecurityToken token)
		{
			using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
			{
				uint userIDClaim = getUserIDFromToken(token);
				await connection.OpenAsync();

				using (var transaction = await connection.BeginTransactionAsync())
				{
					try
					{
						// Check if the build exists and belongs to the user
						var checkBuildQuery = "SELECT COUNT(*) FROM Builds WHERE buildID = @buildID AND userID = @userID";
						using (MySqlCommand command = new MySqlCommand(checkBuildQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildID", buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							int buildCount = Convert.ToInt32(await command.ExecuteScalarAsync());

							if (buildCount != 1)
							{
								return Json(new { success = false, errorMessage = "Build not found or you do not have permission to delete this build." });
							}
						}

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
						var deleteBuildUrlsQuery = "DELETE FROM BuildUrls WHERE buildID = @buildID AND userID = @userID";
						using (MySqlCommand command = new MySqlCommand(deleteBuildUrlsQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildID", buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
							await command.ExecuteNonQueryAsync();
						}

						// Delete from Notes table
						var deleteNotesQuery = "DELETE FROM Notes WHERE buildID = @buildID AND userID = @userID";
						using (MySqlCommand command = new MySqlCommand(deleteNotesQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@buildID", buildID);
							command.Parameters.AddWithValue("@userID", userIDClaim);
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

						string prefix = Path.Combine("Users", userIDClaim.ToString(), buildID.ToString());
						if (_configService.GetEnvironment() != "local")
						{
							await _awsService.BulkDeleteFromS3(prefix);
							await _awsService.DeleteFileFromS3(ThumbnailPath);
						}

						await transaction.CommitAsync();

						return Json(new { success = true });
					}
					catch (Exception ex)
					{
						await transaction.RollbackAsync();
						Console.WriteLine($"BuildService.cs : DeleteBuild : Error - {ex.Message}");
						return Json(new { success = false, errorMessage = "Something went wrong" });
					}
				}
			}
		}

		public async Task<IActionResult> SearchBuilds(int page, string sortBy, IFormCollection request)
		{
			try
			{
				List<BuildSearchModel> builds = new();

                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand("sp_SearchBuilds", connection))
					{
						command.CommandType = CommandType.StoredProcedure;

						string GetFormValue(string key) => request.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;

						var tagList = (GetFormValue("tags") ?? "")
							.ToString()
							.Split(',', StringSplitOptions.RemoveEmptyEntries)
							.Select(t => t.Trim())
							.Where(t => !string.IsNullOrEmpty(t))
							.Take(5)
							.ToArray();

						// Add all parameters to stored procedure
						command.Parameters.AddWithValue("@p_gameID", 
								int.TryParse(GetFormValue("gameId"), out int gameId) ? gameId : 1);

						command.Parameters.AddWithValue("@p_buildName", 
								string.IsNullOrEmpty(GetFormValue("buildName")) ? DBNull.Value : GetFormValue("buildName"));

						command.Parameters.AddWithValue("@p_author", 
								string.IsNullOrEmpty(GetFormValue("author")) ? DBNull.Value : GetFormValue("author"));

						command.Parameters.AddWithValue("@p_classID", 
								int.TryParse(GetFormValue("classId"), out int classId) && classId != 0 ? classId : 0);

						// Add individual tag parameters (null if not provided)
						command.Parameters.AddWithValue("@p_tag1", tagList.Length > 0 ? tagList[0] : DBNull.Value);
						command.Parameters.AddWithValue("@p_tag2", tagList.Length > 1 ? tagList[1] : DBNull.Value);
						command.Parameters.AddWithValue("@p_tag3", tagList.Length > 2 ? tagList[2] : DBNull.Value);
						command.Parameters.AddWithValue("@p_tag4", tagList.Length > 3 ? tagList[3] : DBNull.Value);
						command.Parameters.AddWithValue("@p_tag5", tagList.Length > 4 ? tagList[4] : DBNull.Value);

						command.Parameters.AddWithValue("@p_sortBy", sortBy ?? "newest");
						command.Parameters.AddWithValue("@p_page", page);

						using (DbDataReader reader = await command.ExecuteReaderAsync())
						{
							while (await reader.ReadAsync())
							{
								BuildSearchModel build = new BuildSearchModel
								{
									buildID = Convert.ToUInt32(reader["buildID"]),
									buildName = Convert.ToString(reader["buildName"]),
									userID = Convert.ToUInt32(reader["userID"]),
									imageID = Convert.ToUInt32(reader["imageID"]),
									userName = Convert.ToString(reader["userName"]),
									filePath = Convert.ToString(reader["filePath"]),
									gameName = Convert.ToString(reader["gameName"]),
									className = Convert.ToString(reader["className"]),
									tags = Convert.ToString(reader["tags"]),
									isPublic = true
								};
								builds.Add(build);
							}
						}
					}
				}
				
				return Json(new {success = true, builds});
			}
			catch(Exception ex)
			{
				Console.WriteLine($"BuildService.cs : SearchBuilds : Error - {ex.Message}");
				return Json(new { success = false, errorMessage = "Something went wrong" });
			}			
		}
		public async Task<IActionResult> GetClassesAndTags(int lastTagID = 0, int lastClassID = 0)
		{
			using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
			{
				await connection.OpenAsync();
				try
				{
					List<ClassModel> classes = new List<ClassModel>();

					var classesQuery = "SELECT * FROM Classes WHERE classID > @lastClassID ORDER BY className;";
					using (MySqlCommand command = new MySqlCommand(classesQuery, connection))
					{
						command.Parameters.AddWithValue("@lastClassId", lastClassID);
						using (DbDataReader reader = await command.ExecuteReaderAsync())
							while (reader.Read())
							{
								ClassModel poeClass= new ClassModel();
								poeClass.classID = Convert.ToUInt32(reader["classID"]);
								poeClass.className = reader["className"].ToString();
								poeClass.gameID = Convert.ToUInt32(reader["gameID"]);

								classes.Add(poeClass);
							}
					}

					List<TagModel> tags = new List<TagModel>();
					var tagsQuery = "SELECT * FROM Tags WHERE tagID > @lastTagId ORDER BY tagName;";
					using (MySqlCommand command = new MySqlCommand(tagsQuery, connection))
					{
						command.Parameters.AddWithValue("@lastTagId", lastTagID);
						using (DbDataReader reader = await command.ExecuteReaderAsync())
							while (reader.Read())
							{
								TagModel tag = new TagModel();
								tag.tagID = Convert.ToUInt32(reader["tagID"]);
								tag.tagName = Convert.ToString(reader["tagName"]);
								tags.Add(tag);
							}
					}

					return Json(new {success = true, classes, tags });
				}
				catch (Exception ex)
				{
					Console.WriteLine($"BuildService.cs : GetClassesAndTags : Error - {ex.Message}");
					return Json(new { success = false, errorMessage = "Something went wrong" });
				}
			}
		}
	}
}
