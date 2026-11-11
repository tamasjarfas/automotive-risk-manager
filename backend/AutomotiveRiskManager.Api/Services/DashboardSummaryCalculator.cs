// Services/DashboardSummaryCalculator.cs
// Központi, determinisztikus és egységtesztelhető összesítő logika a vezetői Dashboardhoz.
//
// Szakdolgozati jelentősége:
//   - Egységes dátumhatárokat biztosít az EF Core SQL-lekérdezések (DashboardController)
//     és a memóriabeli egységtesztek számára, a MitigationPolicy.IsOverdue szabályaira építve.
//   - Kiszámítja az összes KPI-t (pl. HighAndCriticalCount, OpenActionsCount, OverdueCount),
//     és előállítja a Top 5 figyelmet igénylő listát (legmagasabb pontszámú nyitott kockázatok,
//     lejárt intézkedések, következő 7 napban esedékes intézkedések).
//   - Az időforrás (referenceNowUtc) paraméterként injektálható, így a tesztek
//     teljesen determinisztikusak.

using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.Services;

public static class DashboardSummaryCalculator
{
    public const int MaxAttentionItems = 5;
    public const int UpcomingWindowDays = 7;

    /// <summary>
    /// Az aktuális UTC naptári nap kezdete (00:00:00Z).
    /// Minden olyan DueDate, amely ennél kisebb (DueDate &lt; TodayStartUtc),
    /// a MitigationPolicy.IsOverdue szabályával megegyezően lejártnak minősül
    /// (ha az intézkedés még Planned vagy InProgress).
    /// </summary>
    public static DateTimeOffset GetTodayStartUtc(DateTimeOffset referenceNowUtc)
    {
        var utcDate = referenceNowUtc.ToUniversalTime().UtcDateTime.Date;
        return new DateTimeOffset(utcDate, TimeSpan.Zero);
    }

    /// <summary>
    /// A „következő 7 napon belül esedékes" időablak felső (kizáró) határa UTC-ben.
    /// Tartomány: [todayStartUtc, todayStartUtc + 8 nap) -> a mai nap és a következő 7 naptári nap.
    /// </summary>
    public static DateTimeOffset GetUpcomingWindowEndExclusiveUtc(
        DateTimeOffset referenceNowUtc,
        int windowDays = UpcomingWindowDays)
    {
        var todayStart = GetTodayStartUtc(referenceNowUtc);
        return todayStart.AddDays(windowDays + 1);
    }

    /// <summary>
    /// Meghatározza, hogy egy intézkedés a következő 7 napon belül esedékes-e
    /// (nem lejárt, van határideje, még Planned vagy InProgress, és a határidő
    /// a [ma .. ma+7 nap] naptári időszakba esik).
    /// </summary>
    public static bool IsUpcomingWithinDays(
        DateTimeOffset? dueDate,
        MitigationStatus status,
        DateTimeOffset referenceNowUtc,
        int windowDays = UpcomingWindowDays)
    {
        if (!dueDate.HasValue)
        {
            return false;
        }

        if (status is MitigationStatus.Completed or MitigationStatus.Cancelled)
        {
            return false;
        }

        var todayStart = GetTodayStartUtc(referenceNowUtc);
        var windowEndExclusive = GetUpcomingWindowEndExclusiveUtc(referenceNowUtc, windowDays);
        var dueUtc = dueDate.Value.ToUniversalTime();

        return dueUtc >= todayStart && dueUtc < windowEndExclusive;
    }

    /// <summary>
    /// Összeállítja a DashboardSuppliersSummaryDto objektumot a darabszámokból.
    /// </summary>
    public static DashboardSuppliersSummaryDto CreateSuppliersSummary(int activeCount, int inactiveCount)
    {
        return new DashboardSuppliersSummaryDto
        {
            TotalCount    = activeCount + inactiveCount,
            ActiveCount   = activeCount,
            InactiveCount = inactiveCount
        };
    }

