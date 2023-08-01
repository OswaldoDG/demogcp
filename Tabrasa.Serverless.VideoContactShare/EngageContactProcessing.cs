using Consumer.VipIntegration;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tabrasa.Serverless.VideoContactShare.Models;
using TabrasaVideo.Internal;
using URLA.VipIntegration;

namespace Tabrasa.Serverless.VideoContactShare;

public partial class VideoContactShareGCF
{

    public async Task ProcessContactsEngage(TopicVideoShareMessage shareData, VideoDataFull video)
    {

        var client = new Tabrasa_URLA_VipIntegrationClient(this.CreateClient())
        {
            BaseUrl = Environment.GetEnvironmentVariable("URLAEndpoint")
        };

        if (client != null)
        {
            _logger.LogInformation($"Posting to {client.BaseUrl} {JsonConvert.SerializeObject(shareData)}");

            var leads = await client.ByIdList2Async(shareData.ContactRecipientIds, shareData.UserId, shareData.ClientId);
            _logger.LogInformation($"{leads.Count} leads returned ");


            var consumers = await GetConsumers(leads.ToList(), shareData.AppScope, shareData);
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




    /// <summary>
    /// Create a new consumer
    /// </summary>
    /// <param name="lead"></param>
    /// <param name="AppScope"></param>
    /// <param name="data"></param>
    /// <returns></returns>

    private async Task<ConsumerExtended> CreateConsumer(LeadSummary lead, string AppScope, TopicVideoShareMessage data)
    {
        _logger.LogInformation($"Creating new consumer {lead.Email}");

        HttpClient cl = new HttpClient();
        cl.SetBearerToken(JWT);
        var client = new Consumer.VipIntegration.Tabrasa_ConsumerIntegrationClient(cl)
        {
            BaseUrl = Environment.GetEnvironmentVariable("ConsumerEndpoint")
        };

        try
        {
            string phone = "";

            try
            {
                if (lead.Phones != null)
                {
                    if (lead.Phones.Count > 0)
                    {
                        var p = lead.Phones.FirstOrDefault(x => x.IsPrimary == true);
                        if (p != null)
                        {
                            phone = p.PhoneNumber;
                        }
                        else
                        {
                            phone = lead.Phones.ToList()[0].PhoneNumber;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            List<ExternalSystemKey> keys = new List<ExternalSystemKey>()
                    {
                        new ExternalSystemKey()
                        {
                              LoanOfficerKey = data.UserId.ToString(),
                              SourceSystemKey = "VIP",
                        }
                    };

            var newConsumer = new Consumer.VipIntegration.ConsumerExtended()
            {
                EmailAddress = lead.Email,
                ConsumerScope = AppScope,
                FirstName = lead.FullName.First,
                LastName = lead.FullName.Last,
                IsActive = true,
                MiddleName = lead.FullName.Middle,
                Phone = phone,
                ExternalSystemKeys = keys,
            };
            _logger.LogInformation($"{JsonConvert.SerializeObject(newConsumer)}");
            var consumer = await client.AddConsumerAsync(newConsumer);

            return consumer;
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"{ex}");
            throw ex;
        }
    }

    /// <summary>
    /// GEt the consumers for the share
    /// </summary>
    /// <param name="leads"></param>
    /// <param name="AppScope"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    private async Task<List<ConsumerExtended>> GetConsumers(List<LeadSummary> leads, string AppScope, TopicVideoShareMessage data)
    {
        List<Consumer.VipIntegration.ConsumerExtended> consumers = new List<Consumer.VipIntegration.ConsumerExtended>();


        _logger.LogInformation($"{JWT} ");
        List<Task<Consumer.VipIntegration.ConsumerExtended>> consumersTask = new List<Task<Consumer.VipIntegration.ConsumerExtended>>();
        foreach (LeadSummary lead in leads)
        {
            HttpClient cl = new HttpClient();
            cl.SetBearerToken(JWT);
            var client = new Consumer.VipIntegration.Tabrasa_ConsumerIntegrationClient(cl)
            {
                BaseUrl = Environment.GetEnvironmentVariable("ConsumerEndpoint")
            };

            try
            {
                _logger.LogInformation($"{JsonConvert.SerializeObject(lead)} ");
                var r = await client.ByexternalidAsync(data.UserId.ToString(), "VIP", null, lead.Email, AppScope);
                if (r.Count > 0)
                {
                    consumers.Add(r.ToList()[0]);
                }
                else
                {
                    _logger.LogInformation($"Not found {lead.Email}");
                    var newConsumer = await CreateConsumer(lead, AppScope, data);
                    consumers.Add(newConsumer);
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
