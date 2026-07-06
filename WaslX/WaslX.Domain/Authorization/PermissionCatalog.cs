using WaslX.Domain.SharedEnums;

namespace WaslX.Domain.Authorization
{
    /// <summary>One row of the permission catalog + its default state per role.</summary>
    public sealed record PermissionDef(
        int Id,
        string Code,
        string Category,
        PermissionTier Tier,
        string Description,
        int Sort,
        bool AgentGranted,
        bool ManagerGranted,
        bool IsScope = false,
        string? ScopeOptions = null,
        string? AgentScope = null,
        string? ManagerScope = null,
        string? AdminScope = null);

    /// <summary>
    /// The single source of truth for the RBAC system. Drives (1) the seeded Permission
    /// table, (2) the default matrix copied into every new tenant, and (3) enforcement.
    /// Admin is always granted every capability, so only Agent/Manager defaults are listed.
    /// </summary>
    public static class PermissionCatalog
    {
        public const string RoleAdmin = "Admin";
        public const string RoleManager = "Manager";
        public const string RoleAgent = "Agent";

        public static readonly IReadOnlyList<string> Roles = new[] { RoleAdmin, RoleManager, RoleAgent };

        public static readonly IReadOnlyList<PermissionDef> All = new[]
        {
            // ── Conversations & inbox ──
            new PermissionDef(1,  Permissions.ConversationViewScope, "Conversations", PermissionTier.Configurable, "Which conversations they can see", 10, true, true, IsScope:true, ScopeOptions:"assigned,team,all", AgentScope:"assigned", ManagerScope:"all", AdminScope:"all"),
            new PermissionDef(2,  Permissions.ConversationReply, "Conversations", PermissionTier.Configurable, "Send WhatsApp replies", 11, true, true),
            new PermissionDef(3,  Permissions.ConversationNote, "Conversations", PermissionTier.Configurable, "Add internal notes", 12, true, true),
            new PermissionDef(4,  Permissions.ConversationStatus, "Conversations", PermissionTier.Configurable, "Change conversation status", 13, true, true),
            new PermissionDef(5,  Permissions.ConversationPriority, "Conversations", PermissionTier.Configurable, "Set conversation priority", 14, true, true),
            new PermissionDef(6,  Permissions.ConversationAssign, "Conversations", PermissionTier.Configurable, "Assign / take conversations", 15, true, true),
            new PermissionDef(7,  Permissions.ConversationStageHandoff, "Conversations", PermissionTier.Configurable, "Move a conversation across stages", 16, true, true),
            new PermissionDef(8,  Permissions.ConversationDelete, "Conversations", PermissionTier.AdminOnly, "Delete conversations", 17, false, false),

            // ── Contacts ──
            new PermissionDef(9,  Permissions.ContactViewScope, "Contacts", PermissionTier.Configurable, "Which contacts they can see", 20, true, true, IsScope:true, ScopeOptions:"team,all", AgentScope:"team", ManagerScope:"all", AdminScope:"all"),
            new PermissionDef(10, Permissions.ContactEdit, "Contacts", PermissionTier.Configurable, "Edit contact details", 21, true, true),
            new PermissionDef(11, Permissions.ContactExport, "Contacts", PermissionTier.ManagerPlus, "Export contacts", 22, false, true),
            new PermissionDef(12, Permissions.ContactDelete, "Contacts", PermissionTier.AdminOnly, "Delete contacts", 23, false, false),

            // ── Tags ──
            new PermissionDef(13, Permissions.TagApply, "Tags", PermissionTier.Configurable, "Apply / remove tags", 30, true, true),
            new PermissionDef(14, Permissions.TagManage, "Tags", PermissionTier.Configurable, "Create / edit / delete tags", 31, false, true),

            // ── Routing, groups, assignment ──
            new PermissionDef(15, Permissions.RoutingConfigure, "Routing & Teams", PermissionTier.ManagerPlus, "Configure routing / round-robin", 40, false, true),
            new PermissionDef(16, Permissions.GroupManage, "Routing & Teams", PermissionTier.ManagerPlus, "Manage groups, teams & stages", 41, false, true),
            new PermissionDef(17, Permissions.AssignmentReassignOthers, "Routing & Teams", PermissionTier.ManagerPlus, "Reassign other people's conversations", 42, false, true),

            // ── AI ──
            new PermissionDef(18, Permissions.AiUseSuggestions, "AI", PermissionTier.Configurable, "See AI reply suggestions", 50, true, true),
            new PermissionDef(19, Permissions.AiConfigure, "AI", PermissionTier.AdminOnly, "Configure AI (RAG / routing / models)", 51, false, false),

            // ── Campaigns ──
            new PermissionDef(20, Permissions.CampaignView, "Campaigns", PermissionTier.Configurable, "View campaigns", 60, true, true),
            new PermissionDef(21, Permissions.CampaignCreateSend, "Campaigns", PermissionTier.ManagerPlus, "Create & send campaigns", 61, false, true),

            // ── Reports ──
            new PermissionDef(22, Permissions.ReportViewOwn, "Reports", PermissionTier.Configurable, "View own performance", 70, true, true),
            new PermissionDef(23, Permissions.ReportViewTeam, "Reports", PermissionTier.ManagerPlus, "View team reports", 71, false, true),
            new PermissionDef(24, Permissions.ReportExport, "Reports", PermissionTier.ManagerPlus, "Export reports", 72, false, true),

            // ── Channel & templates ──
            new PermissionDef(25, Permissions.ChannelConnect, "WhatsApp", PermissionTier.AdminOnly, "Connect / disconnect WhatsApp", 80, false, false),
            new PermissionDef(26, Permissions.TemplateManage, "WhatsApp", PermissionTier.ManagerPlus, "Manage message templates", 81, false, true),

            // ── Users & roles ──
            new PermissionDef(27, Permissions.UserManage, "Team & Access", PermissionTier.AdminOnly, "Invite / manage users & assign roles", 90, false, false),
            new PermissionDef(28, Permissions.RoleManage, "Team & Access", PermissionTier.AdminOnly, "Edit roles & permissions", 91, false, false),

            // ── Tenant & billing ──
            new PermissionDef(29, Permissions.BillingManage, "Workspace", PermissionTier.AdminOnly, "Manage plan, subscription & invoices", 100, false, false),
            new PermissionDef(30, Permissions.SettingsManage, "Workspace", PermissionTier.AdminOnly, "Manage workspace settings", 101, false, false),
            new PermissionDef(31, Permissions.AuditView, "Workspace", PermissionTier.AdminOnly, "View audit logs", 102, false, false),
        };

        /// <summary>A capability that can never be granted to this role, whatever the tenant sets.</summary>
        public static bool IsLockedFor(PermissionDef def, string role) =>
            (def.Tier == PermissionTier.AdminOnly && role != RoleAdmin) ||
            (def.Tier == PermissionTier.ManagerPlus && role == RoleAgent);

        /// <summary>The default (granted, scope) for a role — respecting the hard tier rules.</summary>
        public static (bool granted, string? scope) DefaultFor(PermissionDef def, string role)
        {
            if (IsLockedFor(def, role))
                return (false, null);

            var granted = role switch
            {
                RoleAdmin => true,
                RoleManager => def.ManagerGranted,
                _ => def.AgentGranted
            };

            string? scope = null;
            if (def.IsScope)
                scope = role switch
                {
                    RoleAdmin => def.AdminScope,
                    RoleManager => def.ManagerScope,
                    _ => def.AgentScope
                };

            return (granted, scope);
        }
    }
}
