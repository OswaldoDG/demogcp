using Google.Cloud.Functions.Framework;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Net;

namespace Tabrasasa.Serverless.Forms
{
    public class RPMSalesforce : IHttpFunction
    {
        private readonly ILogger _logger;

        public RPMSalesforce(ILogger<RPMSalesforce> logger) =>
            _logger = logger;


        public async Task HandleAsync(HttpContext context)
        {
            _logger.LogInformation($"Authorizing...");
            if (!Authorize(context))
            {
                context.Response.StatusCode = 401;
                return;
            }

            using TextReader reader = new StreamReader(context.Request.Body);
            string text = await reader.ReadToEndAsync();
            if (text.Length > 0)
            {
                try
                {
                    PostBody(text);
                    context.Response.StatusCode = 200;
                    return;
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex.ToString());
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.Message);
                    return;
                }

            }

            context.Response.StatusCode = 400;
        }

        private void PostBody(string xmlMessage)
        {
            string url = Environment.GetEnvironmentVariable("endpoint");
            _logger.LogInformation($"POST to {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(xmlMessage);
            request.Method = "POST";
            request.ContentType = "text/xml;charset=utf-8";
            request.ContentLength = requestBytes.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader respStream = new StreamReader(response.GetResponseStream(), System.Text.Encoding.Default);
            string receivedResponse = respStream.ReadToEnd();
            respStream.Close();
            response.Close();
        }

        private string GetAPIKey(HttpRequest request)
        {
            string token = "";
            string authorization = request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorization)
                && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization["Bearer ".Length..].Trim();
            }
            return token;
        }

        private bool Authorize(HttpContext context)
        {
            string apiKey = GetAPIKey(context.Request);

            string k = Environment.GetEnvironmentVariable("key");
            if (apiKey == k)
            {
                return true;
            }
            else
            {
                _logger.LogInformation($"Invalid key {apiKey}");
            }

            _logger.LogInformation("Not authorized");
            return false;
        }

    }
}
