using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class ConversationStageHistory : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Guid StageId { get; set; }
        public Guid ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public Stage Stage { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;
    }
}