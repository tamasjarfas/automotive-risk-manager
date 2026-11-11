// DTOs/DashboardDtos.cs
// Adatátviteli objektumok (DTO-k) a vezetői Dashboard (/api/dashboard/summary) végponthoz.
//
// Tervezési elvek:
//   - Egyetlen, strukturált válaszban adja vissza a KPI-kártyák, a két megoszlási
//     diagram (kockázati szintek és intézkedésstátuszok), valamint a három
//     figyelmet igénylő lista (Top 5 nyitott kockázat, Top 5 lejárt intézkedés,
//     Top 5 közelgő határidejű intézkedés) adatait.
//   - Nem tartalmaz érzékeny hitelesítési vagy személyes adatokat.

namespace AutomotiveRiskManager.Api.DTOs;

/// <summary>
/// A GET /api/dashboard/summary végpont teljes válasza.
/// </summary>
public sealed class DashboardSummaryDto
{
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public DashboardSuppliersSummaryDto Suppliers { get; init; } = new();
    public DashboardRisksSummaryDto Risks { get; init; } = new();
    public DashboardMitigationsSummaryDto Mitigations { get; init; } = new();

    /// <summary>Legmagasabb pontszámú, még nyitott vagy felülvizsgálat alatt álló kockázatok (max. 5).</summary>
    public IReadOnlyList<DashboardTopRiskItemDto> TopOpenRisks { get; init; } =
        Array.Empty<DashboardTopRiskItemDto>();

    /// <summary>Lejárt határidejű, még nem befejezett/visszavont intézkedések (max. 5).</summary>
    public IReadOnlyList<DashboardMitigationItemDto> OverdueMitigations { get; init; } =
        Array.Empty<DashboardMitigationItemDto>();

    /// <summary>A következő 7 napon belül esedékes, még nyitott intézkedések (max. 5).</summary>
    public IReadOnlyList<DashboardMitigationItemDto> UpcomingMitigations { get; init; } =
        Array.Empty<DashboardMitigationItemDto>();
}

/// <summary>
/// Beszállítók összesített mutatói (SupplierStatus alapján).
/// </summary>
public sealed class DashboardSuppliersSummaryDto
{
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int InactiveCount { get; init; }
}

/// <summary>
/// Információbiztonsági kockázatok összesített mutatói (RiskLevel és RiskStatus alapján).
/// </summary>
public sealed class DashboardRisksSummaryDto
{
    public int TotalCount { get; init; }

    // Kockázati szintek szerinti megoszlás (1–4, 5–9, 10–16, 17–25)
    public int LowCount { get; init; }
    public int MediumCount { get; init; }
    public int HighCount { get; init; }
    public int CriticalCount { get; init; }

    /// <summary>Magas és kritikus kockázatok összevont száma (HighCount + CriticalCount) – vezetői KPI.</summary>
    public int HighAndCriticalCount { get; init; }

    // Státusz szerinti megoszlás
    public int OpenCount { get; init; }
    public int UnderReviewCount { get; init; }
    public int MitigatedCount { get; init; }
    public int ClosedCount { get; init; }
}

/// <summary>
/// Kockázatcsökkentő intézkedések összesített mutatói (MitigationStatus és határidők alapján).
/// </summary>
public sealed class DashboardMitigationsSummaryDto
{
    public int TotalCount { get; init; }

    // Végrehajtási állapot szerinti megoszlás
    public int PlannedCount { get; init; }
    public int InProgressCount { get; init; }
    public int CompletedCount { get; init; }
    public int CancelledCount { get; init; }

    /// <summary>Nyitott (Planned + InProgress) intézkedések száma – vezetői KPI.</summary>
    public int OpenActionsCount { get; init; }

    /// <summary>Lejárt határidejű és még nem befejezett/visszavont intézkedések száma – vezetői KPI.</summary>
    public int OverdueCount { get; init; }

    /// <summary>Határidő nélküli (DueDate == null) intézkedések száma.</summary>
    public int NoDueDateCount { get; init; }
}

/// <summary>
/// Kiemelt (legmagasabb pontszámú nyitott) kockázat elem a Dashboard listájában.
/// </summary>
public sealed class DashboardTopRiskItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public int Probability { get; init; }
    public int Impact { get; init; }
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;    // "Low" | "Medium" | "High" | "Critical"
    public string Status { get; init; } = string.Empty;   // "Open" | "UnderReview"
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Kiemelt (lejárt vagy közelgő határidejű) intézkedés elem a Dashboard listáiban.
/// </summary>
public sealed class DashboardMitigationItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ResponsiblePerson { get; init; } = string.Empty;
    public Guid RiskId { get; init; }
    public string RiskTitle { get; init; } = string.Empty;
    public int RiskScore { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public DateTimeOffset? DueDate { get; init; }
    public string Status { get; init; } = string.Empty;     // "Planned" | "InProgress"
    public string Priority { get; init; } = string.Empty;   // "Low" | "Medium" | "High" | "Critical"
    public bool IsOverdue { get; init; }
}

