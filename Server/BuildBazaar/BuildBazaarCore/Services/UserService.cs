using BuildBazaarCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.Common;

namespace BuildBazaarCore.Services
{
    public interface IUserService
    {
        JwtSecurityToken ValidateToken(string token);
        Task<IActionResult> CreateUser(UserModel user);
        Task<IActionResult> Login(UserModel user);
        Task<IActionResult> RequestPasswordReset(string email, string host);
        Task<IActionResult> UpdatePasswordWithToken(string password, string token);
    }
    public class UserService : BaseService, IUserService
    {
        private readonly IConfigService _configService;
        private readonly IBuildService _buildService;
        private readonly IAwsService _awsService;
        public UserService(IConfigService configService, IBuildService buildService, IAwsService awsService)
        {
            _configService = configService;
            _buildService = buildService;
            _awsService = awsService;
        }

        public JwtSecurityToken ValidateToken(string token)
        {
            //var token = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            try
            {
                // Validate token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configService.GetConfigValue("SECRET_KEY"));
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configService.GetConfigValue("JWT_ISSUER"),
                    ValidAudience = _configService.GetConfigValue("JWT_AUDIENCE"),
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                return (JwtSecurityToken)validatedToken;
            }
            catch (Exception ex)
            {
				Console.WriteLine($"UserService.cs : ValidateToken : Error - {ex.Message}");
                return null;
            }

        }
        
        public async Task<IActionResult> CreateUser(UserModel user)
        {
            try
            {
                string errorMessage = "";
                if (!Regex.IsMatch(user.username, "^[a-zA-Z0-9_.-]+$"))
                {
                    errorMessage += "Username may only contain letters, numbers, underscores, hyphens, or periods";
                }
                if (user.password.Length < 8)
                {
                    if (errorMessage != "")
                    {
                        errorMessage += " and ";
                    }
                    errorMessage += "Password must be at least 8 characters long";
                }
                if (user.password.Length > 100)
                {
                    if (errorMessage != "")
                    {
                        errorMessage += " and ";
                    }
                    errorMessage += "Password must be at most 100 characters long";
                }
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.password);
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // check if email already exists
                    string query = "SELECT COUNT(*) FROM Users WHERE UPPER(email) = @email";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@email", user.email.ToUpper());
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // email already exists, return error message
                            if (errorMessage != "")
                            {
                                errorMessage += " and ";
                            }
                            errorMessage += "Email already registered";
                        }
                    }

