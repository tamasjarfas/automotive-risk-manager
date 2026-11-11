// Controllers/AuditLogsController.cs
// Csak olvasható (read-only) REST API vezérlő az üzleti auditnapló lekérdezéséhez.
//
// Végpont:
//   GET /api/audit-logs – auditnapló bejegyzések szűrt, lapozott lekérdezése
//
// BIZTONSÁG ÉS INTEGRITÁS:
//   - Kizárólag GET végpont létezik: az auditnapló bejegyzések API-n keresztül
//     nem hozhatók létre manuálisan, nem módosíthatók és nem törölhetők (append-only).
//   - Jogosultság: KIZÁRÓLAG Administrator szerepkör (AppPolicies.ViewAuditLogs).
//     Bejelentkezés nélkül: 401 Unauthorized.
//     RiskManager és Viewer szerepkörrel: 403 Forbidden.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Data;
using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;
using AutomotiveRiskManager.Api.Services;

namespace AutomotiveRiskManager.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AppPolicies.ViewAuditLogs)]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditLogsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ─── GET /api/audit-logs ──────────────────────────────────────────────────
    /// <summary>
    /// Auditnapló bejegyzések szűrt és lapozott lekérdezése, legfrissebb elöl sorrendben.
    /// Opcionális szűrők:
    ///   - fromUtc: kezdő időpont (UTC)
    ///   - toUtc: záró időpont (UTC)
    ///   - entityType: Supplier | Risk | MitigationAction | User
    ///   - action: Created | Updated | StatusChanged | Deactivated | Activated | RoleChanged | Completed | Reopened
    ///   - actor: végrehajtó felhasználónév (részszavas keresés)
    ///   - entityId: érintett entitás azonosítója (Guid)
    ///   - page: oldalszám (1-től indul, alapértelmezett: 1)
    ///   - pageSize: oldalankénti elemszám (1–100, alapértelmezett: 20)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] string? actor = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] int page = AuditLogBuilder.DefaultPage,
        [FromQuery] int pageSize = AuditLogBuilder.DefaultPageSize)
    {
        AuditEntityType? parsedEntityType = null;
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            if (!AuditLogBuilder.TryParseEntityType(entityType, out var et))
            {
                return BadRequest(new
                {
                    Message = $"Érvénytelen entitástípus szűrő: '{entityType}'. Elfogadott értékek: Supplier, Risk, MitigationAction, User."
                });
            }

            parsedEntityType = et;
        }

        AuditAction? parsedAction = null;
        if (!string.IsNullOrWhiteSpace(action))
        {
            if (!AuditLogBuilder.TryParseAction(action, out var act))
            {
                return BadRequest(new
                {
                    Message = $"Érvénytelen műveletszűrő: '{action}'. Elfogadott értékek: Created, Updated, StatusChanged, Deactivated, Activated, RoleChanged, Completed, Reopened."
                });
            }

            parsedAction = act;
        }

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
        {
            return BadRequest(new
            {
                Message = "A kezdő időpont (fromUtc) nem lehet későbbi a záró időpontnál (toUtc)."
            });
        }

        var (normalizedPage, normalizedPageSize) = AuditLogBuilder.NormalizePaging(page, pageSize);

        var filteredQuery = AuditLogBuilder.ApplyFiltersAndOrdering(
            _db.AuditLogs.AsNoTracking(),
            fromUtc,
            toUtc,
            parsedEntityType,
            parsedAction,
            actor,
            entityId);

        int totalCount = await filteredQuery.CountAsync();
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling((double)totalCount / normalizedPageSize);

        var entities = await filteredQuery
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync();

        var items = entities
            .Select(AuditLogDto.FromEntity)
            .ToList();

        return Ok(new AuditLogPageDto
        {
            Items      = items,
            Page       = normalizedPage,
            PageSize   = normalizedPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }
}

