// DashboardSummaryCalculatorTests.cs
// Egységtesztek a vezetői Dashboard összesítő logikájához (DashboardSummaryCalculator).
//
// Futtatás GitHub Codespacesben:
//   dotnet test backend/AutomotiveRiskManager.Tests
//
// Ellenőrzött üzleti szabályok:
//   1. Üres adathalmaz esetén minden összesítés nulla, és a listák üresek.
//   2. Beszállítók (Active / Inactive) és kockázati szintek (Low, Medium, High, Critical) darabszámai helyesek.
//   3. Magas és kritikus kockázatok összevont értéke (HighAndCriticalCount) helyes.
//   4. Intézkedésstátuszok (Planned, InProgress, Completed, Cancelled, OpenActionsCount) összesítése helyes.
//   5. Lejárt határidejű intézkedések (OverdueCount és OverdueMitigations) számítása helyes.
//   6. Befejezett (Completed) vagy visszavont (Cancelled) intézkedés nem számít lejártnak.
//   7. Határidő nélküli (DueDate == null) intézkedés nem számít lejártnak, de a NoDueDateCount számolja.
//   8. A legfontosabb nyitott kockázatok (TopOpenRisks) rendezése pontszám szerint csökkenő, max. 5 rekord.
//   9. A következő 7 napban esedékes intézkedések (UpcomingMitigations) pontosan jelennek meg.

using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Services;
using Xunit;

namespace AutomotiveRiskManager.Tests;

public class DashboardSummaryCalculatorTests
{
    private static readonly DateTimeOffset FixedNowUtc =
        new(2026, 11, 11, 14, 30, 0, TimeSpan.Zero);

    private static Risk CreateRisk(
        Guid supplierId,
        string title,
        int probability,
        int impact,
        RiskStatus status = RiskStatus.Open,
        DateTimeOffset? createdAt = null)
    {
        var eval = RiskCalculator.Evaluate(probability, impact);
        return new Risk
        {
            Id          = Guid.NewGuid(),
            SupplierId  = supplierId,
            Title       = title,
            Category    = RiskCategories.NetworkSecurity,
            Probability = probability,
            Impact      = impact,
            Score       = eval.Score,
            Level       = eval.Level,
            Status      = status,
            CreatedAt   = createdAt ?? FixedNowUtc.AddDays(-10)
        };
    }

    [Fact]
    public void CalculateFromCollections_EmptyDataset_ReturnsAllZerosAndEmptyLists()
    {
        // Act
        var summary = DashboardSummaryCalculator.CalculateFromCollections(
            suppliers:        Array.Empty<Supplier>(),
            risks:            Array.Empty<Risk>(),
            mitigations:      Array.Empty<MitigationAction>(),
            referenceNowUtc:  FixedNowUtc);

        // Assert – Beszállítók
        Assert.Equal(0, summary.Suppliers.TotalCount);
        Assert.Equal(0, summary.Suppliers.ActiveCount);
        Assert.Equal(0, summary.Suppliers.InactiveCount);

        // Assert – Kockázatok
        Assert.Equal(0, summary.Risks.TotalCount);
        Assert.Equal(0, summary.Risks.LowCount);
        Assert.Equal(0, summary.Risks.MediumCount);
        Assert.Equal(0, summary.Risks.HighCount);
        Assert.Equal(0, summary.Risks.CriticalCount);
        Assert.Equal(0, summary.Risks.HighAndCriticalCount);
        Assert.Equal(0, summary.Risks.OpenCount);
        Assert.Equal(0, summary.Risks.UnderReviewCount);
        Assert.Equal(0, summary.Risks.MitigatedCount);
        Assert.Equal(0, summary.Risks.ClosedCount);

        // Assert – Intézkedések
        Assert.Equal(0, summary.Mitigations.TotalCount);
        Assert.Equal(0, summary.Mitigations.PlannedCount);
        Assert.Equal(0, summary.Mitigations.InProgressCount);
        Assert.Equal(0, summary.Mitigations.CompletedCount);
        Assert.Equal(0, summary.Mitigations.CancelledCount);
        Assert.Equal(0, summary.Mitigations.OpenActionsCount);
        Assert.Equal(0, summary.Mitigations.OverdueCount);
        Assert.Equal(0, summary.Mitigations.NoDueDateCount);

        // Assert – Figyelmet igénylő listák
        Assert.Empty(summary.TopOpenRisks);
        Assert.Empty(summary.OverdueMitigations);
        Assert.Empty(summary.UpcomingMitigations);
    }

