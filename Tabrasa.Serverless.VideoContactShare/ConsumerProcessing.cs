using Consumer.VipIntegration;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tabrasa.Serverless.VideoContactShare.Models;
using TabrasaVideo.Internal;

namespace Tabrasa.Serverless.VideoContactShare
{
    public partial class VideoContactShareGCF
    {

        public async Task ProcessConsumers(TopicVideoShareMessage shareData, VideoDataFull video)
        {
            List<Guid> consumerIds = shareData.ConsumerRecipentIds;
            if ((consumerIds?.Count ?? 0) > 0)
            {
                _logger.LogInformation($"Preparing to share video {video.VideoId} with {consumerIds.Count} consumers");


                // Search for consumers
                List<ConsumerExtended> consumers = await GetConsumers(shareData.ConsumerRecipentIds, shareData.AppScope, shareData);

                _logger.LogInformation($"{consumers.Count} consumers returned ");
                if (consumers.Count > 0)
                {
                    bool modified = false;
                    video.ConsumerShares ??= new List<ConsumerShare>();
                    foreach (var consumer in consumers)
                    {
                        if (!video.ConsumerShares.Any(c => c.ConsumerId == consumer.ConsumerId))
                        {
                            modified = true;
                            _logger.LogInformation($"Adding {consumer.ConsumerId}");
                            video.ConsumerShares.Add(new ConsumerShare()
                            {
                                ConsumerId = consumer.ConsumerId,
                                SharingUserId = shareData.UserId
                            });
                        }
                    }

                    if (modified)
                    {
                        await UpdateVideo(video);
                    }
                    else
                    {
                        _logger.LogInformation($"No new shares");
                    }

                    foreach (var consumer in consumers)
                    {
                        await SendConsumerInvitation(shareData, consumer, video);
                    }

                }
                else
                {
                    _logger.LogInformation($"No consumers found");
                }
            }
        }


        private async Task<List<ConsumerExtended>> GetConsumers(List<Guid> consumerIds, string AppScope, TopicVideoShareMessage data)
        {
            List<Consumer.VipIntegration.ConsumerExtended> consumers = new List<Consumer.VipIntegration.ConsumerExtended>();

            List<Task<Consumer.VipIntegration.ConsumerExtended>> consumersTask = new List<Task<Consumer.VipIntegration.ConsumerExtended>>();
            foreach (Guid id in consumerIds)
            {
                HttpClient cl = new HttpClient();
                cl.SetBearerToken(JWT);
                var client = new Consumer.VipIntegration.Tabrasa_ConsumerIntegrationClient(cl)
                {
                    BaseUrl = Environment.GetEnvironmentVariable("ConsumerEndpoint")
                };

                try
                {
                    _logger.LogInformation($"Getting consumer {id}");
                    var r = await client.GetConsumerByIdAsync(id);
                    if (r!=null)
                    {
                        consumers.Add(r);
                    }
                    else
                    {
                        _logger.LogInformation($"Not found {id}");
                    }

                }
                catch (ApiException ex)
                {
                    _logger.LogInformation($"{ex}");

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"{ex}");
                    throw ex;
                }
            }

            return consumers;
        }

    }
}
