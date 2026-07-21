namespace WaslX.Application.Features.Customers.Dtos;

/// <summary>One row in the Contacts directory — a customer plus lightweight conversation aggregates.</summary>
public record CustomerListItemDto(
    int Id,
    string Name,
    string Phone,
    int ConversationCount,
    DateTime? LastContactAt,
    int? AssignedUserId,
    string? AssignedUserName,
    IReadOnlyList<string> Tags);

/// <summary>A page of contacts plus the total count matching the filter (for numbered pagination).</summary>
public record CustomerListResponse(IReadOnlyList<CustomerListItemDto> Items, int Total);

/// <summary>Server-side filter + paging for the Contacts directory. <paramref name="AssignedUserId"/>
/// is the assignee's Identity (GUID) id — the service resolves it to the numeric domain user id.</summary>
public record CustomerListFilter(
    string? Search,
    int? TagId,
    string? AssignedUserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int Page,
    int PageSize);
