// DTOs/RiskDtos.cs
// Adatátviteli objektumok (Data Transfer Objects) a kockázatértékelési modulhoz.
//
// Biztonsági tervezési elvek:
//   - A bemeneti DTO-k (CreateRiskRequest, UpdateRiskRequest) NEM tartalmaznak
//     Score (pontszám) vagy Level (kockázati szint) mezőket. Így a kliens
//     semmilyen körülmények között nem tud manipulált kockázati besorolást beküldeni:
//     a számítás kizárólag szerveroldalon, a RiskCalculator osztályban történik.
//   - A Probability és Impact mezők [Range(1, 5)] attribútummal védettek.

using System.ComponentModel.DataAnnotations;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.DTOs;

// ─── Válasz DTO ───────────────────────────────────────────────────────────────

public sealed class RiskDto
{
    public Guid Id { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public int Probability { get; init; }
    public int Impact { get; init; }
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;    // "Low" | "Medium" | "High" | "Critical"
    public string Status { get; init; } = string.Empty;   // "Open" | "UnderReview" | "Mitigated" | "Closed"
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Risk entitásból DTO létrehozása.</summary>
    public static RiskDto FromEntity(Risk r, string? supplierNameOverride = null) => new()
    {
        Id           = r.Id,
        SupplierId   = r.SupplierId,
        SupplierName = supplierNameOverride ?? r.Supplier?.Name ?? string.Empty,
        Title        = r.Title,
        Description  = r.Description,
        Category     = r.Category,
        Probability  = r.Probability,
        Impact       = r.Impact,
        Score        = r.Score,
        Level        = r.Level.ToString(),
        Status       = r.Status.ToString(),
        CreatedAt    = r.CreatedAt,
        UpdatedAt    = r.UpdatedAt
    };
}

// ─── Létrehozási kérés DTO (POST /api/risks) ──────────────────────────────────

public sealed class CreateRiskRequest
{
    [Required(ErrorMessage = "A beszállító azonosítójának megadása kötelező.")]
    public Guid SupplierId { get; init; }

    [Required(ErrorMessage = "A kockázat megnevezése kötelező.")]
    [MaxLength(200, ErrorMessage = "A kockázat megnevezése legfeljebb 200 karakter lehet.")]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A részletes leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "A kockázati kategória megadása kötelező.")]
    [MaxLength(100, ErrorMessage = "A kategória legfeljebb 100 karakter lehet.")]
    public string Category { get; init; } = string.Empty;

    [Required(ErrorMessage = "A bekövetkezési valószínűség megadása kötelező.")]
    [Range(1, 5, ErrorMessage = "A bekövetkezési valószínűségnek 1 és 5 közötti egész számnak kell lennie.")]
    public int Probability { get; init; }

    [Required(ErrorMessage = "A hatás súlyosságának megadása kötelező.")]
    [Range(1, 5, ErrorMessage = "A hatás súlyosságának 1 és 5 közötti egész számnak kell lennie.")]
    public int Impact { get; init; }
}

// ─── Módosítási kérés DTO (PUT /api/risks/{id}) ───────────────────────────────

public sealed class UpdateRiskRequest
{
    [Required(ErrorMessage = "A beszállító azonosítójának megadása kötelező.")]
    public Guid SupplierId { get; init; }

    [Required(ErrorMessage = "A kockázat megnevezése kötelező.")]
    [MaxLength(200, ErrorMessage = "A kockázat megnevezése legfeljebb 200 karakter lehet.")]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A részletes leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "A kockázati kategória megadása kötelező.")]
    [MaxLength(100, ErrorMessage = "A kategória legfeljebb 100 karakter lehet.")]
    public string Category { get; init; } = string.Empty;

    [Required(ErrorMessage = "A bekövetkezési valószínűség megadása kötelező.")]
    [Range(1, 5, ErrorMessage = "A bekövetkezési valószínűségnek 1 és 5 közötti egész számnak kell lennie.")]
    public int Probability { get; init; }

    [Required(ErrorMessage = "A hatás súlyosságának megadása kötelező.")]
    [Range(1, 5, ErrorMessage = "A hatás súlyosságának 1 és 5 közötti egész számnak kell lennie.")]
    public int Impact { get; init; }

    /// <summary>Opcionális státuszfrissítés a szerkesztő űrlapról (Open, UnderReview, Mitigated, Closed).</summary>
    public string? Status { get; init; }
}

// ─── Státuszváltási kérés DTO (PATCH /api/risks/{id}/status) ──────────────────

public sealed class UpdateRiskStatusRequest
{
    [Required(ErrorMessage = "Az új státusz megadása kötelező.")]
    public string Status { get; init; } = string.Empty;
}

