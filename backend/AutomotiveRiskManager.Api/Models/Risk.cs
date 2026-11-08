// Models/Risk.cs
// A beszállítói információbiztonsági kockázat (Risk / RiskAssessment) entitás.
// Az adatbázis „risks" táblájának C# reprezentációja.
//
// Tervezési elvek:
//   - Minden kockázat egy létező beszállítóhoz (SupplierId -> suppliers.Id) tartozik.
//   - A Probability (1–5) és Impact (1–5) mezők alapján a Score (1–25) és a Level
//     (Low / Medium / High / Critical) értékeket KIZÁRÓLAG a szerver számítja ki
//     a központi RiskCalculator osztály segítségével.
//   - Fizikai törlés nincs: a lezárt kockázatok Status = RiskStatus.Closed értéket kapnak.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomotiveRiskManager.Api.Models;

[Table("risks")]
public class Risk
{
    /// <summary>Egyedi azonosító (UUID v4) – az adatbázis elsődleges kulcsa.</summary>
    public Guid Id { get; set; }

    /// <summary>A kapcsolódó beszállító egyedi azonosítója (idegen kulcs a suppliers táblára).</summary>
    [Required(ErrorMessage = "A beszállító kiválasztása kötelező.")]
    public Guid SupplierId { get; set; }

    /// <summary>Navigációs tulajdonság a kapcsolódó beszállítóhoz.</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>A kockázat rövid, egyértelmű megnevezése. Kötelező, max. 200 karakter.</summary>
    [Required(ErrorMessage = "A kockázat megnevezése kötelező.")]
    [MaxLength(200, ErrorMessage = "A kockázat megnevezése legfeljebb 200 karakter lehet.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>A kockázat részletes szakmai leírása (fenyegetés, sérülékenység, érintett rendszer).</summary>
    [MaxLength(2000, ErrorMessage = "A részletes leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; set; }

    /// <summary>Kockázati kategória (pl. Adatvédelem, Hozzáférés-kezelés, Hálózatbiztonság).</summary>
    [Required(ErrorMessage = "A kockázati kategória megadása kötelező.")]
    [MaxLength(100, ErrorMessage = "A kategória legfeljebb 100 karakter lehet.")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Bekövetkezési valószínűség (1 = nagyon ritka ... 5 = szinte biztos).</summary>
    [Range(1, 5, ErrorMessage = "A bekövetkezési valószínűségnek 1 és 5 közötti egész számnak kell lennie.")]
    public int Probability { get; set; }

    /// <summary>Hatás súlyossága (1 = elhanyagolható ... 5 = kritikus üzleti/biztonsági kár).</summary>
    [Range(1, 5, ErrorMessage = "A hatás súlyosságának 1 és 5 közötti egész számnak kell lennie.")]
    public int Impact { get; set; }

    /// <summary>Automatikusan számított kockázati pontszám: Probability × Impact (1–25).</summary>
    public int Score { get; set; }

    /// <summary>Automatikusan meghatározott kockázati szint (Low, Medium, High, Critical).</summary>
    public RiskLevel Level { get; set; }

    /// <summary>A kockázat aktuális státusza (Open, UnderReview, Mitigated, Closed).</summary>
    public RiskStatus Status { get; set; } = RiskStatus.Open;

    /// <summary>A kockázat rögzítésének időpontja (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Az utolsó módosítás időpontja (UTC). Null, ha még nem módosították.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

