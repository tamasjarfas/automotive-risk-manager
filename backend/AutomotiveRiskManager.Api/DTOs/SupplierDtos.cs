// DTOs/SupplierDtos.cs
// Adatátviteli objektumok (Data Transfer Objects) a Supplier entitáshoz.
//
// Miért kellenek a DTO-k?
//   Az API-végpontok nem adják vissza közvetlenül az entitásosztályt (Supplier),
//   hanem DTO-kon keresztül kommunikálnak. Ennek okai:
//     1. Biztonság: az entitás belső mezői (pl. navigációs property-k,
//        jövőbeli jelszókivonatok) nem kerülnek ki véletlenül a kliensnek.
//     2. Verzióbiztonság: az adatbázis-séma és az API-kontraktus egymástól
//        függetlenül fejlőhet.
//     3. Validáció: a bemeneti DTO-k tartalmazzák az ellenőrzési szabályokat.

using System.ComponentModel.DataAnnotations;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.DTOs;

// ─── Válasz DTO ───────────────────────────────────────────────────────────────
// Ezt adja vissza minden végpont, amikor egy vagy több Supplier-t kérdez le.

public sealed class SupplierDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Activity { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = string.Empty;      // "Active" vagy "Inactive"
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Supplier entitásból DTO létrehozása (leképezés).</summary>
    public static SupplierDto FromEntity(Supplier s) => new()
    {
        Id          = s.Id,
        Name        = s.Name,
        Activity    = s.Activity,
        Description = s.Description,
        Status      = s.Status.ToString(),
        CreatedAt   = s.CreatedAt,
        UpdatedAt   = s.UpdatedAt
    };
}

// ─── Létrehozási kérés DTO ────────────────────────────────────────────────────
// A POST /api/suppliers végpont ezt várja a kérés törzsében.

public sealed class CreateSupplierRequest
{
    [Required(ErrorMessage = "A beszállító neve kötelező.")]
    [MaxLength(200, ErrorMessage = "A név legfeljebb 200 karakter lehet.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "A tevékenységi kör megadása kötelező.")]
    [MaxLength(300, ErrorMessage = "A tevékenységi kör legfeljebb 300 karakter lehet.")]
    public string Activity { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; init; }
}

// ─── Módosítási kérés DTO ─────────────────────────────────────────────────────
// A PUT /api/suppliers/{id} végpont ezt várja.
// Az Id és CreatedAt nem módosítható, ezért nem szerepel itt.

public sealed class UpdateSupplierRequest
{
    [Required(ErrorMessage = "A beszállító neve kötelező.")]
    [MaxLength(200, ErrorMessage = "A név legfeljebb 200 karakter lehet.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "A tevékenységi kör megadása kötelező.")]
    [MaxLength(300, ErrorMessage = "A tevékenységi kör legfeljebb 300 karakter lehet.")]
    public string Activity { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; init; }
}

// ─── Állapotváltási kérés DTO ─────────────────────────────────────────────────
// A PATCH /api/suppliers/{id}/status végpont ezt várja.

public sealed class UpdateSupplierStatusRequest
{
    [Required(ErrorMessage = "Az állapot megadása kötelező.")]
    public string Status { get; init; } = string.Empty;    // "Active" vagy "Inactive"
}

