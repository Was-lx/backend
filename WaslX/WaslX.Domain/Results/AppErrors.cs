namespace WaslX.Domain.Results;

/// <summary>
/// Errors for the platform / billing / tenancy features. Status codes are raw ints
/// to keep the Domain free of an ASP.NET Core dependency.
/// </summary>
public static class AppErrors
{
    // Plans
    public static readonly Error PlanNotFound = new("Plan.NotFound", "Subscription plan not found", 404);
    public static readonly Error DuplicatePlanCode = new("Plan.DuplicateCode", "A plan with this code already exists", 409);

    // Tenants
    public static readonly Error TenantNotFound = new("Tenant.NotFound", "Workspace not found", 404);
    public static readonly Error NoPlansConfigured = new("Tenant.NoPlansConfigured", "No subscription plans are configured yet", 400);
    public static readonly Error NoTenantContext = new("Tenant.NoContext", "This account is not attached to a workspace", 400);
    public static readonly Error TenantSuspended = new("Tenant.Suspended", "This workspace has been suspended. Please contact support.", 403);

    // Onboarding
    public static readonly Error OnboardingInvalidStep = new("Onboarding.InvalidStep", "Invalid onboarding step", 400);

    // Conversation 24-hour window
    public static readonly Error ServiceWindowClosed = new("Conversation.ServiceWindowClosed", "The 24-hour customer service window has expired. Only approved templates can be sent.", 400);

    // Billing / subscription (simulated)
    public static readonly Error TrialAlreadyUsed = new("Billing.TrialAlreadyUsed", "This workspace has already used its trial", 400);
    public static readonly Error InvoiceNotFound = new("Billing.InvoiceNotFound", "Invoice not found", 404);
    public static readonly Error InvoiceAlreadyPaid = new("Billing.InvoiceAlreadyPaid", "This invoice is already paid", 400);
    public static readonly Error InvalidBillingStatus = new("Billing.InvalidStatus", "Invalid billing status", 400);
    public static readonly Error InvalidInvoiceStatus = new("Billing.InvalidInvoiceStatus", "Invalid invoice status", 400);
    public static readonly Error InvalidInvoiceAmount = new("Billing.InvalidAmount", "The invoice amount must be greater than zero", 400);

    // Platform / SuperAdmin (Sprint 6)
    public static readonly Error NotASuperAdmin = new("Platform.NotASuperAdmin", "This user is not a platform administrator", 404);
    public static readonly Error PaymentDeclined = new("Billing.PaymentDeclined", "The card was declined", 402);
    public static readonly Error NoActiveSubscription = new("Billing.NoActiveSubscription", "No active subscription to change", 400);
    public static readonly Error PaymentMethodRequired = new("Billing.PaymentMethodRequired", "Add a payment method first", 400);

    // WhatsApp / Meta Cloud API
    public static readonly Error WhatsAppAccountNotFound = new("WhatsApp.AccountNotFound", "No WhatsApp account is connected for this workspace", 404);
    public static readonly Error WhatsAppNotConnected = new("WhatsApp.NotConnected", "The WhatsApp account is not connected", 400);
    public static readonly Error WhatsAppTokenExchangeFailed = new("WhatsApp.TokenExchangeFailed", "Could not exchange the authorization code with Meta", 400);
    public static readonly Error WhatsAppBusinessInfoFailed = new("WhatsApp.BusinessInfoFailed", "Could not resolve the WhatsApp Business account details from Meta", 400);
    public static readonly Error WhatsAppGraphApiError = new("WhatsApp.GraphApiError", "The Meta Graph API request failed", 502);
    public static readonly Error WhatsAppSendFailed = new("WhatsApp.SendFailed", "Failed to send the WhatsApp message", 502);
    public static readonly Error WhatsAppInvalidWebhookSignature = new("WhatsApp.InvalidWebhookSignature", "Webhook signature validation failed", 401);
    public static readonly Error WhatsAppTemplateCreateFailed = new("WhatsApp.TemplateCreateFailed", "Meta rejected the message template", 400);
    public static readonly Error WhatsAppWindowClosed = new("WhatsApp.WindowClosed", "The 24-hour messaging window is closed; send an approved template to re-open the conversation", 400);

