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

    // Conversations / inbox
    public static readonly Error ConversationNotFound = new("Conversation.NotFound", "Conversation not found", 404);
    public static readonly Error ConversationAccessDenied = new("Conversation.AccessDenied", "You do not have access to this conversation", 403);
}
