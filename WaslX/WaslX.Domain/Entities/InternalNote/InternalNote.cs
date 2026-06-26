using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class InternalNote : BaseEntity
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