                    query = "SELECT COUNT(*) FROM Users WHERE UPPER(username) = @username";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", user.username.ToUpper());
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            // username already exists, return error message
                            if (errorMessage != "")
                            {
                                errorMessage += " and ";
                            }
                            errorMessage += "Username already taken";
                        }
                    }
                    if (errorMessage != "")
                    {
                        return Json(new { success = false, errorMessage });
                    }

                    //add user to db
                    query = "INSERT INTO Users (username, email, password) VALUES (@username, @email, @password)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", user.username);
                        command.Parameters.AddWithValue("@email", user.email);
                        command.Parameters.AddWithValue("@password", hashedPassword);
                        await command.ExecuteScalarAsync();
                    }

                    // Retrieve the userID of the newly created user
                    int userID;
                    query = "SELECT LAST_INSERT_ID()";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        userID = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    // Create an example build for the new user
                    await _buildService.CreateExampleBuild(userID, connection);
                }

                return Json(new { success = true });
            }
            catch (MySqlException ex)
            {
				Console.WriteLine($"UserService.cs : CreateUser : Error - {ex.Message}");
                return Json(new { success = false, errorMessage = "Something went wrong" });
            }
        }

        public async Task<IActionResult> Login(UserModel user)
        {
            if (user.username == null || user.password == null)
            {
                return Json(new { success = false, errorMessage = "Enter a username and password" });
            }

            try
            {
                // Get user from database using username or email
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Users WHERE UPPER(username) = @username";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", user.username.ToUpper());


                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Verify password using bcrypt
                                if (BCrypt.Net.BCrypt.Verify(user.password, reader["password"]?.ToString()))
                                {
                                    // Create claims for the user
                                    var claims = new[]
                                    {
                                        new Claim(ClaimTypes.NameIdentifier, reader["userID"]?.ToString()),
                                        new Claim(ClaimTypes.Name, reader["username"]?.ToString()),
                                        new Claim(ClaimTypes.Email, reader["email"]?.ToString())
                                    };

                                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configService.GetConfigValue("SECRET_KEY")));
                                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                                    // Generate token
                                    var token = new JwtSecurityToken(
                                        issuer: _configService.GetConfigValue("JWT_ISSUER"),
                                        audience: _configService.GetConfigValue("JWT_AUDIENCE"),
                                        claims: claims,
                                        expires: DateTime.Now.AddDays(30),
                                        signingCredentials: creds);

                                    return Json(new { success = true, token = new JwtSecurityTokenHandler().WriteToken(token), username = reader["username"].ToString() });
                                }
                            }
                        }
                    }
                }
                // Login failed, return error message
                return Json(new { success = false, errorMessage = "Invalid username or password." });
            }
            catch (MySqlException ex)
            {
				Console.WriteLine($"UserService.cs : Login : Error - {ex.Message}");
                return Json(new { success = false, errorMessage = "Something went wrong" });
            }
        }

        public async Task<IActionResult> RequestPasswordReset(string email, string host)
        {
            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                await connection.OpenAsync();
                int userID;
                string username;

                // Check if the email exists
                string query = "SELECT userID, userName FROM Users WHERE email = @Email LIMIT 1";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            username = reader["userName"]?.ToString();
                            userID = Convert.ToInt32(reader["userID"]);
                        }
                        else
                        {
                            return Json(new { success = true, message = "If the email is registered, you will receive a reset link." });
                        }
                    }
                }
                // Generate a secure token
                string resetToken = Guid.NewGuid().ToString();
                DateTime expiration = DateTime.UtcNow.AddMinutes(15);

                // Store the token in the database
                string tokenQuery = "INSERT INTO PasswordResets (userID, resetToken, expiration) VALUES (@userID, @resetToken, @expiration)";
                using (MySqlCommand command = new MySqlCommand(tokenQuery, connection))
                {
                    command.Parameters.AddWithValue("@userID", userID);
                    command.Parameters.AddWithValue("@resetToken", resetToken);
                    command.Parameters.AddWithValue("@expiration", expiration);
                    await command.ExecuteNonQueryAsync();
                }

				try
				{
                // Send the email with the reset link
					string resetLink = $"https://{host}/ResetPassword/{resetToken}";
					await _awsService.SendEmail(email, "Password Reset", $"Username: {username}<br><br>Click the link to reset your password: {resetLink}");

					return Json(new { success = true, message = "If the email is registered, you will receive a reset link." });
				}
				catch(Exception ex)
				{
					Console.WriteLine($"UserService.cs : RequestPasswordReset : Error - {ex.Message}");
					return Json(new { success = false, message = "An error occurred while sending the email." });
				}
            }
        }

        public async Task<IActionResult> UpdatePasswordWithToken(string password, string token)
        {
            using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                        uint userID, passwordResetID;
                        DateTime expirationTime;

                        // Check if the email exists
                        string query = "SELECT passwordResetID, expiration, userID FROM PasswordResets WHERE resetToken = @resetToken LIMIT 1";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@resetToken", token);
                            using (DbDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    passwordResetID = Convert.ToUInt32(reader["passwordResetID"]);
                                    userID = Convert.ToUInt32(reader["userID"]);
                                    expirationTime = Convert.ToDateTime(reader["expiration"]);
                                }
                                else
                                {
                                    return Json(new { success = false, errorMessage = "Invalid Reset Request" });
                                }
                            }
                        }

                        if (expirationTime <= DateTime.UtcNow)
                        {
                            return Json(new { success = false, errorMessage = "The reset token has expired" });
                        }

                        // Token is valid, update the user's password
                        string updateQuery = "UPDATE Users SET password = @hashedPassword WHERE userID = @userID LIMIT 1";
                        using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@hashedPassword", hashedPassword);
                            command.Parameters.AddWithValue("@userID", userID);
                            await command.ExecuteNonQueryAsync();
                        }

                        // Optionally, delete the used token
                        string deleteQuery = "DELETE FROM PasswordResets WHERE resetToken = @resetToken";
                        using (MySqlCommand command = new MySqlCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@resetToken", token);
                            await command.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                        return Json(new { success = true, message = "Password has been updated successfully." });
                    }
                    catch (Exception ex)
                    {
						Console.WriteLine($"UserService.cs : UpdatePasswordWithToken : Error - {ex.Message}");
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

            }
        }
    }
}

