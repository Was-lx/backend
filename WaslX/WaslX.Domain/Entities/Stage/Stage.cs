using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Stage : BaseEntity
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }

        public Group Group { get; set; } = null!;
        public ICollection<Conversation> CurrentConversations { get; set; } = new HashSet<Conversation>();
        public ICollection<ConversationStageHistory> ConversationStageHistories { get; set; } = new HashSet<ConversationStageHistory>();
    }
}
