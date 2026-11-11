// Controllers/MitigationsController.cs
// REST API vezérlő a kockázatcsökkentő intézkedések (MitigationAction) kezeléséhez.
//
// Végpontok:
//   GET    /api/mitigations               – intézkedések listázása és szűrése
//   GET    /api/mitigations/{id}          – egy adott intézkedés részletes adatai
//   POST   /api/mitigations               – új intézkedés rögzítése
//   PUT    /api/mitigations/{id}          – meglévő intézkedés módosítása
//   PATCH  /api/mitigations/{id}/status   – intézkedés státuszának (és eredményének) frissítése
//
// JOGOSULTSÁGOK (szerveroldalon érvényesítve):
//   Megtekintés (GET)                     – Administrator, RiskManager, Viewer (ViewMitigations)
//   Létrehozás, módosítás, státuszváltás  – Administrator, RiskManager (ManageMitigations)
//   Bejelentkezés nélkül: 401 Unauthorized, jogosultság nélkül (Viewer módosít): 403 Forbidden.
//   Minden állapotmódosító kéréshez érvényes CSRF-token (X-CSRF-TOKEN) szükséges.

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
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.ViewMitigations)]
public class MitigationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MitigationsController> _logger;

    public MitigationsController(ApplicationDbContext db, ILogger<MitigationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── GET /api/mitigations ─────────────────────────────────────────────────
    /// <summary>
    /// Intézkedések listázása.
    /// Opcionális szűrők:
    ///   - riskId: kapcsolódó kockázat szerint
    ///   - supplierId: érintett beszállító szerint
    ///   - status: Planned | InProgress | Completed | Cancelled
    ///   - priority: Low | Medium | High | Critical
    ///   - responsible: felelős neve (részszavas keresés)
    ///   - dueFilter: overdue (lejárt) | upcoming (határidős, nem lejárt) | noduedate (határidő nélküli)
    ///   - overdueOnly: true esetén csak a lejárt és még nem befejezett/visszavont intézkedések
    ///   - dueBefore: határidő felső korlátja
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MitigationActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? riskId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? responsible = null,
        [FromQuery] string? dueFilter = null,
        [FromQuery] bool? overdueOnly = null,
        [FromQuery] DateTimeOffset? dueBefore = null)
    {
        IQueryable<MitigationAction> query = _db.MitigationActions
            .AsNoTracking()
            .Include(m => m.Risk)
                .ThenInclude(r => r!.Supplier);

        if (riskId.HasValue)
        {
            query = query.Where(m => m.RiskId == riskId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(m => m.Risk != null && m.Risk.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            try
            {
                var parsedStatus = MitigationPolicy.ParseStatus(status);
                query = query.Where(m => m.Status == parsedStatus);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            try
            {
                var parsedPriority = MitigationPolicy.ParsePriority(priority);
                query = query.Where(m => m.Priority == parsedPriority);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        if (!string.IsNullOrWhiteSpace(responsible))
        {
            string search = responsible.Trim().ToLower();
            query = query.Where(m => m.ResponsiblePerson.ToLower().Contains(search));
        }

        if (dueBefore.HasValue)
        {
            var limitUtc = dueBefore.Value.ToUniversalTime();
            query = query.Where(m => m.DueDate.HasValue && m.DueDate.Value <= limitUtc);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var todayStartUtc = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);

        // Határidő-alapú szűrés (dueFilter vagy overdueOnly)
        if (overdueOnly == true || string.Equals(dueFilter, "overdue", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m =>
                m.DueDate.HasValue &&
                m.DueDate.Value < todayStartUtc &&
                m.Status != MitigationStatus.Completed &&
                m.Status != MitigationStatus.Cancelled);
        }
        else if (string.Equals(dueFilter, "upcoming", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m =>
                m.DueDate.HasValue &&
                m.DueDate.Value >= todayStartUtc &&
                m.Status != MitigationStatus.Completed &&
                m.Status != MitigationStatus.Cancelled);
        }
        else if (string.Equals(dueFilter, "noduedate", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => !m.DueDate.HasValue);
        }
        else if (!string.IsNullOrWhiteSpace(dueFilter))
        {
            return BadRequest(new
            {
                Message = $"Érvénytelen határidő-szűrő: '{dueFilter}'. Elfogadott értékek: overdue, upcoming, noduedate."
            });
        }

        var actions = await query
            .OrderBy(m => m.Status == MitigationStatus.Completed || m.Status == MitigationStatus.Cancelled ? 1 : 0)
            .ThenBy(m => m.DueDate == null ? 1 : 0)
            .ThenBy(m => m.DueDate)
            .ThenByDescending(m => m.Priority)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        var dtos = actions
            .Select(m => MitigationActionDto.FromEntity(m, null, nowUtc))
            .ToList();

        return Ok(dtos);
    }

    // ─── GET /api/mitigations/{id} ────────────────────────────────────────────
    /// <summary>Egy adott kockázatcsökkentő intézkedés részletes lekérdezése.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MitigationActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var action = await _db.MitigationActions
            .AsNoTracking()
            .Include(m => m.Risk)
                .ThenInclude(r => r!.Supplier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (action is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú intézkedés nem található." });
        }

        return Ok(MitigationActionDto.FromEntity(action));
    }

    // ─── POST /api/mitigations ────────────────────────────────────────────────
    /// <summary>Új kockázatcsökkentő intézkedés rögzítése egy létező kockázathoz.</summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.ManageMitigations)]
    [ProducesResponseType(typeof(MitigationActionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMitigationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.RiskId == Guid.Empty)
        {
            return BadRequest(new { Message = "Érvényes kockázat kiválasztása kötelező." });
        }

        // Ellenőrizzük, hogy a hivatkozott kockázat létezik-e az adatbázisban
        var risk = await _db.Risks
            .AsNoTracking()
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == request.RiskId);

        if (risk is null)
        {
            return BadRequest(new
            {
                Message = $"A megadott kockázat (Id: {request.RiskId}) nem létezik."
            });
        }

        MitigationAction action;
        try
        {
            action = MitigationPolicy.CreateAction(
                targetRisk:        risk,
                title:             request.Title,
                description:       request.Description,
                responsiblePerson: request.ResponsiblePerson,
                dueDate:           request.DueDate,
                priorityText:      request.Priority,
                statusText:        request.Status,
                resultNotes:       request.ResultNotes,
                nowUtc:            DateTimeOffset.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }

        _db.MitigationActions.Add(action);
        _db.AuditLogs.Add(AuditLogBuilder.ForMitigationCreated(
            AuditActor.FromPrincipal(User), action, risk.Title));
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Új kockázatcsökkentő intézkedés rögzítve: {Title} | Kockázat: {RiskTitle} | Felelős: {Responsible} | Státusz: {Status} | Id: {Id}",
            action.Title, risk.Title, action.ResponsiblePerson, action.Status, action.Id);

        var dto = MitigationActionDto.FromEntity(action, risk);
        return CreatedAtAction(nameof(GetById), new { id = action.Id }, dto);
    }

    // ─── PUT /api/mitigations/{id} ────────────────────────────────────────────
    /// <summary>Meglévő kockázatcsökkentő intézkedés adatainak módosítása.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.ManageMitigations)]
    [ProducesResponseType(typeof(MitigationActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMitigationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var action = await _db.MitigationActions.FindAsync(id);
        if (action is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú intézkedés nem található." });
        }

        if (request.RiskId == Guid.Empty)
        {
            return BadRequest(new { Message = "Érvényes kockázat kiválasztása kötelező." });
        }

        var risk = await _db.Risks
            .AsNoTracking()
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == request.RiskId);

        if (risk is null)
        {
            return BadRequest(new
            {
                Message = $"A megadott kockázat (Id: {request.RiskId}) nem létezik."
            });
        }

        var before = new MitigationAction
        {
            Id                = action.Id,
            RiskId            = action.RiskId,
            Title             = action.Title,
            Description       = action.Description,
            ResponsiblePerson = action.ResponsiblePerson,
            DueDate           = action.DueDate,
            Priority          = action.Priority,
            Status            = action.Status,
            CompletedAt       = action.CompletedAt,
            ResultNotes       = action.ResultNotes
        };

        try
        {
            MitigationPolicy.UpdateAction(
                action:            action,
                targetRisk:        risk,
                title:             request.Title,
                description:       request.Description,
                responsiblePerson: request.ResponsiblePerson,
                dueDate:           request.DueDate,
                priorityText:      request.Priority,
                statusText:        request.Status,
                resultNotes:       request.ResultNotes,
                nowUtc:            DateTimeOffset.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }

        _db.AuditLogs.Add(AuditLogBuilder.ForMitigationUpdated(
            AuditActor.FromPrincipal(User), before, action));
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Kockázatcsökkentő intézkedés módosítva: {Title} | Státusz: {Status} | Id: {Id}",
            action.Title, action.Status, action.Id);

        return Ok(MitigationActionDto.FromEntity(action, risk));
    }

    // ─── PATCH /api/mitigations/{id}/status ───────────────────────────────────
    /// <summary>
    /// Intézkedés státuszának (és opcionálisan eredményének) frissítése.
    /// Befejezéskor (Completed) automatikusan rögzíti a CompletedAt időpontot,
    /// visszanyitáskor pedig törli azt. A kapcsolódó kockázat pontszámát NEM módosítja.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AppPolicies.ManageMitigations)]
    [ProducesResponseType(typeof(MitigationActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMitigationStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var action = await _db.MitigationActions
            .Include(m => m.Risk)
                .ThenInclude(r => r!.Supplier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (action is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú intézkedés nem található." });
        }

        var previousStatus      = action.Status;
        var previousCompletedAt = action.CompletedAt;
        var previousResultNotes = action.ResultNotes;

        try
        {
            MitigationPolicy.ApplyStatusTransition(
                action:        action,
                newStatusText: request.Status,
                resultNotes:   request.ResultNotes,
                nowUtc:        DateTimeOffset.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }

        _db.AuditLogs.Add(AuditLogBuilder.ForMitigationStatusChanged(
            AuditActor.FromPrincipal(User),
            action,
            previousStatus,
            previousCompletedAt,
            previousResultNotes));
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Intézkedés státusza megváltozott: {Title} | {Prev} → {New} (Id: {Id})",
            action.Title, previousStatus, action.Status, action.Id);

        return Ok(MitigationActionDto.FromEntity(action));
    }
}

