using Consumer.VipIntegration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tabrasa.Serverless.VideoContactShare.Models;
using TabrasaVideo.Internal;
using URLA.VipIntegration;

namespace Tabrasa.Serverless.VideoContactShare
{
    public partial class VideoContactShareGCF
    {

        public async Task ProcessContacts(TopicVideoShareMessage shareData, VideoDataFull video)
        {
            var client = new Tabrasa_URLA_VipIntegrationClient(this.CreateClient())
            {
                BaseUrl = Environment.GetEnvironmentVariable("URLAEndpoint")
            };

            var contacts = await VerifyContacts(shareData.ContactRecipientIds, shareData.PartnerRecipientIds, shareData.UserId, shareData.ClientId);
            var origContactCount = (shareData.ContactRecipientIds?.Count ?? 0) + (shareData.PartnerRecipientIds?.Count ?? 0);
            if (contacts.Count != origContactCount)
            {
                _logger.LogWarning($"Request Data Mismatch: Request was to share video with {origContactCount} contacts, but confirmed {contacts.Count} contacts instead");
            }

            if (contacts.Count > 0)
            {
                _logger.LogInformation($"Preparing to share video {video.VideoId} with {contacts.Count} confirmed contacts");
                bool modified = false;
                foreach (var contact in contacts)
                {
                    _logger.LogInformation($"{JsonSerializer.Serialize(contact)}");

                    if (!string.IsNullOrWhiteSpace(contact.Email))
                    {
                        if (contact.ContactType == GlobalContactType.Lead && !(video.ContactShares?.Any(cs => cs.ContactId == contact.ContactId && cs.SharingUserId == shareData.UserId) ?? false))
                        {
                            video.ContactShares.Add(new ContactShare { ContactId = contact.ContactId, SharingUserId = shareData.UserId });
                            modified = true;
                        }
                        else if (contact.ContactType == GlobalContactType.Partner && !video.PartnerShares.Any(cs => cs.ContactId == contact.ContactId && cs.SharingUserId == shareData.UserId))
                        {
                            video.PartnerShares.Add(new ContactShare { ContactId = contact.ContactId, SharingUserId = shareData.UserId });
                            modified = true;
                        }
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

                foreach (var contact in contacts)
                {
                    await SendInvitation(shareData, contact, video);
                }
            }

        }


        public async Task<List<GlobalContactSummary>> VerifyContacts(List<long> contactIds, List<long> partnerIds, int subscriberId, int subscriptionId)
        {
            var client = new Tabrasa_URLA_VipIntegrationClient(this.CreateClient())
            {
                BaseUrl = Environment.GetEnvironmentVariable("URLAEndpoint")
            };

            // First, convert to global contact IDs for the get
            var globalContactIds = new List<GlobalContactIdResult>();
            if (contactIds?.Any() ?? false)
            {
                globalContactIds.AddRange(contactIds.Select(c => new GlobalContactIdResult { Cid = c, CType = GlobalContactType.Lead }));
            }
            if (partnerIds?.Any() ?? false)
            {
                globalContactIds.AddRange(partnerIds.Select(c => new GlobalContactIdResult { Cid = c, CType = GlobalContactType.Partner }));
            }

            var results = new List<GlobalContactSummary>();
            if (globalContactIds.Any())
            {
                results = (await client.ByIdListAsync(subscriberId, subscriptionId, globalContactIds )).ToList();
            }
            return results;
        }
    }
}
