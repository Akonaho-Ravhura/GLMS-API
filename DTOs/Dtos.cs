using GLMS_CORE_APP.API.Services;
using GLMS_CORE_APP.Shared.Data;
using GLMS_CORE_APP.Shared.Models;
using GLMS_CORE_APP.Shared.Models.Enums;
namespace GLMS_CORE_APP.API.DTOs;

// ── Auth ───────────────────────────────────────────────────────────────────

public record LoginRequestDto(string Email, string Password);

public record LoginResponseDto(
    string Token,
    string FullName,
    string Email,
    string Role,
    DateTime ExpiresAt);

public record RegisterRequestDto(
    string FullName,
    string Email,
    string Password,
    Role Role);

// ── Client ─────────────────────────────────────────────────────────────────

public record ClientDto(
    Guid ClientId,
    string Name,
    string ContactEmail,
    string? PhoneNumber,
    string CountryCode,
    string PreferredCurrency,
    string? Address,
    bool IsActive,
    DateTime CreatedAt);

public record CreateClientDto(
    string Name,
    string ContactEmail,
    string? PhoneNumber,
    string CountryCode,
    string PreferredCurrency,
    string? Address,
    bool IsActive = true);

public record UpdateClientDto(
    string Name,
    string ContactEmail,
    string? PhoneNumber,
    string CountryCode,
    string PreferredCurrency,
    string? Address,
    bool IsActive);

// ── Contract ───────────────────────────────────────────────────────────────

public record ContractDto(
    Guid ContractId,
    Guid ClientId,
    string ClientName,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    ContractStatus Status,
    decimal Value,
    string BaseCurrency,
    int ExpiryWarningDays,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateContractDto(
    Guid ClientId,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal Value,
    string BaseCurrency,
    int ExpiryWarningDays = 30,
    ContractStatus Status = ContractStatus.Draft);

public record UpdateContractDto(
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal Value,
    string BaseCurrency,
    int ExpiryWarningDays,
    ContractStatus Status);

public record PatchContractStatusDto(ContractStatus Status);

// ── SLA ────────────────────────────────────────────────────────────────────

public record SLADto(
    Guid SLAId,
    Guid ContractId,
    string ContractTitle,
    string MetricName,
    string? MetricDescription,
    decimal TargetValue,
    string Unit,
    DateTime ReviewDate,
    int ReviewWarningDays,
    SLAStatus Status,
    DateTime? BreachedAt,
    string? BreachNotes,
    DateTime CreatedAt);

public record CreateSLADto(
    Guid ContractId,
    string MetricName,
    string? MetricDescription,
    decimal TargetValue,
    string Unit,
    DateTime ReviewDate,
    int ReviewWarningDays = 14,
    SLAStatus Status = SLAStatus.Pending);

public record UpdateSLADto(
    string MetricName,
    string? MetricDescription,
    decimal TargetValue,
    string Unit,
    DateTime ReviewDate,
    int ReviewWarningDays,
    SLAStatus Status);

// ── Service Request ────────────────────────────────────────────────────────

public record ServiceRequestDto(
    Guid RequestId,
    Guid ContractId,
    string ContractTitle,
    string ClientName,
    string RaisedByName,
    string? AssignedToName,
    string Title,
    string Description,
    string Priority,
    RequestStatus Status,
    DateTime RaisedAt,
    DateTime? ResolvedAt,
    string? RejectionReason,
    string? Notes);

public record CreateServiceRequestDto(
    Guid ContractId,
    string Title,
    string Description,
    string Priority,
    string? Notes);

public record UpdateServiceRequestDto(
    string Title,
    string Description,
    string Priority,
    RequestStatus Status,
    Guid? AssignedToUserId,
    string? RejectionReason,
    string? Notes);

// ── Currency ───────────────────────────────────────────────────────────────

public record CurrencyRateDto(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime FetchedAt,
    string Source);

public record ConvertCurrencyDto(
    decimal Amount,
    string FromCurrency,
    string ToCurrency);

public record ConvertCurrencyResponseDto(
    decimal OriginalAmount,
    string FromCurrency,
    decimal ConvertedAmount,
    string ToCurrency,
    decimal Rate,
    DateTime RateTimestamp);

// ── Shared ─────────────────────────────────────────────────────────────────

/// <summary>Standard API error envelope.</summary>
public record ApiErrorDto(string Message, string? Detail = null);

/// <summary>Standard paged response envelope.</summary>
public record PagedResponseDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
