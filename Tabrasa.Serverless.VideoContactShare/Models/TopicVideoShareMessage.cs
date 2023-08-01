using Invitation.Integration;
using System;

namespace Tabrasa.Serverless.VideoContactShare.Models
{
    public class TopicVideoShareMessage: VideoShareMessage
    {
        public Guid VideoId { get; set; }
        public int ClientId { get; set; } = 0;
        public int UserId { get; set; } = 0;
        public string AppScope { get; set; } = string.Empty;
        public ConsumerVideoInvitationRequestSource InvitationSource { get; set; }
    }
}
