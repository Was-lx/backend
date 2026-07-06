namespace WaslX.Domain.Authorization
{
    /// <summary>
    /// Stable permission code constants. Use these everywhere a permission is checked
    /// so a rename is a compile error, not a silent security hole.
    /// </summary>
    public static class Permissions
    {
        // Conversations & inbox
        public const string ConversationViewScope = "conversation.view_scope";
        public const string ConversationReply = "conversation.reply";
        public const string ConversationNote = "conversation.note";
        public const string ConversationStatus = "conversation.status";
        public const string ConversationPriority = "conversation.priority";
        public const string ConversationAssign = "conversation.assign";
        public const string ConversationStageHandoff = "conversation.stage_handoff";
        public const string ConversationDelete = "conversation.delete";

        // Contacts
        public const string ContactViewScope = "contact.view_scope";
        public const string ContactEdit = "contact.edit";
        public const string ContactMaskPii = "contact.mask_pii";
        public const string ContactExport = "contact.export";
        public const string ContactDelete = "contact.delete";

        // Tags
        public const string TagApply = "tag.apply";
        public const string TagManage = "tag.manage";

        // Routing / groups / assignment
        public const string RoutingConfigure = "routing.configure";
        public const string GroupManage = "group.manage";
        public const string AssignmentReassignOthers = "assignment.reassign_others";

        // AI
        public const string AiUseSuggestions = "ai.use_suggestions";
        public const string AiConfigure = "ai.configure";

        // Campaigns
        public const string CampaignView = "campaign.view";
        public const string CampaignCreateSend = "campaign.create_send";

        // Reports
        public const string ReportViewOwn = "report.view_own";
        public const string ReportViewTeam = "report.view_team";
        public const string ReportExport = "report.export";

        // Channel / templates
        public const string ChannelConnect = "channel.connect";
        public const string TemplateManage = "template.manage";

        // Users & roles
        public const string UserManage = "user.manage";
        public const string RoleManage = "role.manage";

        // Tenant & billing
        public const string BillingManage = "billing.manage";
        public const string SettingsManage = "settings.manage";
        public const string AuditView = "audit.view";
    }
}
