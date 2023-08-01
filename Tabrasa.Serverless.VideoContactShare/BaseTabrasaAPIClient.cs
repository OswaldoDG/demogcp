using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tabrasa.Serverless.VideoContactShare
{
    public class BaseTabrasaAPIClient
    {
        protected readonly ILogger _logger;
        protected string jwtClientSecret = null;
        protected string apiKeySecret = null;
        protected string JWT;
        protected HttpClient client;

        public BaseTabrasaAPIClient(ILogger logger)
        {
            _logger = logger;
        }

        
        protected HttpClient CreateClient()
        {

            if (string.IsNullOrEmpty(JWT))
            {
                _logger.LogInformation($"Error creating access token");
                return null;
            }

            client = new HttpClient();
            client.SetBearerToken(JWT);

            return client;
        }

        public async Task<string> GetAccessToken(string ClientSecret)
        {
            var client = new HttpClient();
            var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = Environment.GetEnvironmentVariable("TokenEndpoint"),
                ClientId = Environment.GetEnvironmentVariable("TokenClientId"),
                ClientSecret = ClientSecret
            });

            if (response.IsError)
            {
                _logger.LogWarning(response.Exception, $"Error getting JWT Token {response.HttpStatusCode}");
            }
            return response.IsError ? null : response.AccessToken;
        }

        protected async Task<bool> GetSecrets(SecretReader secrets)
        {
            Task<string> t = secrets.GetTokenClientSecret();
            jwtClientSecret = await t.RetryResult(_logger, $"Getting JWT Client Secret");
            if (string.IsNullOrEmpty(jwtClientSecret))
            {
                _logger.LogInformation($"Can't get Client Secret");
                return false;
            }

            t = GetAccessToken(jwtClientSecret);
            JWT = await t.RetryResult(_logger, $"Creating JWT");
            _logger.LogInformation($"= {JWT}");
            if (string.IsNullOrEmpty(JWT))
            {
                _logger.LogInformation($"Can't get a JWT");
                return false;
            }

            return true;
        }
    
    
    }
}
