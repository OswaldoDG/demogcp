using System;
using System.Collections.Generic;

namespace Tabrasa.Serverless.VideoContactShare.Models
{
    public class VideoShareMessage
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool LoginRequired { get; set; }
        public bool SingleRecipient { get; set; }
        public List<Guid> ConsumerRecipentIds { set; get; }
        public List<long> ContactRecipientIds { get; set; }
        public List<long> PartnerRecipientIds { get; set; }
    }
}
