// Models/Supplier.cs
// A Supplier entitás az adatbázis „suppliers" táblájának C# reprezentációja.
//
// Az adatmodell tervezési elvei:
//   - Guid Id: az SQL integer-id helyett UUID-t használunk, mert
//     biztonságosabb (nem sejthetők szekvenciálisan), és elosztott
//     rendszerekben is egyedi marad.
//   - DateTimeOffset: az egyszerű DateTime helyett időzóna-tudatos típust
//     használunk, ez a legjobb gyakorlat szerver oldali alkalmazásokban.
//   - [Required] és [MaxLength]: adatbázis-szintű megszorítások (EF Core
//     ezekből hozza létre a megfelelő SQL-korlátokat a migrációban).
//   - Soft delete helyett Status mező: a beszállítókat nem töröljük, hanem
//     inaktiváljuk, így az adatok auditálhatók maradnak.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomotiveRiskManager.Api.Models;

[Table("suppliers")]   // Az EF Core ezen a táblán tárolja az entitásokat
public class Supplier
{
    /// <summary>Egyedi azonosító (UUID v4) – az adatbázis elsődleges kulcsa.</summary>
    public Guid Id { get; set; }

    /// <summary>A beszállító cég neve. Kötelező, maximum 200 karakter.</summary>
    [Required(ErrorMessage = "A beszállító neve kötelező.")]
    [MaxLength(200, ErrorMessage = "A név legfeljebb 200 karakter lehet.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tevékenységi kör (pl. elektronikai alkatrészgyártás). Kötelező, max. 300 karakter.</summary>
    [Required(ErrorMessage = "A tevékenységi kör megadása kötelező.")]
    [MaxLength(300, ErrorMessage = "A tevékenységi kör legfeljebb 300 karakter lehet.")]
    public string Activity { get; set; } = string.Empty;

    /// <summary>Opcionális, részletesebb leírás a beszállítóról. Maximum 2000 karakter.</summary>
    [MaxLength(2000, ErrorMessage = "A leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; set; }

    /// <summary>A beszállító aktuális állapota: Active (aktív) vagy Inactive (inaktív).</summary>
    public SupplierStatus Status { get; set; } = SupplierStatus.Active;

    /// <summary>A rögzítés időpontja (UTC). Az adatbázis nem módosítható automatikusan.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Az utolsó módosítás időpontja (UTC). Null, ha még nem módosították.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

