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

    // Onboarding
    public static readonly Error OnboardingInvalidStep = new("Onboarding.InvalidStep", "Invalid onboarding step", 400);

    // Conversation 24-hour window
    public static readonly Error ServiceWindowClosed = new("Conversation.ServiceWindowClosed", "The 24-hour customer service window has expired. Only approved templates can be sent.", 400);

    // Billing / subscription (simulated)
    public static readonly Error TrialAlreadyUsed = new("Billing.TrialAlreadyUsed", "This workspace has already used its trial", 400);
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

    // AI / OpenAI (Sprint 4)
    public static readonly Error OpenAiNotConfigured = new("AI.NotConfigured", "The AI service is not configured (missing API key)", 503);
    public static readonly Error SummaryGenerationFailed = new("AI.SummaryGenerationFailed", "Could not generate the conversation summary", 502);
    public static readonly Error ConversationHasNoMessages = new("AI.NoMessages", "There are no messages to summarize yet", 400);
    public static readonly Error AiNumberDisabled = new("AI.NumberDisabled", "AI is disabled for this WhatsApp number by the administrator", 403);

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
