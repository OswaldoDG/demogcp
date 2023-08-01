using CloudNative.CloudEvents;
using Consumer.VipIntegration;
using Google.Cloud.Functions.Framework;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tabrasa.Serverless.VideoContactShare.Models;
using TabrasaVideo.Internal;
using URLA.VipIntegration;

namespace Tabrasa.Serverless.VideoContactShare
{
    public partial class VideoContactShareGCF : BaseTabrasaAPIClient, ICloudEventFunction<MessagePublishedData>
    {
        public VideoContactShareGCF(ILogger<VideoContactShareGCF> logger) : base(logger)
        {
            logger.LogInformation($"Starting");
        }


        public async Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
        {
            string textMessage = data.Message?.TextData;
            try
            {
                var shareData = JsonConvert.DeserializeObject<TopicVideoShareMessage>(textMessage);


                _logger.LogInformation($"{JsonConvert.SerializeObject(shareData)}");


                try
                {

                    _logger.LogInformation($"Creating JWT");
                    SecretReader secrets = new SecretReader(_logger);
                    bool validKeys = await GetSecrets(secrets);
                    if (!validKeys)
                    {
                        return;
                    }

                    _logger.LogInformation($"Getting the video data");

                    var video = await GetVideoById(shareData.VideoId);

                    if (video != null)
                    {
                        video.ConsumerShares ??= new List<ConsumerShare>();
                        video.ContactShares ??= new List<ContactShare>();
                        video.PartnerShares ??= new List<ContactShare>();

                        if (shareData.InvitationSource == Invitation.Integration.ConsumerVideoInvitationRequestSource.Engage
                        && shareData.ContactRecipientIds != null && shareData.ContactRecipientIds.Count > 0)
                        {
                            await ProcessContactsEngage(shareData, video);
                        }

                        if (shareData.InvitationSource == Invitation.Integration.ConsumerVideoInvitationRequestSource.Email
                            && shareData.ConsumerRecipentIds != null && shareData.ConsumerRecipentIds.Count > 0)
                        {
                            await ProcessConsumers(shareData, video);
                        }


                        if (shareData.InvitationSource == Invitation.Integration.ConsumerVideoInvitationRequestSource.Email
                            && ((shareData.ContactRecipientIds != null && shareData.ContactRecipientIds.Count > 0) ||
                                (shareData.PartnerRecipientIds != null && shareData.PartnerRecipientIds.Count > 0)))
                        {
                            await ProcessContacts(shareData, video);
                        }

                    }
                    else
                    {
                        _logger.LogInformation($"Video not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex}");
                    throw;
                }

            }
            catch (Exception ex)
            {

                throw;
            }
        }


        private string GetThumbnailUrl(VideoDataFull video) {

            switch (video.ThumbnailSettings.ThumbnailType)
            {
                case ThumbnailSettingsThumbnailType.Static:
                    return video.ThumbnailSettings.StaticUrl;

                case ThumbnailSettingsThumbnailType.Animated:
                    return video.ThumbnailSettings.AnimatedUrl;

                case ThumbnailSettingsThumbnailType.Custom:
                    return video.ThumbnailSettings.CustomUrl;

            }

            throw new Exception($"Thumbnail URL not found for {video.VideoId}");
        }

        private async Task SendInvitation(TopicVideoShareMessage data, GlobalContactSummary contact, VideoDataFull video)
        {
            _logger.LogInformation($"Send invitation {video.VideoId}");

            HttpClient cl = new HttpClient();
            cl.SetBearerToken(JWT);
            var client = new Invitation.Integration.Tabrasa_Invitation_IntegrationClient(cl)
            {
                BaseUrl = Environment.GetEnvironmentVariable("InvitationEndpoint")
            };

            try
            {
                string videoUrl = GetShareUrlForContact(video.VideoId, contact.ContactId);
                
                Invitation.Integration.GlobalContactType contactType = Invitation.Integration.GlobalContactType.Lead;
                switch (contact.ContactType)
                {
                    case GlobalContactType.Applicant:
                        contactType = Invitation.Integration.GlobalContactType.Applicant;
                        break;

                    case GlobalContactType.Lead:
                        contactType = Invitation.Integration.GlobalContactType.Lead;
                        break;

                    case GlobalContactType.Partner:
                        contactType = Invitation.Integration.GlobalContactType.Partner;
                        break;
                }

                if (!string.IsNullOrEmpty(video.Description))
                {
                    video.Description = $"<tr><td>Message:&nbsp;</td><td><p>{video.Description}</p></td></tr>";
                    
                }

                if (!string.IsNullOrEmpty (video.Title))
                {
                    video.Title = $"<tr><td>Subject:&nbsp;</td><td><p>{video.Title}</p></td></tr>";
                }

                await client.VideoAsync(data.ClientId, data.UserId, new Invitation.Integration.ConsumerVideoInvitationRequest()
                {
                    Description = video.Description,
                    Title = video.Title,
                    ContactId = contact.ContactId,
                    ContactType = contactType,
                    CustomMessage = data.Message,
                    ThumbnailUrl = GetThumbnailUrl(video),
                    VideoUrl = videoUrl,
                    Source = Invitation.Integration.ConsumerVideoInvitationRequestSource.Email,
                    VideoId = video.VideoId
                });
                return;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                throw ex;
            }

        }

        private string GetShareUrlForContact(Guid videoId, long contactId)
        {
            string ContactURL = Environment.GetEnvironmentVariable("ContactVideoURLPattern");
            return string.Format(ContactURL, videoId, contactId);
        }

        private async Task SendConsumerInvitation(TopicVideoShareMessage data, ConsumerExtended consumer, VideoDataFull video)
        {
            _logger.LogInformation($"Send invitation {video.VideoId}");

            HttpClient cl = new HttpClient();
            cl.SetBearerToken(JWT);
            var client = new Invitation.Integration.Tabrasa_Invitation_IntegrationClient(cl)
            {
                BaseUrl = Environment.GetEnvironmentVariable("InvitationEndpoint")
            };

            try
            {
                // FOr consumer video URL is calculated during invitation
                await client.VideoAsync(data.ClientId, data.UserId, new Invitation.Integration.ConsumerVideoInvitationRequest()
                {
                    Description = video.Description,
                    Title = video.Title,
                    ConsumerId = consumer.ConsumerId,
                    CustomMessage = data.Message,
                    ThumbnailUrl = GetThumbnailUrl(video),
                    VideoUrl = "",
                    Source = Invitation.Integration.ConsumerVideoInvitationRequestSource.Engage,
                    VideoId = video.VideoId,
                });
                return;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                throw ex;
            }

        }


        /// <summary>
        /// UPdate the video with the new contacts
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        private async Task<VideoDataFull> UpdateVideo(VideoDataFull video)
        {
            _logger.LogInformation($"Update video {video.VideoId}");

            HttpClient cl = new HttpClient();
            cl.SetBearerToken(JWT);
            var client = new TabrasaVideo.Internal.TabrasaVideo_InternalClient(cl)
            {
                BaseUrl = Environment.GetEnvironmentVariable("VideoEndpoint")
            };

            try
            {

                var videoUpdated = await client.VideoPUTAsync(video.VideoId, video);
                return videoUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                throw ex;
            }

        }


        /// <summary>
        /// GEt the video by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<VideoDataFull> GetVideoById(Guid id)
        {
            _logger.LogInformation($"Get video {id}");

            HttpClient cl = new HttpClient();
            cl.SetBearerToken(JWT);
            var client = new TabrasaVideo.Internal.TabrasaVideo_InternalClient(cl)
            {
                BaseUrl = Environment.GetEnvironmentVariable("VideoEndpoint")
            };

            try
            {

                var video = await client.VideoGETAsync(id);
                return video;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                throw ex;
            }

        }


        private async Task<VideoURLResponse> GetVideoURLById(Guid id)
        {
            _logger.LogInformation($"Get video {id}");

            HttpClient cl = new HttpClient();
            cl.SetBearerToken(JWT);
            var client = new TabrasaVideo.Internal.TabrasaVideo_InternalClient(cl)
            {
                BaseUrl = Environment.GetEnvironmentVariable("VideoEndpoint")
            };

            try
            {

                var videoURL = await client.UrlAsync(id);
                return videoURL;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{ex}");
                throw ex;
            }

        }


    }
}