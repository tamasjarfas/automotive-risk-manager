// Controllers/UsersController.cs
// Felhasználókezelés – KIZÁRÓLAG Administrator szerepkörnek (szerveroldali szabályzat).
//
//   GET   /api/users              – felhasználók listája
//   GET   /api/users/{id}         – egy felhasználó
//   POST  /api/users              – új felhasználó létrehozása (admin adja a kezdeti jelszót)
//   PUT   /api/users/{id}/role    – szerepkör megváltoztatása
//   PATCH /api/users/{id}/status  – aktiválás / inaktiválás
//
// Védelmek:
//   - Saját fiókon nem lehet szerepkört módosítani vagy inaktiválni. Mivel a hívó mindig
//     aktív Administrator, így mindig marad legalább egy aktív Administrator.
//   - Inaktiváláskor és szerepkörváltáskor a biztonsági pecsét cserélődik, ezért a
//     felhasználó meglévő munkamenete azonnal érvénytelen lesz.
//   - Jelszó és jelszókivonat sosem kerül a válaszba vagy a naplóba.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Data;
using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;
using AutomotiveRiskManager.Api.Services;

namespace AutomotiveRiskManager.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AppPolicies.ManageUsers)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    // ─── GET /api/users ───────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .ToListAsync();

        // Felhasználó → szerepkör(ök) leképezés egyetlen lekérdezéssel
        var userRoles = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync();

        var roleByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.RoleName)));

        var result = users.Select(u =>
            UserDto.FromEntity(u, roleByUser.TryGetValue(u.Id, out var role) ? role : string.Empty));

        return Ok(result);
    }

    // ─── GET /api/users/{id} ──────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "A felhasználó nem található." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(UserDto.FromEntity(user, string.Join(", ", roles)));
    }

    // ─── POST /api/users ──────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var role = NormalizeRole(request.Role);
        if (role is null)
        {
            return BadRequest(new { message = InvalidRoleMessage });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,   // nincs e-mailes megerősítés: a fiókot rendszergazda hozza létre
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            return BadRequest(new
            {
                message = "A felhasználó létrehozása sikertelen.",
                errors = create.Errors.Select(e => e.Description).ToArray()
            });
        }

        var addRole = await _userManager.AddToRoleAsync(user, role);
        if (!addRole.Succeeded)
        {
            return BadRequest(new { message = "A szerepkör hozzárendelése sikertelen." });
        }

        _db.AuditLogs.Add(AuditLogBuilder.ForUserCreated(AuditActor.FromPrincipal(User), user.Id, user.UserName ?? "", role, user.IsActive));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Felhasználó létrehozva: {UserName} ({Role}), létrehozta: {AdminId}",
            user.UserName, role, CurrentUserId());

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, UserDto.FromEntity(user, role));
    }

    // ─── PUT /api/users/{id}/role ─────────────────────────────────────────────
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, [FromBody] SetUserRoleRequest request)
    {
        var role = NormalizeRole(request.Role);
        if (role is null)
        {
            return BadRequest(new { message = InvalidRoleMessage });
        }

        if (id == CurrentUserId())
        {
            return BadRequest(new { message = "A saját szerepköröd nem módosíthatod." });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "A felhasználó nem található." });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var previousRole = string.Join(", ", currentRoles);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // Előbb az újat adjuk hozzá, csak utána vesszük el a régit: a felhasználó sosem marad szerepkör nélkül.
        if (!currentRoles.Contains(role))
        {
            var add = await _userManager.AddToRoleAsync(user, role);
            if (!add.Succeeded)
            {
                return BadRequest(new { message = "A szerepkör módosítása sikertelen." });
            }
        }

        var toRemove = currentRoles.Where(r => r != role).ToList();
        if (toRemove.Count > 0)
        {
            var remove = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!remove.Succeeded)
            {
                return BadRequest(new { message = "A korábbi szerepkör eltávolítása sikertelen." });
            }
        }

        // A munkamenet jogosultságai azonnal frissüljenek
        await _userManager.UpdateSecurityStampAsync(user);

        _db.AuditLogs.Add(AuditLogBuilder.ForUserRoleChanged(AuditActor.FromPrincipal(User), user.Id, user.UserName ?? "", previousRole, role));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Szerepkör módosítva: {UserName} → {Role}, módosította: {AdminId}",
            user.UserName, role, CurrentUserId());

        return Ok(UserDto.FromEntity(user, role));
    }

    // ─── PATCH /api/users/{id}/status ─────────────────────────────────────────
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetUserStatusRequest request)
    {
        if (id == CurrentUserId())
        {
            return BadRequest(new { message = "A saját fiókod állapota nem módosítható." });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound(new { message = "A felhasználó nem található." });
        }

        var previousIsActive = user.IsActive;
        var newIsActive = request.IsActive!.Value;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        user.IsActive = newIsActive;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return BadRequest(new { message = "Az állapot módosítása sikertelen." });
        }

        if (user.IsActive)
        {
            // Újraaktiváláskor a korábbi zárolás is feloldódik
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        // Inaktiváláskor a meglévő munkamenet azonnal érvénytelenné válik
        await _userManager.UpdateSecurityStampAsync(user);

        _db.AuditLogs.Add(AuditLogBuilder.ForUserStatusChanged(AuditActor.FromPrincipal(User), user.Id, user.UserName ?? "", previousIsActive, newIsActive));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Felhasználó állapota módosítva: {UserName} → {Status}, módosította: {AdminId}",
            user.UserName, user.IsActive ? "Aktív" : "Inaktív", CurrentUserId());

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(UserDto.FromEntity(user, string.Join(", ", roles)));
    }

    // ─── Segédfüggvények ──────────────────────────────────────────────────────
    private const string InvalidRoleMessage =
        "Érvénytelen szerepkör. Elfogadott értékek: Administrator, RiskManager, Viewer.";

    private static string? NormalizeRole(string role) =>
        AppRoles.All.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

