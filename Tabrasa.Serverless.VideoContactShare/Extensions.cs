using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tabrasa.Serverless.VideoContactShare.Models;

namespace Tabrasa.Serverless.VideoContactShare
{
    public static class Extensions
    {

        public static async Task<long> RetryResult(this Task<long> task, ILogger _logger, string ProcessName, int MaxAttempts = 4) 
        {
            bool posted = false;
            long result = 0;
            int retries = 0;
            while (!posted)
            {
                try
                {
                    _logger.LogInformation($"Processing API {ProcessName} started");
                    result = await task;
                    posted = true;
                    _logger.LogInformation($"Processing API {ProcessName} Done");
                }
                catch (ApiException ex)
                {
                    _logger.LogError(ex.ToString());
                    if (ex.StatusCode != 500)
                    {
                        _logger.LogWarning(ex, $"Finished {ProcessName} with status {ex.StatusCode}");
                        break;
                    }
                    else
                    {
                        _logger.LogInformation($"Retrying ...");
                        await Task.Delay(((int)Math.Pow(2, retries)) * 250);
                        retries++;
                        if (retries == MaxAttempts) break;
                    }
                                      
                }
            }
            return result;
        }

        public static async Task<U> RetryResult<U>(this Task<U> task, ILogger _logger, string ProcessName, int MaxAttempts = 4) where U : class 
        {
            bool posted = false;
            U result = null;
            int retries = 0;
            while (!posted)
            {
                try
                {
                    _logger.LogInformation($"Processing API {ProcessName} started");
                    result = await task;
                    posted = true;
                    _logger.LogInformation($"Processing API {ProcessName} Done");
                }
                catch (ApiException ex)
                {
                    _logger.LogError($"{ex}");
                    if (ex.StatusCode != 500)
                    {
                        _logger.LogWarning(ex, $"Finished {ProcessName} with status {ex.StatusCode}");
                        break;
                    } else
                    {
                        _logger.LogInformation($"Retrying ...");
                        await Task.Delay(((int)Math.Pow(2, retries)) * 250);
                        retries++;
                        if (retries == MaxAttempts) break;
                    }
                }
            }
            return result;
        }


        public static async  Task<bool> Retry(this Task task, ILogger _logger, string ProcessName, int MaxAttempts=4){
            bool posted = false;
            int retries = 0;
            while (!posted)
            {
                try
                {
                    _logger.LogInformation($"Processing API {ProcessName} started");
                    await task;
                    posted = true;
                    _logger.LogInformation($"Processing API {ProcessName} Done");
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode != 500)
                    {
                        _logger.LogWarning(ex, $"Finished {ProcessName} with status {ex.StatusCode}");
                        posted = true;
                        break;
                    }

                    _logger.LogInformation($"Retrying ...");
                    await Task.Delay(((int)Math.Pow(2, retries)) * 250);
                    retries++;
                    if (retries == MaxAttempts) break;
                }
            }
            return posted;
        }

    }
}
