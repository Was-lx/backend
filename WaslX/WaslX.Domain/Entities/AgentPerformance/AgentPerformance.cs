using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class AgentPerformance : BaseEntity
    {
        public int UserId { get; set; }
        public int ChatsHandled { get; set; }
        public decimal AvgResponseTime { get; set; }
        public decimal ResolutionRate { get; set; }
        public int ActiveChats { get; set; }
        public int ResolvedChats { get; set; }
        public DateTime LastUpdated { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public User User { get; set; } = null!;
    }
}
