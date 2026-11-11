// Models/AuditLog.cs
// Biztonsági és üzleti auditnapló (AuditLog) entitás.
// Az adatbázis „audit_logs" táblájának C# reprezentációja.
//
// Tervezési és biztonsági elvek:
//   - Az AuditLog rekordokat KIZÁRÓLAG a szerveroldali üzleti folyamatok hozzák létre.
//   - Nincs módosító (PUT/PATCH) vagy törlő (DELETE) végpont: a napló megváltoztathatatlan (append-only).
//   - Nincs kaszkád törlési kapcsolat sem a felhasználókra, sem az üzleti entitásokra:
//     az auditnyom akkor is megmarad, ha egy rekord később inaktívvá vagy lezárttá válik.
//   - A Changes mező kizárólag engedélyezett (allowlist) mezők változásait tartalmazza JSON-ként;
//     jelszó, jelszóhash, token, süti vagy titok SOHA nem kerülhet bele.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomotiveRiskManager.Api.Models;

[Table("audit_logs")]
public class AuditLog
{
    /// <summary>Egyedi azonosító (UUID v4) – az adatbázis elsődleges kulcsa.</summary>
    public Guid Id { get; set; }

    /// <summary>Az esemény pontos UTC időbélyege.</summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>A műveletet végrehajtó felhasználó azonosítója (ha bejelentkezett felhasználó végezte).</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>A műveletet végrehajtó felhasználó neve / stabil azonosítója.</summary>
    [Required]
    [MaxLength(150)]
    public string ActorUserName { get; set; } = string.Empty;

    /// <summary>A végrehajtott művelet típusa (Created, Updated, StatusChanged, Deactivated, Activated, RoleChanged, Completed, Reopened).</summary>
    public AuditAction Action { get; set; }

    /// <summary>Az érintett entitás típusa (Supplier, Risk, MitigationAction, User).</summary>
    public AuditEntityType EntityType { get; set; }

    /// <summary>Az érintett rekord egyedi azonosítója (Guid).</summary>
    public Guid EntityId { get; set; }

    /// <summary>Rövid, ember által olvasható magyar nyelvű eseményösszefoglaló.</summary>
    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Opcionális, strukturált JSON-adat (PostgreSQL jsonb) az engedélyezett mezők
    /// korábbi és új értékéről: [{"field":"Score","oldValue":"9","newValue":"20"}].
    /// </summary>
    public string? Changes { get; set; }
}

