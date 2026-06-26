using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Assignment : BaseEntity
    {
        public int ConversationId { get; set; }
        public int AssignedToUserId { get; set; }
        public AssignmentMethod Method { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User AssignedToUser { get; set; } = null!;
    }
}