    // Conversations / inbox
    public static readonly Error ConversationNotFound = new("Conversation.NotFound", "Conversation not found", 404);
    public static readonly Error ConversationAccessDenied = new("Conversation.AccessDenied", "You do not have access to this conversation", 403);
    public static readonly Error ConversationInvalidTransition = new("Conversation.InvalidTransition", "That status change is not allowed from the current status", 400);
    public static readonly Error UserContextNotResolved = new("User.ContextNotResolved", "Could not resolve the current user for this workspace", 400);
    public static readonly Error OwnerLocked = new("User.OwnerLocked", "The workspace owner's role cannot be changed and the owner cannot be disabled.", 403);
    public static readonly Error InvalidStatus = new("Conversation.InvalidStatus", "Invalid status or mode provided", 400);

    // Channels (Sprint 3)
    public static readonly Error ChannelNotFound = new("Channel.NotFound", "Channel not found", 404);
    public static readonly Error ChannelNameRequired = new("Channel.NameRequired", "A channel name is required", 400);
    public static readonly Error DuplicateChannelName = new("Channel.DuplicateName", "A channel with this name already exists", 409);

    // Groups / teams & stages (Sprint 3)
    public static readonly Error GroupNotFound = new("Group.NotFound", "Group not found", 404);
    public static readonly Error GroupNameRequired = new("Group.NameRequired", "A group name is required", 400);
    public static readonly Error DuplicateGroupName = new("Group.DuplicateName", "A group with this name already exists", 409);
    public static readonly Error GroupInUse = new("Group.InUse", "This group or one of its stages is still referenced by conversations and cannot be deleted", 409);
    public static readonly Error StageNotFound = new("Stage.NotFound", "Stage not found", 404);
    public static readonly Error StageNameRequired = new("Stage.NameRequired", "A stage name is required", 400);
    public static readonly Error StageNotInGroup = new("Stage.NotInGroup", "That stage does not belong to the conversation's current group", 400);

    // Tags (Sprint 3)
    public static readonly Error TagNotFound = new("Tag.NotFound", "Tag not found", 404);
    public static readonly Error TagNameRequired = new("Tag.NameRequired", "A tag name is required", 400);
    public static readonly Error DuplicateTagName = new("Tag.DuplicateName", "A tag with this name already exists", 409);

    // Working hours & shifts (Sprint 3)
    public static readonly Error ShiftNotFound = new("Shift.NotFound", "Shift not found", 404);
    public static readonly Error ShiftNameRequired = new("Shift.NameRequired", "A shift name is required", 400);
    public static readonly Error ShiftOutsideCompanyHours = new("Shift.OutsideCompanyHours", "A shift day must fall on a company working day and stay within its hours", 400);
    public static readonly Error InvalidWorkingHours = new("WorkingHours.Invalid", "Invalid working hours; check the day and that the end time is after the start time", 400);

    // Campaigns (Sprint 5 — FR-CMP)
    public static readonly Error CampaignNotFound = new("Campaign.NotFound", "Campaign not found", 404);
    public static readonly Error CampaignNameRequired = new("Campaign.NameRequired", "A campaign name is required", 400);
    public static readonly Error CampaignTemplateRequired = new("Campaign.TemplateRequired", "An approved WhatsApp template is required", 400);
    public static readonly Error CampaignHasNoRecipients = new("Campaign.HasNoRecipients", "The audience filter did not match any customers", 400);
    public static readonly Error CampaignInvalidStatusTransition = new("Campaign.InvalidStatusTransition", "That action is not allowed from the campaign's current status", 400);
    public static readonly Error CampaignNotEditable = new("Campaign.NotEditable", "Only a Draft campaign can be edited", 400);