    [Fact]
    public void CalculateFromCollections_RiskLevelsAndHighCriticalCombined_AreCalculatedCorrectly()
    {
        var supplierId = Guid.NewGuid();
        var suppliers = new[]
        {
            new Supplier { Id = supplierId, Name = "Beszállító A", Activity = "Elektronika", Status = SupplierStatus.Active },
            new Supplier { Id = Guid.NewGuid(), Name = "Beszállító B", Activity = "Logisztika", Status = SupplierStatus.Inactive }
        };

        var risks = new[]
        {
            CreateRisk(supplierId, "Alacsony 1", 1, 2, RiskStatus.Open),         // 2  -> Low
            CreateRisk(supplierId, "Közepes 1",  2, 3, RiskStatus.UnderReview),  // 6  -> Medium
            CreateRisk(supplierId, "Közepes 2",  3, 3, RiskStatus.Mitigated),    // 9  -> Medium
            CreateRisk(supplierId, "Magas 1",    3, 4, RiskStatus.Open),         // 12 -> High
            CreateRisk(supplierId, "Magas 2",    4, 4, RiskStatus.Open),         // 16 -> High
            CreateRisk(supplierId, "Kritikus 1", 4, 5, RiskStatus.Open),         // 20 -> Critical
            CreateRisk(supplierId, "Kritikus 2", 5, 5, RiskStatus.Closed)        // 25 -> Critical (Closed)
        };

        // Act
        var summary = DashboardSummaryCalculator.CalculateFromCollections(
            suppliers,
            risks,
            Array.Empty<MitigationAction>(),
            FixedNowUtc);

        // Assert – Beszállítók
        Assert.Equal(2, summary.Suppliers.TotalCount);
        Assert.Equal(1, summary.Suppliers.ActiveCount);
        Assert.Equal(1, summary.Suppliers.InactiveCount);

        // Assert – Kockázati szintek és összevont Magas + Kritikus KPI
        Assert.Equal(7, summary.Risks.TotalCount);
        Assert.Equal(1, summary.Risks.LowCount);
        Assert.Equal(2, summary.Risks.MediumCount);
        Assert.Equal(2, summary.Risks.HighCount);
        Assert.Equal(2, summary.Risks.CriticalCount);
        Assert.Equal(4, summary.Risks.HighAndCriticalCount); // 2 High + 2 Critical = 4

        // Assert – Kockázati státuszok
        Assert.Equal(4, summary.Risks.OpenCount);
        Assert.Equal(1, summary.Risks.UnderReviewCount);
        Assert.Equal(1, summary.Risks.MitigatedCount);
        Assert.Equal(1, summary.Risks.ClosedCount);
    }

