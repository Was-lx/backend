using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
using WaslX.Domain.Entities;
using WaslX.Domain.Results;
using WaslX.Domain.SharedEnums;
using WaslX.Persistance.Data;

namespace WaslX.Persistance.Services;

internal sealed partial class WhatsAppTemplateService(ApplicationDbContext db, IMetaGraphApiService graphApi) : IWhatsAppTemplateService
{
    public async Task<Result<IReadOnlyList<TemplateDto>>> GetTemplatesAsync(int? tenantId, string? status, CancellationToken cancellationToken = default)
    {
        var accountResult = await ResolveAccountAsync(tenantId, cancellationToken);
        if (accountResult.IsFailure)
            return Result.Failure<IReadOnlyList<TemplateDto>>(accountResult.Error);

        var (tid, wabaId, accessToken) = accountResult.Value;
        var listResult = await graphApi.ListTemplatesAsync(wabaId, accessToken, status, cancellationToken);
        if (listResult.IsFailure)
            return Result.Failure<IReadOnlyList<TemplateDto>>(listResult.Error);

        // Merge live Meta data with the locally-stored review metadata (reason, submitted category,
        // allow_category_change, reviewed_at) by Meta template id.
        var reviews = await db.TemplateReviews
            .Where(r => r.TenantId == tid)
            .ToDictionaryAsync(r => r.MetaTemplateId, cancellationToken);

        var templates = listResult.Value.Select(t => Map(t, reviews.GetValueOrDefault(t.Id))).ToList();
        return Result.Success<IReadOnlyList<TemplateDto>>(templates);
    }

    public async Task<Result<TemplateCreateResultDto>> CreateTemplateAsync(int? tenantId, CreateTemplateInput input, CancellationToken cancellationToken = default)
    {
        var accountResult = await ResolveAccountAsync(tenantId, cancellationToken);
        if (accountResult.IsFailure)
            return Result.Failure<TemplateCreateResultDto>(accountResult.Error);

        var (tid, wabaId, accessToken) = accountResult.Value;
        var payload = BuildCreatePayload(input);

        var result = await graphApi.CreateTemplateAsync(wabaId, accessToken, payload, cancellationToken);
        if (result.IsFailure)
            return Result.Failure<TemplateCreateResultDto>(result.Error);

        // Persist the create-time audit row: Meta never echoes back the category we submitted nor
        // our allow_category_change choice, so we keep them locally to power the "requested vs final"
        // category comparison later (once the review webhook lands).
        var review = new TemplateReview
        {
            TenantId = tid,
            MetaTemplateId = result.Value.Id,
            MessageTemplateName = input.Name.Trim().ToLowerInvariant(),
            Language = input.Language,
            Status = result.Value.Status,
            SubmittedCategory = input.Category.ToUpperInvariant(),
            AllowCategoryChange = input.AllowCategoryChange,
            ReviewedAt = null
        };
        await db.TemplateReviews.AddAsync(review, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TemplateCreateResultDto(result.Value.Id, result.Value.Status, result.Value.Category));
    }

    /// <summary>Shapes the Meta create-template request body. Authentication templates use the fixed OTP structure.</summary>
    private static object BuildCreatePayload(CreateTemplateInput input)
    {
        var category = input.Category.ToUpperInvariant();
        var components = new List<object>();

        if (category == "AUTHENTICATION")
        {
            // Authentication templates have a Meta-generated body; we only opt into the security
            // recommendation and attach the required one-tap/copy-code OTP button.
            components.Add(new { type = "BODY", add_security_recommendation = true });
            var otpText = input.Buttons.FirstOrDefault()?.Text;
            components.Add(new
            {
                type = "BUTTONS",
                buttons = new object[] { new { type = "OTP", otp_type = "COPY_CODE", text = string.IsNullOrWhiteSpace(otpText) ? "Copy code" : otpText } }
            });
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(input.HeaderText))
                components.Add(new { type = "HEADER", format = "TEXT", text = input.HeaderText });