    // Reporting & analytics (Sprint 5 — FR-RPT)
    public static readonly Error ReportUnknown = new("Report.Unknown", "Unknown report", 404);
    public static readonly Error ReportInvalidRange = new("Report.InvalidRange", "The report date range is invalid; 'from' must be on or before 'to'", 400);

    // Notifications (Sprint 5 — FR-NOTIF)
    public static readonly Error NotificationNotFound = new("Notification.NotFound", "Notification not found", 404);

    // Platform AI cost / budget alerts (Sprint 6 — US-6.5)
    public static readonly Error BudgetAlertNotFound = new("BudgetAlert.NotFound", "Budget alert not found", 404);
    public static readonly Error BudgetThresholdInvalid = new("BudgetAlert.ThresholdInvalid", "The budget threshold must be greater than zero", 400);

    // Platform credentials / secrets (Sprint 6 — US-6.6)
    public static readonly Error CredentialNotFound = new("Credential.NotFound", "Credential not found", 404);
    public static readonly Error CredentialKeyRequired = new("Credential.KeyRequired", "A credential key is required", 400);
    public static readonly Error CredentialValueRequired = new("Credential.ValueRequired", "A credential value is required", 400);
    public static readonly Error DuplicateCredentialKey = new("Credential.DuplicateKey", "A credential with this key already exists", 409);

    // Platform feature flags (Sprint 6 — US-6.7)
    public static readonly Error FeatureFlagNotFound = new("FeatureFlag.NotFound", "Feature flag not found", 404);
    public static readonly Error FeatureFlagKeyRequired = new("FeatureFlag.KeyRequired", "A feature flag key is required", 400);

    // Platform impersonation (Sprint 6 — US-6.8)
    public static readonly Error ImpersonationReasonRequired = new("Impersonation.ReasonRequired", "A reason is required to impersonate a workspace", 400);
    public static readonly Error TenantHasNoAdmin = new("Impersonation.TenantHasNoAdmin", "This workspace has no admin/owner account to impersonate", 400);
    public static readonly Error ImpersonationSessionNotFound = new("Impersonation.SessionNotFound", "Impersonation session not found", 404);
    public static readonly Error ImpersonationAlreadyEnded = new("Impersonation.AlreadyEnded", "This impersonation session has already ended", 400);

    // Platform announcements (Sprint 6 — US-6.10)
    public static readonly Error AnnouncementNotFound = new("Announcement.NotFound", "Announcement not found", 404);
    public static readonly Error AnnouncementTitleRequired = new("Announcement.TitleRequired", "An announcement title is required", 400);
    public static readonly Error AnnouncementBodyRequired = new("Announcement.BodyRequired", "An announcement body is required", 400);
    public static readonly Error AnnouncementAlreadyPublished = new("Announcement.AlreadyPublished", "This announcement is already published", 400);

    // Media
    public static readonly Error MediaUploadFailed = new("Media.UploadFailed", "Could not upload the file", 502);
    public static readonly Error MediaFileRequired = new("Media.FileRequired", "A file is required", 400);
    public static readonly Error MediaUnsupportedType = new("Media.UnsupportedType", "This file type is not supported", 400);

    // AI / RAG (chat, embeddings, vector store)
    public static readonly Error AiGatewayError = new("Ai.GatewayError", "The AI gateway request failed", 502);
    public static readonly Error AiGenerationFailed = new("Ai.GenerationFailed", "The AI model could not generate a response", 502);
    public static readonly Error EmbeddingFailed = new("Ai.EmbeddingFailed", "Could not generate embeddings", 502);
    public static readonly Error VectorStoreError = new("Vector.StoreError", "The vector store request failed", 502);
    public static readonly Error KnowledgeDocumentNotFound = new("Knowledge.DocumentNotFound", "Knowledge document not found", 404);
    public static readonly Error FaqNotFound = new("Knowledge.FaqNotFound", "FAQ not found", 404);
    public static Error KnowledgeIngestionFailed(string reason) => new("Knowledge.IngestionFailed", reason, 502);
}
