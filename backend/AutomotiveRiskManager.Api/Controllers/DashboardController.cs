// Controllers/DashboardController.cs
// REST API vezérlő a vezetői Dashboard összesített mutatóinak lekérdezéséhez.
//
// Végpont:
//   GET /api/dashboard/summary – összesített KPI-k, kockázati és intézkedési
//                                megoszlások, valamint figyelmet igénylő Top 5 listák.
//
// Tervezési és teljesítményelvek:
//   - Minden mutató közvetlenül a PostgreSQL adatbázisból, szerveroldali EF Core
//     lekérdezésekkel (GroupBy, CountAsync, OrderBy, Take(5)) és AsNoTracking()
//     használatával számolódik. Nem töltjük be a teljes táblákat a memóriába.
//   - A lejárt és a 7 napon belül esedékes határidők számítása a központi
//     DashboardSummaryCalculator / MitigationPolicy szabályait használja.
//   - Csak olvasási művelet (GET): minden bejelentkezett szerepkör (Administrator,
//     RiskManager, Viewer) elérheti; bejelentkezés nélkül 401 Unauthorized.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Data;
using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;
using AutomotiveRiskManager.Api.Services;

namespace AutomotiveRiskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AppPolicies.ViewDashboard)]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ─── GET /api/dashboard/summary ───────────────────────────────────────────
    /// <summary>
    /// Vezetői összesítő adatok lekérdezése a PostgreSQL adatbázis aktuális állapotából.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSummary()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var todayStartUtc = DashboardSummaryCalculator.GetTodayStartUtc(nowUtc);
        var upcomingEndExclusiveUtc = DashboardSummaryCalculator.GetUpcomingWindowEndExclusiveUtc(
            nowUtc,
            DashboardSummaryCalculator.UpcomingWindowDays);

        // 1. Beszállítók összesítése státusz szerint (szerveroldali GroupBy)
        var supplierCounts = await _db.Suppliers
            .AsNoTracking()
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int activeSuppliers   = supplierCounts.FirstOrDefault(x => x.Status == SupplierStatus.Active)?.Count ?? 0;
        int inactiveSuppliers = supplierCounts.FirstOrDefault(x => x.Status == SupplierStatus.Inactive)?.Count ?? 0;

        var suppliersSummary = DashboardSummaryCalculator.CreateSuppliersSummary(
            activeSuppliers,
            inactiveSuppliers);

        // 2. Kockázatok összesítése kockázati szint szerint (szerveroldali GroupBy)
        var riskLevelCounts = await _db.Risks
            .AsNoTracking()
            .GroupBy(r => r.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync();

        // Kockázatok összesítése státusz szerint (szerveroldali GroupBy)
        var riskStatusCounts = await _db.Risks
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var risksSummary = DashboardSummaryCalculator.CreateRisksSummary(
            lowCount:         riskLevelCounts.FirstOrDefault(x => x.Level == RiskLevel.Low)?.Count ?? 0,
            mediumCount:      riskLevelCounts.FirstOrDefault(x => x.Level == RiskLevel.Medium)?.Count ?? 0,
            highCount:        riskLevelCounts.FirstOrDefault(x => x.Level == RiskLevel.High)?.Count ?? 0,
            criticalCount:    riskLevelCounts.FirstOrDefault(x => x.Level == RiskLevel.Critical)?.Count ?? 0,
            openCount:        riskStatusCounts.FirstOrDefault(x => x.Status == RiskStatus.Open)?.Count ?? 0,
            underReviewCount: riskStatusCounts.FirstOrDefault(x => x.Status == RiskStatus.UnderReview)?.Count ?? 0,
            mitigatedCount:   riskStatusCounts.FirstOrDefault(x => x.Status == RiskStatus.Mitigated)?.Count ?? 0,
            closedCount:      riskStatusCounts.FirstOrDefault(x => x.Status == RiskStatus.Closed)?.Count ?? 0);

        // 3. Intézkedések összesítése végrehajtási állapot és határidő szerint
        var mitigationStatusCounts = await _db.MitigationActions
            .AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int overdueCount = await _db.MitigationActions
            .AsNoTracking()
            .CountAsync(m =>
                m.DueDate.HasValue &&
                m.DueDate.Value < todayStartUtc &&
                m.Status != MitigationStatus.Completed &&
                m.Status != MitigationStatus.Cancelled);

        int noDueDateCount = await _db.MitigationActions
            .AsNoTracking()
            .CountAsync(m => !m.DueDate.HasValue);

        var mitigationsSummary = DashboardSummaryCalculator.CreateMitigationsSummary(
            plannedCount:    mitigationStatusCounts.FirstOrDefault(x => x.Status == MitigationStatus.Planned)?.Count ?? 0,
            inProgressCount: mitigationStatusCounts.FirstOrDefault(x => x.Status == MitigationStatus.InProgress)?.Count ?? 0,
            completedCount:  mitigationStatusCounts.FirstOrDefault(x => x.Status == MitigationStatus.Completed)?.Count ?? 0,
            cancelledCount:  mitigationStatusCounts.FirstOrDefault(x => x.Status == MitigationStatus.Cancelled)?.Count ?? 0,
            overdueCount:    overdueCount,
            noDueDateCount:  noDueDateCount);

        // 4. Figyelmet igénylő lista #1: Legmagasabb pontszámú nyitott vagy felülvizsgálat alatt álló kockázatok (max. 5)
        var topOpenRisks = await _db.Risks
            .AsNoTracking()
            .Where(r => r.Status == RiskStatus.Open || r.Status == RiskStatus.UnderReview)
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.CreatedAt)
            .Take(DashboardSummaryCalculator.MaxAttentionItems)
            .Select(r => new DashboardTopRiskItemDto
            {
                Id           = r.Id,
                Title        = r.Title,
                Category     = r.Category,
                SupplierId   = r.SupplierId,
                SupplierName = r.Supplier != null ? r.Supplier.Name : string.Empty,
                Probability  = r.Probability,
                Impact       = r.Impact,
                Score        = r.Score,
                Level        = r.Level.ToString(),
                Status       = r.Status.ToString(),
                CreatedAt    = r.CreatedAt
            })
            .ToListAsync();

        // 5. Figyelmet igénylő lista #2: Lejárt határidejű, még nem befejezett intézkedések (max. 5)
        var overdueMitigations = await _db.MitigationActions
            .AsNoTracking()
            .Where(m =>
                m.DueDate.HasValue &&
                m.DueDate.Value < todayStartUtc &&
                m.Status != MitigationStatus.Completed &&
                m.Status != MitigationStatus.Cancelled)
            .OrderBy(m => m.DueDate)
            .ThenByDescending(m => m.Priority)
            .Take(DashboardSummaryCalculator.MaxAttentionItems)
            .Select(m => new DashboardMitigationItemDto
            {
                Id                = m.Id,
                Title             = m.Title,
                ResponsiblePerson = m.ResponsiblePerson,
                RiskId            = m.RiskId,
                RiskTitle         = m.Risk != null ? m.Risk.Title : string.Empty,
                RiskScore         = m.Risk != null ? m.Risk.Score : 0,
                RiskLevel         = m.Risk != null ? m.Risk.Level.ToString() : string.Empty,
                SupplierId        = m.Risk != null ? m.Risk.SupplierId : Guid.Empty,
                SupplierName      = m.Risk != null && m.Risk.Supplier != null ? m.Risk.Supplier.Name : string.Empty,
                DueDate           = m.DueDate,
                Status            = m.Status.ToString(),
                Priority          = m.Priority.ToString(),
                IsOverdue         = true
            })
            .ToListAsync();

        // 6. Figyelmet igénylő lista #3: Közelgő határidők a következő 7 napon belül (max. 5)
        var upcomingMitigations = await _db.MitigationActions
            .AsNoTracking()
            .Where(m =>
                m.DueDate.HasValue &&
                m.DueDate.Value >= todayStartUtc &&
                m.DueDate.Value < upcomingEndExclusiveUtc &&
                m.Status != MitigationStatus.Completed &&
                m.Status != MitigationStatus.Cancelled)
            .OrderBy(m => m.DueDate)
            .ThenByDescending(m => m.Priority)
            .Take(DashboardSummaryCalculator.MaxAttentionItems)
            .Select(m => new DashboardMitigationItemDto
            {
                Id                = m.Id,
                Title             = m.Title,
                ResponsiblePerson = m.ResponsiblePerson,
                RiskId            = m.RiskId,
                RiskTitle         = m.Risk != null ? m.Risk.Title : string.Empty,
                RiskScore         = m.Risk != null ? m.Risk.Score : 0,
                RiskLevel         = m.Risk != null ? m.Risk.Level.ToString() : string.Empty,
                SupplierId        = m.Risk != null ? m.Risk.SupplierId : Guid.Empty,
                SupplierName      = m.Risk != null && m.Risk.Supplier != null ? m.Risk.Supplier.Name : string.Empty,
                DueDate           = m.DueDate,
                Status            = m.Status.ToString(),
                Priority          = m.Priority.ToString(),
                IsOverdue         = false
            })
            .ToListAsync();

        var response = new DashboardSummaryDto
        {
            GeneratedAtUtc      = nowUtc,
            Suppliers           = suppliersSummary,
            Risks               = risksSummary,
            Mitigations         = mitigationsSummary,
            TopOpenRisks        = topOpenRisks,
            OverdueMitigations  = overdueMitigations,
            UpcomingMitigations = upcomingMitigations
        };

        return Ok(response);
    }
}

