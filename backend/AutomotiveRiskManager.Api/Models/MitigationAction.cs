// Models/MitigationAction.cs
// Kockázatcsökkentő intézkedés (MitigationAction) entitás.
// Az adatbázis „mitigation_actions" táblájának C# reprezentációja.
//
// Tervezési és üzleti elvek:
//   - Minden intézkedés egy létező beszállítói kockázathoz (RiskId -> risks.Id) tartozik.
//   - Egy kockázathoz több intézkedés is tartozhat (1:N kapcsolat).
//   - A kapcsolódó kockázat nem törölhető kaszkád módon az intézkedésekkel együtt (DeleteBehavior.Restrict).
//   - Az intézkedés befejezése (Status = Completed) rögzíti a CompletedAt időpontot,
//     de NEM módosítja automatikusan a kapcsolódó Risk valószínűségét, hatását,
//     pontszámát vagy szintjét – a maradványkockázat újraértékelése külön RiskManager feladat.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomotiveRiskManager.Api.Models;

[Table("mitigation_actions")]
public class MitigationAction
{
    /// <summary>Egyedi azonosító (UUID v4) – az adatbázis elsődleges kulcsa.</summary>
    public Guid Id { get; set; }

    /// <summary>A kapcsolódó kockázat egyedi azonosítója (kötelező idegen kulcs a risks táblára).</summary>
    [Required(ErrorMessage = "A kapcsolódó kockázat kiválasztása kötelező.")]
    public Guid RiskId { get; set; }

    /// <summary>Navigációs tulajdonság a kapcsolódó kockázathoz.</summary>
    public Risk? Risk { get; set; }

    /// <summary>Az intézkedés rövid, egyértelmű megnevezése. Kötelező, max. 200 karakter.</summary>
    [Required(ErrorMessage = "Az intézkedés megnevezése kötelező.")]
    [MaxLength(200, ErrorMessage = "Az intézkedés megnevezése legfeljebb 200 karakter lehet.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Az intézkedés részletes leírása (mit és hogyan kell végrehajtani). Max. 2000 karakter.</summary>
    [MaxLength(2000, ErrorMessage = "A részletes leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; set; }

    /// <summary>A végrehajtásért felelős személy vagy szervezeti egység megnevezése. Kötelező, max. 150 karakter.</summary>
    [Required(ErrorMessage = "A felelős megadása kötelező.")]
    [MaxLength(150, ErrorMessage = "A felelős megnevezése legfeljebb 150 karakter lehet.")]
    public string ResponsiblePerson { get; set; } = string.Empty;

    /// <summary>Teljesítési határidő (UTC). Null, ha nincs konkrét határidő kijelölve.</summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>Az intézkedés végrehajtási állapota (Planned, InProgress, Completed, Cancelled).</summary>
    public MitigationStatus Status { get; set; } = MitigationStatus.Planned;

    /// <summary>Az intézkedés prioritása (Low, Medium, High, Critical).</summary>
    public MitigationPriority Priority { get; set; } = MitigationPriority.Medium;

    /// <summary>A tényleges teljesítés időpontja (UTC), ha az állapot Completed; egyébként null.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>A végrehajtás eredménye, tapasztalatai vagy bizonyítékai. Max. 2000 karakter.</summary>
    [MaxLength(2000, ErrorMessage = "Az eredmény / megjegyzés legfeljebb 2000 karakter lehet.")]
    public string? ResultNotes { get; set; }

    /// <summary>A létrehozás időpontja (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Az utolsó módosítás időpontja (UTC). Null, ha még nem módosították.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

