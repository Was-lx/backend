using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class ConversationTag
    {
        public int ConversationId { get; set; }
        public int TagId { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}
