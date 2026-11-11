// Controllers/RisksController.cs
// REST API vezérlő a beszállítói információbiztonsági kockázatok kezeléséhez.
//
// Végpontok:
//   GET    /api/risks               – kockázatok listázása (szűrhető: supplierId, level, status)
//   GET    /api/risks/{id}          – egy adott kockázat részletes adatai
//   POST   /api/risks               – új kockázat rögzítése és automatikus értékelése
//   PUT    /api/risks/{id}          – kockázat módosítása és pontszám/szint újraszámítása
//   PATCH  /api/risks/{id}/status   – kockázat státuszának módosítása (pl. lezárás/archiválás)
//
// JOGOSULTSÁGOK (szerveroldalon érvényesítve):
//   Megtekintés (GET)                     – Administrator, RiskManager, Viewer (ViewRisks)
//   Létrehozás, módosítás, státuszváltás  – Administrator, RiskManager (ManageRisks)
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
[Authorize(Policy = AppPolicies.ViewRisks)]
public class RisksController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RisksController> _logger;

    public RisksController(ApplicationDbContext db, ILogger<RisksController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── GET /api/risks ───────────────────────────────────────────────────────
    /// <summary>
    /// Kockázatok listázása csökkenő kockázati pontszám szerint.
    /// Opcionális szűrők: supplierId, level (Low/Medium/High/Critical), status (Open/UnderReview/Mitigated/Closed).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RiskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? level = null,
        [FromQuery] string? status = null)
    {
        IQueryable<Risk> query = _db.Risks
            .AsNoTracking()
            .Include(r => r.Supplier);

        if (supplierId.HasValue)
        {
            query = query.Where(r => r.SupplierId == supplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(level))
        {
            if (!Enum.TryParse<RiskLevel>(level, ignoreCase: true, out var parsedLevel) ||
                !Enum.IsDefined(parsedLevel))
            {
                return BadRequest(new
                {
                    Message = $"Érvénytelen kockázati szint szűrő: '{level}'. Elfogadott értékek: Low, Medium, High, Critical."
                });
            }

            query = query.Where(r => r.Level == parsedLevel);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RiskStatus>(status, ignoreCase: true, out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                return BadRequest(new
                {
                    Message = $"Érvénytelen státusz szűrő: '{status}'. Elfogadott értékek: Open, UnderReview, Mitigated, Closed."
                });
            }

            query = query.Where(r => r.Status == parsedStatus);
        }

        var risks = await query
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => RiskDto.FromEntity(r, null))
            .ToListAsync();

        return Ok(risks);
    }

    // ─── GET /api/risks/{id} ──────────────────────────────────────────────────
    /// <summary>Egy adott kockázat részletes adatainak lekérdezése.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RiskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var risk = await _db.Risks
            .AsNoTracking()
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (risk is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú kockázat nem található." });
        }

        return Ok(RiskDto.FromEntity(risk));
    }

    // ─── POST /api/risks ──────────────────────────────────────────────────────
    /// <summary>Új beszállítói kockázat rögzítése és szerveroldali kockázatszámítás.</summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.ManageRisks)]
    [ProducesResponseType(typeof(RiskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRiskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.SupplierId == Guid.Empty)
        {
            return BadRequest(new { Message = "Érvényes beszállító kiválasztása kötelező." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { Message = "A kockázat megnevezése nem lehet üres." });
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { Message = "A kockázati kategória megadása kötelező." });
        }

        // Ellenőrizzük, hogy a hivatkozott beszállító valóban létezik-e az adatbázisban
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId);

        if (supplier is null)
        {
            return BadRequest(new
            {
                Message = $"A megadott beszállító (Id: {request.SupplierId}) nem létezik."
            });
        }

        // Szerveroldali kockázatszámítás (a kliens nem küldhet pontszámot vagy szintet)
        RiskEvaluationResult evaluation;
        try
        {
            evaluation = RiskCalculator.Evaluate(request.Probability, request.Impact);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }

        var risk = new Risk
        {
            Id          = Guid.NewGuid(),
            SupplierId  = supplier.Id,
            Title       = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category    = request.Category.Trim(),
            Probability = request.Probability,
            Impact      = request.Impact,
            Score       = evaluation.Score,
            Level       = evaluation.Level,
            Status      = RiskStatus.Open,
            CreatedAt   = DateTimeOffset.UtcNow,
            UpdatedAt   = null
        };

        _db.Risks.Add(risk);
        _db.AuditLogs.Add(AuditLogBuilder.ForRiskCreated(AuditActor.FromPrincipal(User), risk, supplier.Name));
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Új kockázat rögzítve: {Title} | Beszállító: {Supplier} | Pontszám: {Score} ({Level}) | Id: {Id}",
            risk.Title, supplier.Name, risk.Score, risk.Level, risk.Id);

        var dto = RiskDto.FromEntity(risk, supplier.Name);
        return CreatedAtAction(nameof(GetById), new { id = risk.Id }, dto);
    }

    // ─── PUT /api/risks/{id} ──────────────────────────────────────────────────
    /// <summary>Meglévő kockázat adatainak módosítása és a pontszám/szint automatikus újraszámítása.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.ManageRisks)]
    [ProducesResponseType(typeof(RiskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRiskRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var risk = await _db.Risks.FindAsync(id);
        if (risk is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú kockázat nem található." });
        }

        if (request.SupplierId == Guid.Empty)
        {
            return BadRequest(new { Message = "Érvényes beszállító kiválasztása kötelező." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { Message = "A kockázat megnevezése nem lehet üres." });
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { Message = "A kockázati kategória megadása kötelező." });
        }

        var supplier = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId);

        if (supplier is null)
        {
            return BadRequest(new
            {
                Message = $"A megadott beszállító (Id: {request.SupplierId}) nem létezik."
            });
        }

        // Ha a kérésben státusz is érkezett, ellenőrizzük
        RiskStatus parsedStatus = risk.Status;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<RiskStatus>(request.Status, ignoreCase: true, out var newStatus) ||
                !Enum.IsDefined(newStatus))
            {
                return BadRequest(new
                {
                    Message = $"Érvénytelen státuszérték: '{request.Status}'. Elfogadott értékek: Open, UnderReview, Mitigated, Closed."
                });
            }

            parsedStatus = newStatus;
        }

        // Pontszám és kockázati szint kötelező szerveroldali újraszámítása
        RiskEvaluationResult evaluation;
        try
        {
            evaluation = RiskCalculator.Evaluate(request.Probability, request.Impact);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }

        var before = new Risk
        {
            Id          = risk.Id,
            SupplierId  = risk.SupplierId,
            Title       = risk.Title,
            Description = risk.Description,
            Category    = risk.Category,
            Probability = risk.Probability,
            Impact      = risk.Impact,
            Score       = risk.Score,
            Level       = risk.Level,
            Status      = risk.Status
        };

        risk.SupplierId  = supplier.Id;
        risk.Title       = request.Title.Trim();
        risk.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        risk.Category    = request.Category.Trim();
        risk.Probability = request.Probability;
        risk.Impact      = request.Impact;
        risk.Score       = evaluation.Score;
        risk.Level       = evaluation.Level;
        risk.Status      = parsedStatus;
        risk.UpdatedAt   = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(AuditLogBuilder.ForRiskUpdated(
            AuditActor.FromPrincipal(User), before, risk, supplier.Name));
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Kockázat módosítva és újraértékelve: {Title} | Pontszám: {Score} ({Level}) | Státusz: {Status} | Id: {Id}",
            risk.Title, risk.Score, risk.Level, risk.Status, risk.Id);

        return Ok(RiskDto.FromEntity(risk, supplier.Name));
    }

    // ─── PATCH /api/risks/{id}/status ─────────────────────────────────────────
    /// <summary>
    /// Kockázat státuszának megváltoztatása (Open, UnderReview, Mitigated, Closed).
    /// Fizikai törlés helyett a kockázatok lezárhatók (Closed).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AppPolicies.ManageRisks)]
    [ProducesResponseType(typeof(RiskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateRiskStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var risk = await _db.Risks
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (risk is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú kockázat nem található." });
        }

        if (!Enum.TryParse<RiskStatus>(request.Status, ignoreCase: true, out var newStatus) ||
            !Enum.IsDefined(newStatus))
        {
            return BadRequest(new
            {
                Message = $"Érvénytelen státuszérték: '{request.Status}'. Elfogadott értékek: Open, UnderReview, Mitigated, Closed."
            });
        }

        var previousStatus = risk.Status;
        risk.Status        = newStatus;
        risk.UpdatedAt     = DateTimeOffset.UtcNow;

        _db.AuditLogs.Add(AuditLogBuilder.ForRiskStatusChanged(
            AuditActor.FromPrincipal(User), risk, previousStatus, newStatus));
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Kockázat státusza megváltozott: {Title} | {Prev} → {New} (Id: {Id})",
            risk.Title, previousStatus, newStatus, risk.Id);

        return Ok(RiskDto.FromEntity(risk));
    }

    // ─── GET /api/risks/{id}/mitigations ──────────────────────────────────────
    /// <summary>Egy adott kockázathoz kapcsolódó összes kockázatcsökkentő intézkedés lekérdezése.</summary>
    [HttpGet("{id:guid}/mitigations")]
    [ProducesResponseType(typeof(IEnumerable<MitigationActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMitigationsForRisk(Guid id)
    {
        var risk = await _db.Risks
            .AsNoTracking()
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (risk is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú kockázat nem található." });
        }

        var actions = await _db.MitigationActions
            .AsNoTracking()
            .Where(m => m.RiskId == id)
            .OrderBy(m => m.Status == MitigationStatus.Completed || m.Status == MitigationStatus.Cancelled ? 1 : 0)
            .ThenBy(m => m.DueDate == null ? 1 : 0)
            .ThenBy(m => m.DueDate)
            .ThenByDescending(m => m.Priority)
            .ToListAsync();

        var nowUtc = DateTimeOffset.UtcNow;
        var dtos = actions
            .Select(m => MitigationActionDto.FromEntity(m, risk, nowUtc))
            .ToList();

        return Ok(dtos);
    }
}