    /// <summary>
    /// Összeállítja a DashboardRisksSummaryDto objektumot a szint- és státuszdarabszámokból.
    /// </summary>
    public static DashboardRisksSummaryDto CreateRisksSummary(
        int lowCount,
        int mediumCount,
        int highCount,
        int criticalCount,
        int openCount,
        int underReviewCount,
        int mitigatedCount,
        int closedCount)
    {
        return new DashboardRisksSummaryDto
        {
            TotalCount           = lowCount + mediumCount + highCount + criticalCount,
            LowCount             = lowCount,
            MediumCount          = mediumCount,
            HighCount            = highCount,
            CriticalCount        = criticalCount,
            HighAndCriticalCount = highCount + criticalCount,
            OpenCount            = openCount,
            UnderReviewCount     = underReviewCount,
            MitigatedCount       = mitigatedCount,
            ClosedCount          = closedCount
        };
    }

    /// <summary>
    /// Összeállítja a DashboardMitigationsSummaryDto objektumot a státusz- és határidő-darabszámokból.
    /// </summary>
    public static DashboardMitigationsSummaryDto CreateMitigationsSummary(
        int plannedCount,
        int inProgressCount,
        int completedCount,
        int cancelledCount,
        int overdueCount,
        int noDueDateCount)
    {
        return new DashboardMitigationsSummaryDto
        {
            TotalCount       = plannedCount + inProgressCount + completedCount + cancelledCount,
            PlannedCount     = plannedCount,
            InProgressCount  = inProgressCount,
            CompletedCount   = completedCount,
            CancelledCount   = cancelledCount,
            OpenActionsCount = plannedCount + inProgressCount,
            OverdueCount     = overdueCount,
            NoDueDateCount   = noDueDateCount
        };
    }

    /// <summary>
    /// Determinisztikus, memóriabeli összesítő metódus (egységtesztekhez és referencia-számításhoz).
    /// Pontosan ugyanazokat az üzleti szabályokat alkalmazza, mint az EF Core lekérdezések.
    /// </summary>
    public static DashboardSummaryDto CalculateFromCollections(
        IEnumerable<Supplier> suppliers,
        IEnumerable<Risk> risks,
        IEnumerable<MitigationAction> mitigations,
        DateTimeOffset referenceNowUtc)
    {
        var supplierList   = (suppliers ?? Enumerable.Empty<Supplier>()).ToList();
        var riskList       = (risks ?? Enumerable.Empty<Risk>()).ToList();
        var mitigationList = (mitigations ?? Enumerable.Empty<MitigationAction>()).ToList();

        var nowUtc = referenceNowUtc.ToUniversalTime();

        // 1. Beszállítók összesítése
        int activeSuppliers   = supplierList.Count(s => s.Status == SupplierStatus.Active);
        int inactiveSuppliers = supplierList.Count(s => s.Status == SupplierStatus.Inactive);
        var suppliersSummary  = CreateSuppliersSummary(activeSuppliers, inactiveSuppliers);

        // 2. Kockázatok összesítése
        int lowRisks         = riskList.Count(r => r.Level == RiskLevel.Low);
        int mediumRisks      = riskList.Count(r => r.Level == RiskLevel.Medium);
        int highRisks        = riskList.Count(r => r.Level == RiskLevel.High);
        int criticalRisks    = riskList.Count(r => r.Level == RiskLevel.Critical);
        int openRisks        = riskList.Count(r => r.Status == RiskStatus.Open);
        int underReviewRisks = riskList.Count(r => r.Status == RiskStatus.UnderReview);
        int mitigatedRisks   = riskList.Count(r => r.Status == RiskStatus.Mitigated);
        int closedRisks      = riskList.Count(r => r.Status == RiskStatus.Closed);

        var risksSummary = CreateRisksSummary(
            lowCount:         lowRisks,
            mediumCount:      mediumRisks,
            highCount:        highRisks,
            criticalCount:    criticalRisks,
            openCount:        openRisks,
            underReviewCount: underReviewRisks,
            mitigatedCount:   mitigatedRisks,
            closedCount:      closedRisks);

        // 3. Intézkedések összesítése (a MitigationPolicy.IsOverdue szabályával)
        int plannedMitigations    = mitigationList.Count(m => m.Status == MitigationStatus.Planned);
        int inProgressMitigations = mitigationList.Count(m => m.Status == MitigationStatus.InProgress);
        int completedMitigations  = mitigationList.Count(m => m.Status == MitigationStatus.Completed);
        int cancelledMitigations  = mitigationList.Count(m => m.Status == MitigationStatus.Cancelled);
        int overdueMitigations    = mitigationList.Count(m =>
            MitigationPolicy.IsOverdue(m.DueDate, m.Status, nowUtc));
        int noDueDateMitigations  = mitigationList.Count(m => !m.DueDate.HasValue);

        var mitigationsSummary = CreateMitigationsSummary(
            plannedCount:    plannedMitigations,
            inProgressCount: inProgressMitigations,
            completedCount:  completedMitigations,
            cancelledCount:  cancelledMitigations,
            overdueCount:    overdueMitigations,
            noDueDateCount:  noDueDateMitigations);

        // Szótárak a kapcsolódó nevek feloldásához (ha a navigációs property nincs kitöltve)
        var supplierMap = supplierList.ToDictionary(s => s.Id);
        var riskMap     = riskList.ToDictionary(r => r.Id);

        // 4. Figyelmet igénylő tétel #1: Top 5 nyitott / felülvizsgálat alatt álló kockázat pontszám szerint
        var topOpenRisks = riskList
            .Where(r => r.Status is RiskStatus.Open or RiskStatus.UnderReview)
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.CreatedAt)
            .Take(MaxAttentionItems)
            .Select(r =>
            {
                string supplierName = r.Supplier?.Name
                    ?? (supplierMap.TryGetValue(r.SupplierId, out var sup) ? sup.Name : string.Empty);

                return new DashboardTopRiskItemDto
                {
                    Id           = r.Id,
                    Title        = r.Title,
                    Category     = r.Category,
                    SupplierId   = r.SupplierId,
                    SupplierName = supplierName,
                    Probability  = r.Probability,
                    Impact       = r.Impact,
                    Score        = r.Score,
                    Level        = r.Level.ToString(),
                    Status       = r.Status.ToString(),
                    CreatedAt    = r.CreatedAt
                };
            })
            .ToList();