            var body = input.BodyText ?? string.Empty;
            var variableCount = CountBodyVariables(body);
            if (variableCount > 0)
            {
                // Meta requires a sample value per BODY variable on create.
                var samples = Enumerable.Range(1, variableCount).Select(i => $"sample{i}").ToArray();
                components.Add(new { type = "BODY", text = body, example = new { body_text = new[] { samples } } });
            }
            else
            {
                components.Add(new { type = "BODY", text = body });
            }

            if (!string.IsNullOrWhiteSpace(input.FooterText))
                components.Add(new { type = "FOOTER", text = input.FooterText });

            var buttons = input.Buttons
                .Select(b => b.Type.ToUpperInvariant() switch
                {
                    "URL" => (object)new { type = "URL", text = b.Text, url = b.Url ?? string.Empty },
                    _ => new { type = "QUICK_REPLY", text = b.Text }
                })
                .ToArray();
            if (buttons.Length > 0)
                components.Add(new { type = "BUTTONS", buttons });
        }

        return new
        {
            name = input.Name.Trim().ToLowerInvariant(),
            language = input.Language,
            category,
            allow_category_change = input.AllowCategoryChange,
            components
        };
    }

    private static int CountBodyVariables(string body)
    {
        var max = 0;
        foreach (Match m in VariableRegex().Matches(body))
        {
            if (int.TryParse(m.Groups[1].Value, out var n) && n > max)
                max = n;
        }
        return max;
    }

    private static TemplateDto Map(MetaTemplate t, TemplateReview? review)
    {
        var header = t.Components.FirstOrDefault(c => c.Type.Equals("HEADER", StringComparison.OrdinalIgnoreCase));
        string? Text(string type) => t.Components.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase))?.Text;
        var buttons = t.Components
            .FirstOrDefault(c => c.Type.Equals("BUTTONS", StringComparison.OrdinalIgnoreCase))?.Buttons
            .Select(b => new TemplateButtonDto(b.Type, b.Text, b.Url, b.PhoneNumber))
            .ToList() ?? [];

        // Live Meta category is the authoritative "final" category. The submitted category is the
        // one we stored at create time; when they differ, Meta changed it during review.
        var finalCategory = t.Category;
        var submittedCategory = string.IsNullOrEmpty(review?.SubmittedCategory) ? null : review.SubmittedCategory;
        var changedByMeta = submittedCategory is not null
            && !string.Equals(submittedCategory, finalCategory, StringComparison.OrdinalIgnoreCase);

        return new TemplateDto(
            t.Id, t.Name, t.Language, t.Category, t.Status,
            Text("HEADER"), Text("BODY"), Text("FOOTER"), buttons,
            ReasonCode: review?.ReasonCode,
            ReasonText: review?.ReasonText,
            MetaNotes: review?.MetaNotes,
            SubmittedCategory: submittedCategory,
            FinalCategory: finalCategory,
            AllowCategoryChange: review?.AllowCategoryChange ?? false,
            ChangedByMeta: changedByMeta,
            ReviewedAt: review?.ReviewedAt,
            // Uppercased so the frontend can switch on TEXT/IMAGE/VIDEO/DOCUMENT reliably.
            HeaderFormat: header?.Format?.ToUpperInvariant());
    }

    /// <summary>Resolves the tenant's connected account: tenant id + WABA id + access token, or a clear failure.</summary>
    private async Task<Result<(int TenantId, string WabaId, string AccessToken)>> ResolveAccountAsync(int? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid)
            return Result.Failure<(int, string, string)>(AppErrors.NoTenantContext);

        var account = await db.WhatsAppAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tid, cancellationToken);
        if (account is null)
            return Result.Failure<(int, string, string)>(AppErrors.WhatsAppAccountNotFound);
        if (account.Status != WhatsAppAccountStatus.Connected)
            return Result.Failure<(int, string, string)>(AppErrors.WhatsAppNotConnected);

        return Result.Success((tid, account.WhatsAppBusinessAccountId, account.AccessToken));
    }

    [GeneratedRegex(@"\{\{(\d+)\}\}")]
    private static partial Regex VariableRegex();
}
