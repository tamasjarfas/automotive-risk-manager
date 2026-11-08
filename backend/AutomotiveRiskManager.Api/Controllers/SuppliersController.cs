// Controllers/SuppliersController.cs
// REST API vezérlő a beszállítók kezeléséhez.
//
// Az alábbi végpontokat biztosítja:
//   GET    /api/suppliers           – összes beszállító listája
//   GET    /api/suppliers/{id}      – egy adott beszállító adatai
//   POST   /api/suppliers           – új beszállító rögzítése
//   PUT    /api/suppliers/{id}      – meglévő beszállító módosítása
//   PATCH  /api/suppliers/{id}/status – állapot megváltoztatása
//
// JOGOSULTSÁGOK (szerveroldalon ellenőrizve, lásd Program.cs szabályzatai):
//   Megtekintés (GET)                 – Administrator, RiskManager, Viewer
//   Létrehozás, módosítás, állapot    – Administrator, RiskManager
//   Bejelentkezés nélkül: 401, jogosultság nélkül (pl. Viewer módosít): 403.
//   Az állapotmódosító kérésekhez CSRF-token is kell (X-CSRF-TOKEN fejléc).

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Data;
using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;

namespace AutomotiveRiskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.ViewSuppliers)]   // alap: minden szerepkör megtekinthet
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SuppliersController> _logger;

    // Az ApplicationDbContext-et az ASP.NET Core DI rendszere adja be (dependency injection).
    public SuppliersController(ApplicationDbContext db, ILogger<SuppliersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── GET /api/suppliers ───────────────────────────────────────────────────
    /// <summary>Az összes beszállító visszaadása névsorban.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var suppliers = await _db.Suppliers
            .OrderBy(s => s.Name)
            .Select(s => SupplierDto.FromEntity(s))
            .ToListAsync();

        return Ok(suppliers);
    }

    // ─── GET /api/suppliers/{id} ──────────────────────────────────────────────
    /// <summary>Egy adott azonosítójú beszállító visszaadása.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú beszállító nem található." });
        }

        return Ok(SupplierDto.FromEntity(supplier));
    }

    // ─── POST /api/suppliers ──────────────────────────────────────────────────
    /// <summary>Új beszállító rögzítése.</summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.ManageSuppliers)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        // A ModelState ellenőrzése a DataAnnotation attribútumok alapján automatikus,
        // de [ApiController] attribútum esetén a 400-as választ automatikusan visszaadja.
        // Az explicit ellenőrzés jobb hibaüzenetért és dokumentálhatóságért szerepel itt.
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Duplikált név ellenőrzése
        bool nameExists = await _db.Suppliers
            .AnyAsync(s => s.Name == request.Name);

        if (nameExists)
        {
            return BadRequest(new { Message = $"'{request.Name}' nevű beszállító már létezik." });
        }

        var supplier = new Supplier
        {
            Id          = Guid.NewGuid(),
            Name        = request.Name.Trim(),
            Activity    = request.Activity.Trim(),
            Description = request.Description?.Trim(),
            Status      = SupplierStatus.Active,
            CreatedAt   = DateTimeOffset.UtcNow,
            UpdatedAt   = null
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Új beszállító rögzítve: {Name} (Id: {Id})", supplier.Name, supplier.Id);

        // 201 Created + Location header a létrehozott erőforrás URL-jével
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, SupplierDto.FromEntity(supplier));
    }

    // ─── PUT /api/suppliers/{id} ──────────────────────────────────────────────
    /// <summary>Egy meglévő beszállító adatainak teljes frissítése.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.ManageSuppliers)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var supplier = await _db.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú beszállító nem található." });
        }

        // Duplikált név ellenőrzése (saját magát kizárva)
        bool nameExists = await _db.Suppliers
            .AnyAsync(s => s.Name == request.Name && s.Id != id);

        if (nameExists)
        {
            return BadRequest(new { Message = $"'{request.Name}' nevű beszállító már létezik." });
        }

        supplier.Name        = request.Name.Trim();
        supplier.Activity    = request.Activity.Trim();
        supplier.Description = request.Description?.Trim();
        supplier.UpdatedAt   = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Beszállító módosítva: {Name} (Id: {Id})", supplier.Name, supplier.Id);

        return Ok(SupplierDto.FromEntity(supplier));
    }

    // ─── PATCH /api/suppliers/{id}/status ────────────────────────────────────
    /// <summary>Egy beszállító állapotának megváltoztatása (aktív ↔ inaktív).</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AppPolicies.ManageSuppliers)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSupplierStatusRequest request)
    {
        var supplier = await _db.Suppliers.FindAsync(id);

        if (supplier is null)
        {
            return NotFound(new { Message = $"A(z) {id} azonosítójú beszállító nem található." });
        }

        // Érvényes-e az állapot?
        if (!Enum.TryParse<SupplierStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            return BadRequest(new
            {
                Message = $"Érvénytelen állapotérték: '{request.Status}'. Elfogadott értékek: Active, Inactive."
            });
        }

        var previousStatus = supplier.Status;
        supplier.Status    = newStatus;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Beszállító állapota megváltozott: {Name} | {Prev} → {New} (Id: {Id})",
            supplier.Name, previousStatus, newStatus, supplier.Id);

        return Ok(SupplierDto.FromEntity(supplier));
    }
}