        // Segédfüggvény MitigationAction -> DashboardMitigationItemDto leképezéshez
        DashboardMitigationItemDto MapMitigationItem(MitigationAction m, bool isOverdueFlag)
        {
            var risk = m.Risk ?? (riskMap.TryGetValue(m.RiskId, out var foundRisk) ? foundRisk : null);
            string supplierName = risk?.Supplier?.Name
                ?? (risk is not null && supplierMap.TryGetValue(risk.SupplierId, out var sup) ? sup.Name : string.Empty);

            return new DashboardMitigationItemDto
            {
                Id                = m.Id,
                Title             = m.Title,
                ResponsiblePerson = m.ResponsiblePerson,
                RiskId            = m.RiskId,
                RiskTitle         = risk?.Title ?? string.Empty,
                RiskScore         = risk?.Score ?? 0,
                RiskLevel         = risk?.Level.ToString() ?? string.Empty,
                SupplierId        = risk?.SupplierId ?? Guid.Empty,
                SupplierName      = supplierName,
                DueDate           = m.DueDate,
                Status            = m.Status.ToString(),
                Priority          = m.Priority.ToString(),
                IsOverdue         = isOverdueFlag
            };
        }

        // 5. Figyelmet igénylő tétel #2: Top 5 lejárt határidejű intézkedés (legrégebben lejárt elöl)
        var overdueItems = mitigationList
            .Where(m => MitigationPolicy.IsOverdue(m.DueDate, m.Status, nowUtc))
            .OrderBy(m => m.DueDate)
            .ThenByDescending(m => m.Priority)
            .Take(MaxAttentionItems)
            .Select(m => MapMitigationItem(m, isOverdueFlag: true))
            .ToList();

        // 6. Figyelmet igénylő tétel #3: Top 5 közelgő határidő a következő 7 napon belül
        var upcomingItems = mitigationList
            .Where(m => IsUpcomingWithinDays(m.DueDate, m.Status, nowUtc, UpcomingWindowDays))
            .OrderBy(m => m.DueDate)
            .ThenByDescending(m => m.Priority)
            .Take(MaxAttentionItems)
            .Select(m => MapMitigationItem(m, isOverdueFlag: false))
            .ToList();

        return new DashboardSummaryDto
        {
            GeneratedAtUtc      = nowUtc,
            Suppliers           = suppliersSummary,
            Risks               = risksSummary,
            Mitigations         = mitigationsSummary,
            TopOpenRisks        = topOpenRisks,
            OverdueMitigations  = overdueItems,
            UpcomingMitigations = upcomingItems
        };
    }
}

