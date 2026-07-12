using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WaslX.Application.Abstractions.WhatsApp;
using WaslX.Application.Features.WhatsApp.Templates.Dtos;
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

        var (wabaId, accessToken) = accountResult.Value;
        var listResult = await graphApi.ListTemplatesAsync(wabaId, accessToken, status, cancellationToken);
        if (listResult.IsFailure)
            return Result.Failure<IReadOnlyList<TemplateDto>>(listResult.Error);

        var templates = listResult.Value.Select(Map).ToList();
        return Result.Success<IReadOnlyList<TemplateDto>>(templates);
    }

    public async Task<Result<TemplateCreateResultDto>> CreateTemplateAsync(int? tenantId, CreateTemplateInput input, CancellationToken cancellationToken = default)
    {
        var accountResult = await ResolveAccountAsync(tenantId, cancellationToken);
        if (accountResult.IsFailure)
            return Result.Failure<TemplateCreateResultDto>(accountResult.Error);

        var (wabaId, accessToken) = accountResult.Value;
        var payload = BuildCreatePayload(input);

        var result = await graphApi.CreateTemplateAsync(wabaId, accessToken, payload, cancellationToken);
        if (result.IsFailure)
            return Result.Failure<TemplateCreateResultDto>(result.Error);

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

    private static TemplateDto Map(MetaTemplate t)
    {
        string? Text(string type) => t.Components.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase))?.Text;
        var buttons = t.Components
            .FirstOrDefault(c => c.Type.Equals("BUTTONS", StringComparison.OrdinalIgnoreCase))?.Buttons
            .Select(b => new TemplateButtonDto(b.Type, b.Text, b.Url, b.PhoneNumber))
            .ToList() ?? [];

        return new TemplateDto(t.Id, t.Name, t.Language, t.Category, t.Status,
            Text("HEADER"), Text("BODY"), Text("FOOTER"), buttons);
    }

    /// <summary>Resolves the tenant's connected WABA id + access token, or a clear failure.</summary>
    private async Task<Result<(string WabaId, string AccessToken)>> ResolveAccountAsync(int? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is not { } tid)
            return Result.Failure<(string, string)>(AppErrors.NoTenantContext);

        var account = await db.WhatsAppAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tid, cancellationToken);
        if (account is null)
            return Result.Failure<(string, string)>(AppErrors.WhatsAppAccountNotFound);
        if (account.Status != WhatsAppAccountStatus.Connected)
            return Result.Failure<(string, string)>(AppErrors.WhatsAppNotConnected);

        return Result.Success((account.whatsAppBusinessAccountId, account.AccessToken));
    }

    [GeneratedRegex(@"\{\{(\d+)\}\}")]
    private static partial Regex VariableRegex();
}
