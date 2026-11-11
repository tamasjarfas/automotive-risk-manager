// DTOs/MitigationDtos.cs
// Adatátviteli objektumok (Data Transfer Objects) a kockázatcsökkentő
// intézkedések (MitigationAction) modulhoz.
//
// Tervezési elvek:
//   - A válasz DTO (MitigationActionDto) a kapcsolódó kockázat (RiskTitle, RiskScore,
//     RiskLevel) és beszállító (SupplierId, SupplierName) adatait is visszaadja,
//     valamint tartalmazza a számított IsOverdue (lejárt határidő) jelzőt.
//   - Az IsOverdue jelző külön mező: a Status értékét soha nem írja felül.

using System.ComponentModel.DataAnnotations;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Services;

namespace AutomotiveRiskManager.Api.DTOs;

// ─── Válasz DTO ───────────────────────────────────────────────────────────────

public sealed class MitigationActionDto
{
    public Guid Id { get; init; }
    public Guid RiskId { get; init; }
    public string RiskTitle { get; init; } = string.Empty;
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ResponsiblePerson { get; init; } = string.Empty;
    public DateTimeOffset? DueDate { get; init; }
    public string Status { get; init; } = string.Empty;     // "Planned" | "InProgress" | "Completed" | "Cancelled"
    public string Priority { get; init; } = string.Empty;   // "Low" | "Medium" | "High" | "Critical"

    /// <summary>
    /// Számított jelző: true, ha van határidő, az már elmúlt, és az intézkedés még nincs befejezve/visszavonva.
    /// Nem írja felül a Status mezőt!
    /// </summary>
    public bool IsOverdue { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
    public string? ResultNotes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>MitigationAction entitásból válasz DTO létrehozása.</summary>
    public static MitigationActionDto FromEntity(
        MitigationAction m,
        Risk? riskOverride = null,
        DateTimeOffset? referenceNowUtc = null)
    {
        var risk = riskOverride ?? m.Risk;
        var now  = referenceNowUtc ?? DateTimeOffset.UtcNow;

        return new MitigationActionDto
        {
            Id                = m.Id,
            RiskId            = m.RiskId,
            RiskTitle         = risk?.Title ?? string.Empty,
            RiskScore         = risk?.Score ?? 0,
            RiskLevel         = risk?.Level.ToString() ?? string.Empty,
            SupplierId        = risk?.SupplierId ?? Guid.Empty,
            SupplierName      = risk?.Supplier?.Name ?? string.Empty,
            Title             = m.Title,
            Description       = m.Description,
            ResponsiblePerson = m.ResponsiblePerson,
            DueDate           = m.DueDate,
            Status            = m.Status.ToString(),
            Priority          = m.Priority.ToString(),
            IsOverdue         = MitigationPolicy.IsOverdue(m.DueDate, m.Status, now),
            CompletedAt       = m.CompletedAt,
            ResultNotes       = m.ResultNotes,
            CreatedAt         = m.CreatedAt,
            UpdatedAt         = m.UpdatedAt
        };
    }
}

// ─── Létrehozási kérés DTO (POST /api/mitigations) ────────────────────────────

public sealed class CreateMitigationRequest
{
    [Required(ErrorMessage = "A kapcsolódó kockázat azonosítójának megadása kötelező.")]
    public Guid RiskId { get; init; }

    [Required(ErrorMessage = "Az intézkedés megnevezése kötelező.")]
    [MaxLength(200, ErrorMessage = "Az intézkedés megnevezése legfeljebb 200 karakter lehet.")]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A részletes leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "A felelős megadása kötelező.")]
    [MaxLength(150, ErrorMessage = "A felelős megnevezése legfeljebb 150 karakter lehet.")]
    public string ResponsiblePerson { get; init; } = string.Empty;

    public DateTimeOffset? DueDate { get; init; }

    /// <summary>Prioritás: Low, Medium, High, Critical (alapértelmezett: Medium).</summary>
    public string Priority { get; init; } = "Medium";

    /// <summary>Opcionális kezdő státusz: Planned, InProgress, Completed, Cancelled (alapértelmezett: Planned).</summary>
    public string? Status { get; init; }

    [MaxLength(2000, ErrorMessage = "Az eredmény / megjegyzés legfeljebb 2000 karakter lehet.")]
    public string? ResultNotes { get; init; }
}

// ─── Módosítási kérés DTO (PUT /api/mitigations/{id}) ─────────────────────────

public sealed class UpdateMitigationRequest
{
    [Required(ErrorMessage = "A kapcsolódó kockázat azonosítójának megadása kötelező.")]
    public Guid RiskId { get; init; }

    [Required(ErrorMessage = "Az intézkedés megnevezése kötelező.")]
    [MaxLength(200, ErrorMessage = "Az intézkedés megnevezése legfeljebb 200 karakter lehet.")]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "A részletes leírás legfeljebb 2000 karakter lehet.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "A felelős megadása kötelező.")]
    [MaxLength(150, ErrorMessage = "A felelős megnevezése legfeljebb 150 karakter lehet.")]
    public string ResponsiblePerson { get; init; } = string.Empty;

    public DateTimeOffset? DueDate { get; init; }

    [Required(ErrorMessage = "A prioritás megadása kötelező.")]
    public string Priority { get; init; } = "Medium";

    /// <summary>Opcionális státuszfrissítés a szerkesztő űrlapról (Planned, InProgress, Completed, Cancelled).</summary>
    public string? Status { get; init; }

    [MaxLength(2000, ErrorMessage = "Az eredmény / megjegyzés legfeljebb 2000 karakter lehet.")]
    public string? ResultNotes { get; init; }
}

// ─── Státuszváltási kérés DTO (PATCH /api/mitigations/{id}/status) ────────────

public sealed class UpdateMitigationStatusRequest
{
    [Required(ErrorMessage = "Az új státusz megadása kötelező.")]
    public string Status { get; init; } = string.Empty;

    /// <summary>Opcionális eredmény / megjegyzés (pl. az intézkedés befejezésekor vagy visszavonásakor).</summary>
    [MaxLength(2000, ErrorMessage = "Az eredmény / megjegyzés legfeljebb 2000 karakter lehet.")]
    public string? ResultNotes { get; init; }
}

