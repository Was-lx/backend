namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>Assign/change a tenant's plan from the SuperAdmin console (sets PlanId + billing state).</summary>
/// <param name="PlanId">The plan to move the tenant onto.</param>
/// <param name="BillingStatus">Optional billing state to apply (Trial | Active | PastDue | Cancelled); defaults to Active.</param>
public record ChangeTenantPlanInput(int PlanId, string? BillingStatus);

/// <summary>Manually generate an invoice for a tenant (cross-tenant billing action).</summary>
/// <param name="Amount">Invoice amount.</param>
/// <param name="DueDate">Optional due date; defaults to 14 days out.</param>
/// <param name="Status">Optional status (Pending | Paid | Overdue | Cancelled); defaults to Pending.</param>
public record GenerateInvoiceInput(decimal Amount, DateTime? DueDate, string? Status);
