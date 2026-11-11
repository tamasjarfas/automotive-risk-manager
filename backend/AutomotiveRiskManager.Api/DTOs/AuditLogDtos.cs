// DTOs/AuditLogDtos.cs
// Adatátviteli objektumok (DTO-k) a biztonsági auditnapló (/api/audit-logs) végponthoz.
//
// Tervezési elvek:
//   - Az AuditLog kizárólag olvasható (GET): nincsenek létrehozó vagy módosító DTO-k.
//   - A Changes JSON-stringet strukturált, biztonságos listaként (ChangeItems) is
//     visszaadjuk a React felület részletnézetéhez.

using System.Text.Json;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Services;

namespace AutomotiveRiskManager.Api.DTOs;

public sealed class AuditLogDto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Guid Id { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public Guid? ActorUserId { get; init; }
    public string ActorUserName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? Changes { get; init; }
    public IReadOnlyList<AuditFieldChange> ChangeItems { get; init; } =
        Array.Empty<AuditFieldChange>();

    public static AuditLogDto FromEntity(AuditLog log)
    {
        IReadOnlyList<AuditFieldChange> parsedChanges = Array.Empty<AuditFieldChange>();

        if (!string.IsNullOrWhiteSpace(log.Changes))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<AuditFieldChange>>(log.Changes, JsonOptions);
                if (items is not null)
                {
                    parsedChanges = items;
                }
            }
            catch
            {
                // Ha a JSON nem értelmezhető, üres listát adunk vissza, a nyers Changes megmarad
            }
        }

        return new AuditLogDto
        {
            Id            = log.Id,
            TimestampUtc  = log.TimestampUtc,
            ActorUserId   = log.ActorUserId,
            ActorUserName = log.ActorUserName,
            Action        = log.Action.ToString(),
            EntityType    = log.EntityType.ToString(),
            EntityId      = log.EntityId,
            Summary       = log.Summary,
            Changes       = log.Changes,
            ChangeItems   = parsedChanges
        };
    }
}

/// <summary>
/// Lapozott válasz a GET /api/audit-logs végponthoz.
/// </summary>
public sealed class AuditLogPageDto
{
    public IReadOnlyList<AuditLogDto> Items { get; init; } = Array.Empty<AuditLogDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

