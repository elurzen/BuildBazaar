using Amazon.SimpleSystemsManagement.Model;
using Amazon.SimpleSystemsManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace BuildBazaarCore.Services
{
    public interface IConfigService
    {
        Task InitializeAsync();
        string GetConnectionString();
        string GetConfigValue(string key);
        string GetEnvironment();
    }

    public class ConfigService : IConfigService
    {
        private readonly Dictionary<string, string> _config = new();
        private string _connectionString;
        private readonly Amazon.RegionEndpoint _awsRegion;
        private readonly string _environment;

        public ConfigService()
        {
            
            _environment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "local";

            var region = Environment.GetEnvironmentVariable("AWS_REGION");
            if (region != null)
            {
                _awsRegion = Amazon.RegionEndpoint.GetBySystemName(region);
            }
            else
            {
                _awsRegion = Amazon.RegionEndpoint.USEast1;
            }
            
        }

        public async Task InitializeAsync()
        {
            var client = new AmazonSimpleSystemsManagementClient(_awsRegion);

            var parameterNames = new List<string>
            {
                "REGION",
                "DB_HOST",
                "DB_PORT",
                "DB_NAME",
                "DB_USERNAME",
                "DB_PASSWORD",
                "JWT_ISSUER",
                "JWT_AUDIENCE",
                "SECRET_KEY",
                "BUCKET_NAME",
                "CLOUDFRONT_PRIVATE_KEY",
                "CLOUDFRONT_KEY_PAIR_ID",
                "CLOUDFRONT_DISTRIBUTION_DOMAIN"
            };

            if (_environment == "local")
            {
                foreach (string parameter in parameterNames)
                {
                    _config[parameter] = Environment.GetEnvironmentVariable(parameter);
                }
                //no SSL
                _connectionString = $"server={_config["DB_HOST"]};port={_config["DB_PORT"]};database={_config["DB_NAME"]};user={_config["DB_USERNAME"]};password={_config["DB_PASSWORD"]};SslMode=none;";
                return;
            }

            foreach (var parameter in parameterNames)
            {
                try
                {
                    var request = new GetParameterRequest
                    {
                        Name = $"{parameter}_{_environment}",
                        WithDecryption = true
                    };
                    var response = await client.GetParameterAsync(request);
                    _config[parameter] = response.Parameter.Value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ConfigService.cs : Error getting parameter {parameter}: {ex.Message}");
                }
            }


            string sslCertPath = Path.Combine("/app", "Certificates", "us-east-1-bundle.pem");
            if (!File.Exists(sslCertPath))
            {
                throw new FileNotFoundException($"SSL certificate not found at {sslCertPath}");
            }

            //VerifyCA SSL
            _connectionString = $"server={_config["DB_HOST"]};port={_config["DB_PORT"]};database={_config["DB_NAME"]};user={_config["DB_USERNAME"]};password={_config["DB_PASSWORD"]};SslMode=VerifyCA;CertificateFile={sslCertPath};";
        }

        public string GetConnectionString()
        {

            return _connectionString;
        }

        public string GetConfigValue(string key)
        {
            return _config.ContainsKey(key) ? _config[key] : null;
        }

        public string GetEnvironment()
        {
            return _environment;
        }
    }
}

