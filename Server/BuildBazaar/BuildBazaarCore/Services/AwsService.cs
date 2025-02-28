using Amazon.CloudFront;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Amazon.SimpleEmailV2.Model;
using Amazon.SimpleEmailV2;
using System.Data;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BuildBazaarCore.Services
{
    public interface IAwsService
    {
        Task<string> UploadFileToS3(string filePath, string fileName);
        Task<string> UploadStreamToS3(Stream stream, string fileName);
        Task<bool> CopyS3ObjectAsync(string sourceKey, string destinationKey);
        Task<bool> DeleteFileFromS3(string fileName);
        Task<bool> DeleteFilesFromS3(List<string> fileNames);
        Task BulkDeleteFromS3(string prefix);
        IActionResult GenerateCloudFrontSignedUrl(string filePath);
        Task<IActionResult> GetBulkImageUrls(List<string> imageIDs, JwtSecurityToken token);
        Task SendEmail(string toEmail, string subject, string body);
    }

    public class AwsService : BaseService, IAwsService
    {
        private readonly IConfigService _configService;
        private readonly IAmazonS3 _s3Client;

        public AwsService(IConfigService configService, IAmazonS3 s3Client)
        {            
            _configService = configService;            
            _s3Client = s3Client;
        }

        public async Task<string> UploadFileToS3(string filePath, string fileName)
        {
            var fileTransferUtility = new TransferUtility(_s3Client);
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            if ((fileStream.Length / 1024 / 1024) > 10) //size in MB
            {
                throw new InvalidOperationException("File size exceeds the maximum allowed limit.");
            }

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = _configService.GetConfigValue("BUCKET_NAME")
            };

            await fileTransferUtility.UploadAsync(uploadRequest);
            return $"https://{_configService.GetConfigValue("BUCKET_NAME")}.s3.amazonaws.com/{fileName}";
        }

        public async Task<string> UploadStreamToS3(Stream stream, string fileName)
        {
            if ((stream.Length / 1024 / 1024) > 10) //size in MB
            {
                throw new InvalidOperationException("File size exceeds the maximum allowed limit.");
            }
            var fileTransferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = fileName,
                BucketName = _configService.GetConfigValue("BUCKET_NAME"),
            };

            await fileTransferUtility.UploadAsync(uploadRequest);
            return $"https://{_configService.GetConfigValue("BUCKET_NAME")}.s3.amazonaws.com/{fileName}";
        }

        public async Task<bool> CopyS3ObjectAsync(string sourceKey, string destinationKey)
        {
            try
            {
                var copyRequest = new CopyObjectRequest
                {
                    SourceBucket = _configService.GetConfigValue("BUCKET_NAME"),
                    SourceKey = sourceKey,
                    DestinationBucket = _configService.GetConfigValue("BUCKET_NAME"),
                    DestinationKey = destinationKey
                };

                CopyObjectResponse response = await _s3Client.CopyObjectAsync(copyRequest);

                return response.HttpStatusCode == HttpStatusCode.OK;

            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error encountered on server. Message: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown error encountered. Message: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteFileFromS3(string fileName)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _configService.GetConfigValue("BUCKET_NAME"),
                    Key = fileName
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                return true; // File deleted successfully
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Console.WriteLine($"Error deleting file from S3: {ex.Message}");
                return false; // Return false if deletion fails
            }
        }

        public async Task<bool> DeleteFilesFromS3(List<string> fileNames)
        {
            if (fileNames.Count == 0 || fileNames == null)
            {
                return true;
            }
            try
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _configService.GetConfigValue("BUCKET_NAME"),
                    Objects = fileNames.Select(fileName => new KeyVersion { Key = fileName }).ToList(),
                    ChecksumAlgorithm = ChecksumAlgorithm.FindValue("MD5")
                };
                var response = await _s3Client.DeleteObjectsAsync(deleteRequest);

                if (response.DeleteErrors.Any())
                {
                    foreach (var error in response.DeleteErrors)
                    {
                        Console.WriteLine($"Failed to delete {error.Key}: {error.Message}");
                    }
                    return false; // Indicate failure due to partial errors
                }

                return true; // All files deleted successfully
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error)
                Console.WriteLine($"Error deleting file from S3: {ex.Message}");
                return false; // Return false if deletion fails
            }
        }

        public async Task BulkDeleteFromS3(string prefix)
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _configService.GetConfigValue("BUCKET_NAME"),
                Prefix = prefix // e.g., "user/builds/35/"
            };

            ListObjectsV2Response listResponse;

            do
            {
                // Get a list of all objects with the specified prefix
                listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                if (listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _configService.GetConfigValue("BUCKET_NAME"),
                        Objects = listResponse.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList(),
                        ChecksumAlgorithm = ChecksumAlgorithm.SHA1
                    };

                    // Delete the objects
                    var deleteResponse = await _s3Client.DeleteObjectsAsync(deleteRequest);
                }

                // Continue if there are more objects to list
                listRequest.ContinuationToken = listResponse.NextContinuationToken;

            } while (listResponse.IsTruncated); // Loop if not all objects are retrieved in one call
        }

        public IActionResult GenerateCloudFrontSignedUrl(string filePath)
        {
            string distributionDomain = _configService.GetConfigValue("CLOUDFRONT_DISTRIBUTION_DOMAIN");
            string keyPairId = _configService.GetConfigValue("CLOUDFRONT_KEY_PAIR_ID"); ; // CloudFront Key Group ID

            string fullUrl = $"https://{distributionDomain}/{filePath}";
            StringReader privateKeyReader = new StringReader(_configService.GetConfigValue("CLOUDFRONT_PRIVATE_KEY"));

            try
            {
                string signedUrl = AmazonCloudFrontUrlSigner.GetCannedSignedURL(
                   fullUrl,
                   privateKeyReader,
                   keyPairId,
                   DateTime.UtcNow.AddDays(1)
                );
                return Json(new { success = true, url = signedUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }

        public async Task<IActionResult> GetBulkImageUrls(List<string> imageIDs, JwtSecurityToken token)
        {

            if (imageIDs == null || !imageIDs.Any())
            {
                return Json(new { success = false, errorMessage = "Image request(s) are missing or invalid in the request body." });
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_configService.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    List<object> urls = new List<object>();
                    string distributionDomain = _configService.GetConfigValue("CLOUDFRONT_DISTRIBUTION_DOMAIN");
                    string keyPairId = _configService.GetConfigValue("CLOUDFRONT_KEY_PAIR_ID"); ; // CloudFront Key Group ID
                    var imageListParameterNames = imageIDs.Select((id, index) => $"@imageID{index}").ToArray();
                    string imagesQuery = $@"
                    SELECT Images.filePath
                    FROM Images
                    JOIN Builds ON Images.buildID = Builds.buildID
                    WHERE Images.imageID IN ({string.Join(",", imageListParameterNames)})
                    ";

                    if (token == null)
                    {
                        imagesQuery += "AND Builds.isPublic = 1";
                    }
                    else
                    {
                        imagesQuery += "AND (Builds.isPublic = 1 OR Builds.userID = @userID)";
                    }

                    using (MySqlCommand command = new MySqlCommand(imagesQuery, connection))
                    {
                        if (token != null)
                        {
                            var userIDClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                            command.Parameters.AddWithValue("@userID", userIDClaim);
                        }
                        for (int i = 0; i < imageIDs.Count; i++)
                        {
                            command.Parameters.AddWithValue(imageListParameterNames[i], imageIDs[i]);
                        }

                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                using (var privateKeyReader = new StringReader(_configService.GetConfigValue("CLOUDFRONT_PRIVATE_KEY")))
                                {
                                    var filePath = reader["filePath"]?.ToString();
                                    string fullUrl = $"https://{distributionDomain}/{filePath}";
                                    string signedUrl = AmazonCloudFrontUrlSigner.GetCannedSignedURL(
                                        fullUrl,
                                        privateKeyReader,
                                        keyPairId,
                                        DateTime.UtcNow.AddDays(1)
                                    );

                                    urls.Add(new { filePath, url = signedUrl });
                                }
                            }
                            return Json(new { success = true, urls });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }
        
        public async Task SendEmail(string toEmail, string subject, string body)
        {
            var client = new AmazonSimpleEmailServiceV2Client(Amazon.RegionEndpoint.GetBySystemName(
                _configService.GetConfigValue("REGION") ?? Amazon.RegionEndpoint.USEast1.SystemName));

            var sendRequest = new SendEmailRequest
            {
                FromEmailAddress = "noreply@BuildBazaar.net",
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content { Data = subject },
                        Body = new Body
                        {
                            Html = new Content { Data = body } // Use Html or Text content as needed
                        }
                    }
                }
            };

            try
            {
                var response = await client.SendEmailAsync(sendRequest);
                Console.WriteLine("Email sent! Message ID: " + response.MessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
            }
        }
    }
}
