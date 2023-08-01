using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Tabrasa.Serverless.VideoContactShare
{
    public class SecretReader
    {

        private readonly ILogger _logger;
        public SecretReader(ILogger logger)
        {
            _logger = logger;
        }


        public async Task<string> GetTokenClientSecret()
        {
#if DEBUG
            return Environment.GetEnvironmentVariable("TokenClientSecret");
#endif

            SecretManagerServiceClient secrets = SecretManagerServiceClient.Create();
            AccessSecretVersionResponse result = null;
            try
            {
                result = await secrets.AccessSecretVersionAsync(Environment.GetEnvironmentVariable("TokenClientSecret"));
                _logger.LogInformation($"JWT Client Token found");
            }
            catch (Exception ex)
            {

                _logger.LogInformation($"{ex}");
            }

            return result?.Payload.Data.ToStringUtf8();
        }

        public async Task<string> GetRedisConnectionString()
        {

#if DEBUG
            return Environment.GetEnvironmentVariable("RedisServerSecret");
#endif

            SecretManagerServiceClient secrets = SecretManagerServiceClient.Create();
            AccessSecretVersionResponse result = null;
            try
            {
                result = await secrets.AccessSecretVersionAsync(Environment.GetEnvironmentVariable("RedisServerSecret"));
                string ConnStr = result?.Payload.Data.ToStringUtf8();
                return ConnStr;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                return null;
            }
        }

        public async Task<string> GetAPIKeySecret()
        {
            SecretManagerServiceClient secrets = SecretManagerServiceClient.Create();
            AccessSecretVersionResponse result = null;
#if DEBUG
            return Environment.GetEnvironmentVariable("URLAIntegrationAPIKeySecretName");
#endif

            try
            {
                result = await secrets.AccessSecretVersionAsync(Environment.GetEnvironmentVariable("URLAIntegrationAPIKeySecretName"));
                _logger.LogInformation($"API Key found");
                return result?.Payload.Data.ToStringUtf8();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                return null;
            }
        }
    }
}
