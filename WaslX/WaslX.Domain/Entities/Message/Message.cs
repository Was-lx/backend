using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Message : BaseEntity
    {
        public int ConversationId { get; set; }
        public int? SenderUserId { get; set; }
        public SenderType SenderType { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public string WhatsAppMessageId { get; set; } = string.Empty;
        public MessageStatus Status { get; set; }
        public DateTime Timestamp { get; set; }

        public Conversation Conversation { get; set; } = null!;
        public User? SenderUser { get; set; }
    }
}