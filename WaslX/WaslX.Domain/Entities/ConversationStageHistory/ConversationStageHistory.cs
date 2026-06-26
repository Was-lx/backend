using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class ConversationStageHistory : BaseEntity
    {
        public int ConversationId { get; set; }
        public int StageId { get; set; }
        public int ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public Stage Stage { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;
    }
}
