using WaslX.Domain.SharedEnums;
using System;
using System.Collections.Generic;
using WaslX.Domain.Common;
namespace WaslX.Domain.Entities
{

    public class Role : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();
        public ICollection<User> Users { get; set; } = new HashSet<User>();
    }
}