    [Fact]
    public void CalculateFromCollections_MitigationStatusesOverdueAndNoDueDate_FollowMitigationPolicyRules()
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "Teszt Beszállító Kft.",
            Activity = "Szoftverfejlesztés",
            Status = SupplierStatus.Active
        };
        var risk = CreateRisk(supplier.Id, "Kritikus VPN kockázat", 4, 5, RiskStatus.Open);

        var pastDue1  = FixedNowUtc.AddDays(-5);
        var pastDue2  = FixedNowUtc.AddDays(-2);
        var futureDue = FixedNowUtc.AddDays(10);

        var mitigations = new[]
        {
            // 1. Tervezett + múltbeli határidő -> LEJÁRT (Overdue)
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Lejárt tervezett",
                ResponsiblePerson = "Felelős 1", Status = MitigationStatus.Planned,
                Priority = MitigationPriority.High, DueDate = pastDue1
            },
            // 2. Folyamatban + múltbeli határidő -> LEJÁRT (Overdue)
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Lejárt folyamatban",
                ResponsiblePerson = "Felelős 2", Status = MitigationStatus.InProgress,
                Priority = MitigationPriority.Critical, DueDate = pastDue2
            },
            // 3. Befejezett + múltbeli határidő -> NEM lejárt!
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Befejezett régi határidővel",
                ResponsiblePerson = "Felelős 3", Status = MitigationStatus.Completed,
                Priority = MitigationPriority.Medium, DueDate = pastDue1, CompletedAt = FixedNowUtc.AddDays(-1)
            },
            // 4. Visszavont + múltbeli határidő -> NEM lejárt!
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Visszavont régi határidővel",
                ResponsiblePerson = "Felelős 4", Status = MitigationStatus.Cancelled,
                Priority = MitigationPriority.Low, DueDate = pastDue1
            },
            // 5. Határidő nélküli (DueDate == null) + Planned -> NEM lejárt, de NoDueDateCount számolja!
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Határidő nélküli intézkedés",
                ResponsiblePerson = "Felelős 5", Status = MitigationStatus.Planned,
                Priority = MitigationPriority.Medium, DueDate = null
            },
            // 6. Jövőbeli határidő + InProgress -> NEM lejárt
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Jövőbeli folyamatban",
                ResponsiblePerson = "Felelős 6", Status = MitigationStatus.InProgress,
                Priority = MitigationPriority.High, DueDate = futureDue
            }
        };

        // Act
        var summary = DashboardSummaryCalculator.CalculateFromCollections(
            new[] { supplier },
            new[] { risk },
            mitigations,
            FixedNowUtc);

        // Assert
        Assert.Equal(6, summary.Mitigations.TotalCount);
        Assert.Equal(2, summary.Mitigations.PlannedCount);
        Assert.Equal(2, summary.Mitigations.InProgressCount);
        Assert.Equal(1, summary.Mitigations.CompletedCount);
        Assert.Equal(1, summary.Mitigations.CancelledCount);
        Assert.Equal(4, summary.Mitigations.OpenActionsCount); // 2 Planned + 2 InProgress
        Assert.Equal(2, summary.Mitigations.OverdueCount);     // Csak az 1. és 2. intézkedés
        Assert.Equal(1, summary.Mitigations.NoDueDateCount);   // Csak az 5. intézkedés

        Assert.Equal(2, summary.OverdueMitigations.Count);
        Assert.Equal("Lejárt tervezett", summary.OverdueMitigations[0].Title);
        Assert.Equal("Lejárt folyamatban", summary.OverdueMitigations[1].Title);
        Assert.All(summary.OverdueMitigations, item => Assert.True(item.IsOverdue));
    }

    [Fact]
    public void CalculateFromCollections_TopOpenRisks_OrdersByScoreDescendingAndLimitsToFive()
    {
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier { Id = supplierId, Name = "Autóipari Partner Zrt.", Activity = "Gyártás" };

        var risks = new[]
        {
            CreateRisk(supplierId, "Pontszám 4 (Open)",         2, 2, RiskStatus.Open),
            CreateRisk(supplierId, "Pontszám 25 (Closed)",      5, 5, RiskStatus.Closed),      // Lezárt -> nem kerülhet be!
            CreateRisk(supplierId, "Pontszám 20 (Mitigated)",   4, 5, RiskStatus.Mitigated),   // Már kezelt -> nem kerül be!
            CreateRisk(supplierId, "Pontszám 20 (Open)",        4, 5, RiskStatus.Open),
            CreateRisk(supplierId, "Pontszám 16 (UnderReview)", 4, 4, RiskStatus.UnderReview),
            CreateRisk(supplierId, "Pontszám 15 (Open)",        3, 5, RiskStatus.Open),
            CreateRisk(supplierId, "Pontszám 12 (Open)",        3, 4, RiskStatus.Open),
            CreateRisk(supplierId, "Pontszám 9 (UnderReview)",  3, 3, RiskStatus.UnderReview)
        };

        // Act
        var summary = DashboardSummaryCalculator.CalculateFromCollections(
            new[] { supplier },
            risks,
            Array.Empty<MitigationAction>(),
            FixedNowUtc);

        // Assert: Maximum 5 elem, csökkenő pontszám sorrendben: 20, 16, 15, 12, 9
        Assert.Equal(5, summary.TopOpenRisks.Count);
        Assert.Equal(new[] { 20, 16, 15, 12, 9 }, summary.TopOpenRisks.Select(r => r.Score).ToArray());
        Assert.Equal("Autóipari Partner Zrt.", summary.TopOpenRisks[0].SupplierName);
    }

    [Fact]
    public void CalculateFromCollections_UpcomingMitigations_IncludesOnlyNextSevenDaysOpenActions()
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Beszállító Kft.", Activity = "IT" };
        var risk = CreateRisk(supplier.Id, "Adatvédelmi kockázat", 3, 4, RiskStatus.Open);

        var todayStart = DashboardSummaryCalculator.GetTodayStartUtc(FixedNowUtc);

        var mitigations = new[]
        {
            // Tegnapi -> lejárt, NEM közelgő
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Tegnapi lejárt",
                ResponsiblePerson = "A", Status = MitigationStatus.Planned,
                Priority = MitigationPriority.High, DueDate = todayStart.AddDays(-1)
            },
            // Mai esedékesség -> KÖZELGŐ (0. nap)
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Ma esedékes",
                ResponsiblePerson = "B", Status = MitigationStatus.InProgress,
                Priority = MitigationPriority.Critical, DueDate = todayStart
            },
            // 3 nap múlva esedékes -> KÖZELGŐ
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "3 nap múlva esedékes",
                ResponsiblePerson = "C", Status = MitigationStatus.Planned,
                Priority = MitigationPriority.High, DueDate = todayStart.AddDays(3)
            },
            // Pontosan 7 nap múlva esedékes -> KÖZELGŐ
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "7 nap múlva esedékes",
                ResponsiblePerson = "D", Status = MitigationStatus.Planned,
                Priority = MitigationPriority.Medium, DueDate = todayStart.AddDays(7)
            },
            // 8 nap múlva esedékes -> kívül esik a 7 napos ablakon!
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "8 nap múlva esedékes",
                ResponsiblePerson = "E", Status = MitigationStatus.Planned,
                Priority = MitigationPriority.High, DueDate = todayStart.AddDays(8)
            },
            // 2 nap múlva esedékes, de már Completed -> NEM közelgő!
            new MitigationAction
            {
                Id = Guid.NewGuid(), RiskId = risk.Id, Title = "Már befejezett",
                ResponsiblePerson = "F", Status = MitigationStatus.Completed,
                Priority = MitigationPriority.High, DueDate = todayStart.AddDays(2)
            }
        };

        // Act
        var summary = DashboardSummaryCalculator.CalculateFromCollections(
            new[] { supplier },
            new[] { risk },
            mitigations,
            FixedNowUtc);

        // Assert
        Assert.Equal(3, summary.UpcomingMitigations.Count);
        Assert.Equal(
            new[] { "Ma esedékes", "3 nap múlva esedékes", "7 nap múlva esedékes" },
            summary.UpcomingMitigations.Select(m => m.Title).ToArray());
        Assert.All(summary.UpcomingMitigations, item => Assert.False(item.IsOverdue));
    }
}

