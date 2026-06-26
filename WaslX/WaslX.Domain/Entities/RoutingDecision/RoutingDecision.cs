using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class RoutingDecision : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Guid? RecommendedUserId { get; set; }
        public string Topic { get; set; } = string.Empty;
        public Language Language { get; set; }
        public Sentiment Sentiment { get; set; }
        public ConversationPriority Priority { get; set; }
        public bool VipFlag { get; set; }
        public bool AutoReply { get; set; }
        public RoutingMode Mode { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User? RecommendedUser { get; set; }
    }
}