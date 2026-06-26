using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class UserGroup
    {
        public int UserId { get; set; }
        public int GroupId { get; set; }

        public User User { get; set; } = null!;
        public Group Group { get; set; } = null!;
    }
}
