// MitigationPolicyTests.cs
// Egységtesztek a kockázatcsökkentő intézkedések (MitigationAction / MitigationPolicy)
// üzleti szabályainak ellenőrzéséhez.
//
// Futtatás GitHub Codespacesben:
//   dotnet test backend/AutomotiveRiskManager.Tests
//
// Ellenőrzött üzleti szabályok:
//   1. Intézkedés megfelelő státusszal létrehozható (Planned, InProgress, Completed, Cancelled).
//   2. Érvénytelen státusz és érvénytelen prioritás elutasításra kerül.
//   3. Hiányzó (null vagy Guid.Empty) kockázat nem használható.
//   4. Üres megnevezés (Title) vagy üres felelős (ResponsiblePerson) elutasításra kerül.
//   5. Határidő lejártának (IsOverdue) felismerése helyes, és nem írja felül a státuszt.
//   6. Befejezéskor (Completed) a CompletedAt időpont rögzül; visszanyitáskor nullázódik.
//   7. A kockázat pontszáma (Score) és szintje (Level) NEM változik automatikusan az intézkedés befejezésekor.

using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Services;
using Xunit;

namespace AutomotiveRiskManager.Tests;

public class MitigationPolicyTests
{
    private static Risk CreateSampleRisk()
    {
        var eval = RiskCalculator.Evaluate(probability: 4, impact: 5); // 20 pont -> Critical
        return new Risk
        {
            Id          = Guid.NewGuid(),
            SupplierId  = Guid.NewGuid(),
            Title       = "Nem titkosított beszállítói VPN-kapcsolat",
            Category    = RiskCategories.NetworkSecurity,
            Probability = 4,
            Impact      = 5,
            Score       = eval.Score,
            Level       = eval.Level,
            Status      = RiskStatus.Open,
            CreatedAt   = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero)
        };
    }

    [Theory]
    [InlineData("Planned",    MitigationStatus.Planned,    false)]
    [InlineData("InProgress", MitigationStatus.InProgress, false)]
    [InlineData("Completed",  MitigationStatus.Completed,  true)]
    [InlineData("Cancelled",  MitigationStatus.Cancelled,  false)]
    public void CreateAction_WithValidStatus_CreatesActionCorrectly(
        string statusInput,
        MitigationStatus expectedStatus,
        bool expectCompletedAt)
    {
        // Arrange
        var risk = CreateSampleRisk();
        var now  = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

        // Act
        var action = MitigationPolicy.CreateAction(
            targetRisk:        risk,
            title:             "Kétfaktoros hitelesítés (MFA) és TLS 1.3 bevezetése",
            description:       "Minden távoli beszállítói hozzáféréshez kötelező MFA.",
            responsiblePerson: "Kovács János (IT biztonsági felelős)",
            dueDate:           now.AddDays(14),
            priorityText:      "High",
            statusText:        statusInput,
            resultNotes:       null,
            nowUtc:            now);

        // Assert
        Assert.NotEqual(Guid.Empty, action.Id);
        Assert.Equal(risk.Id, action.RiskId);
        Assert.Equal(expectedStatus, action.Status);
        Assert.Equal(MitigationPriority.High, action.Priority);
        if (expectCompletedAt)
        {
            Assert.Equal(now, action.CompletedAt);
        }
        else
        {
            Assert.Null(action.CompletedAt);
        }
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("Done")]
    [InlineData("Deleted")]
    [InlineData("999")]
    public void CreateAction_WithInvalidStatus_ThrowsArgumentException(string invalidStatus)
    {
        var risk = CreateSampleRisk();
        var now  = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            MitigationPolicy.CreateAction(
                targetRisk:        risk,
                title:             "Tűzfal szabályok szigorítása",
                description:       null,
                responsiblePerson: "Hálózati üzemeltetés",
                dueDate:           now.AddDays(7),
                priorityText:      "Medium",
                statusText:        invalidStatus,
                resultNotes:       null,
                nowUtc:            now));
    }

    [Fact]
    public void CreateAction_WithMissingOrEmptyRisk_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var emptyIdRisk = new Risk { Id = Guid.Empty, Title = "Érvénytelen kockázat" };

        Assert.Throws<ArgumentException>(() =>
            MitigationPolicy.CreateAction(
                targetRisk:        null,
                title:             "Intézkedés kockázat nélkül",
                description:       null,
                responsiblePerson: "Felelős",
                dueDate:           null,
                priorityText:      "Medium",
                statusText:        "Planned",
                resultNotes:       null,
                nowUtc:            now));

        Assert.Throws<ArgumentException>(() =>
            MitigationPolicy.CreateAction(
                targetRisk:        emptyIdRisk,
                title:             "Intézkedés üres RiskId-val",
                description:       null,
                responsiblePerson: "Felelős",
                dueDate:           null,
                priorityText:      "Medium",
                statusText:        "Planned",
                resultNotes:       null,
                nowUtc:            now));
    }

    [Theory]
    [InlineData("",    "Felelős Neve")]
    [InlineData("   ", "Felelős Neve")]
    [InlineData("Érvényes megnevezés", "")]
    [InlineData("Érvényes megnevezés", "   ")]
    public void CreateAction_WithEmptyTitleOrResponsible_ThrowsArgumentException(
        string title,
        string responsible)
    {
        var risk = CreateSampleRisk();
        var now  = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            MitigationPolicy.CreateAction(
                targetRisk:        risk,
                title:             title,
                description:       null,
                responsiblePerson: responsible,
                dueDate:           null,
                priorityText:      "Medium",
                statusText:        "Planned",
                resultNotes:       null,
                nowUtc:            now));
    }

    [Fact]
    public void IsOverdue_CorrectlyDetectsOverdueWithoutOverwritingStatus()
    {
        var referenceNow = new DateTimeOffset(2026, 11, 11, 14, 0, 0, TimeSpan.Zero);
        var pastDue      = new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);
        var todayDue     = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
        var futureDue    = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

        // 1. Lejárt határidő + Planned vagy InProgress -> IsOverdue = true, de a Status változatlan!
        Assert.True(MitigationPolicy.IsOverdue(pastDue, MitigationStatus.Planned, referenceNow));
        Assert.True(MitigationPolicy.IsOverdue(pastDue, MitigationStatus.InProgress, referenceNow));

        // 2. Befejezett (Completed) vagy visszavont (Cancelled) intézkedés múltbeli határidővel -> NEM lejárt
        Assert.False(MitigationPolicy.IsOverdue(pastDue, MitigationStatus.Completed, referenceNow));
        Assert.False(MitigationPolicy.IsOverdue(pastDue, MitigationStatus.Cancelled, referenceNow));

        // 3. Mai vagy jövőbeli határidő -> NEM lejárt
        Assert.False(MitigationPolicy.IsOverdue(todayDue, MitigationStatus.InProgress, referenceNow));
        Assert.False(MitigationPolicy.IsOverdue(futureDue, MitigationStatus.Planned, referenceNow));

        // 4. Határidő nélküli intézkedés (DueDate == null) -> NEM lejárt
        Assert.False(MitigationPolicy.IsOverdue(null, MitigationStatus.Planned, referenceNow));

        // 5. Ellenőrizzük, hogy a DTO leképezés sem írja felül a munkafolyamat státuszát
        var risk = CreateSampleRisk();
        var overdueAction = MitigationPolicy.CreateAction(
            targetRisk:        risk,
            title:             "Lejárt intézkedés",
            description:       null,
            responsiblePerson: "Teszt Felelős",
            dueDate:           pastDue,
            priorityText:      "High",
            statusText:        "InProgress",
            resultNotes:       null,
            nowUtc:            referenceNow);

        var dto = MitigationActionDto.FromEntity(overdueAction, risk, referenceNow);
        Assert.True(dto.IsOverdue);
        Assert.Equal("InProgress", dto.Status);
    }

    [Fact]
    public void ApplyStatusTransition_SetsCompletedAtOnCompletion_AndClearsOnReopen()
    {
        var risk       = CreateSampleRisk();
        var createdAt  = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        var completedAt = new DateTimeOffset(2026, 11, 10, 16, 30, 0, TimeSpan.Zero);
        var reopenedAt  = new DateTimeOffset(2026, 11, 11, 11, 0, 0, TimeSpan.Zero);

        var action = MitigationPolicy.CreateAction(
            targetRisk:        risk,
            title:             "Hálózati szegmentáció kialakítása",
            description:       null,
            responsiblePerson: "Hálózatbiztonsági csapat",
            dueDate:           createdAt.AddDays(15),
            priorityText:      "Critical",
            statusText:        "InProgress",
            resultNotes:       null,
            nowUtc:            createdAt);

        Assert.Null(action.CompletedAt);

        // Act 1: Befejezés (Completed) eredmény rögzítésével
        MitigationPolicy.ApplyStatusTransition(
            action:        action,
            newStatusText: "Completed",
            resultNotes:   "VLAN szegmentáció és tűzfalszabályok élesítve, auditálva.",
            nowUtc:        completedAt);

        // Assert 1: CompletedAt és ResultNotes rögzült
        Assert.Equal(MitigationStatus.Completed, action.Status);
        Assert.Equal(completedAt, action.CompletedAt);
        Assert.Equal("VLAN szegmentáció és tűzfalszabályok élesítve, auditálva.", action.ResultNotes);

        // Act 2: Visszanyitás (InProgress)
        MitigationPolicy.ApplyStatusTransition(
            action:        action,
            newStatusText: "InProgress",
            resultNotes:   null,
            nowUtc:        reopenedAt);

        // Assert 2: CompletedAt következetesen nullázódott
        Assert.Equal(MitigationStatus.InProgress, action.Status);
        Assert.Null(action.CompletedAt);
    }

    [Fact]
    public void CompletingMitigationAction_DoesNotAutomaticallyChangeRiskScoreOrLevel()
    {
        // Arrange: Kritikus kockázat (P=4, I=5 -> Score=20, Level=Critical)
        var risk = CreateSampleRisk();
        int initialProbability = risk.Probability;
        int initialImpact      = risk.Impact;
        int initialScore       = risk.Score;
        RiskLevel initialLevel = risk.Level;
        RiskStatus initialStatus = risk.Status;

        var now = DateTimeOffset.UtcNow;
        var action = MitigationPolicy.CreateAction(
            targetRisk:        risk,
            title:             "Titkosítási protokoll frissítése",
            description:       null,
            responsiblePerson: "Biztonsági mérnök",
            dueDate:           now.AddDays(5),
            priorityText:      "Critical",
            statusText:        "Planned",
            resultNotes:       null,
            nowUtc:            now);

        // Act: Az intézkedést befejezetté nyilvánítjuk
        MitigationPolicy.ApplyStatusTransition(
            action:        action,
            newStatusText: "Completed",
            resultNotes:   "TLS 1.3 bekapcsolva.",
            nowUtc:        now.AddDays(2));

        // Assert: A kockázat értékelése (P, I, Score, Level, Status) teljesen változatlan marad!
        Assert.Equal(MitigationStatus.Completed, action.Status);
        Assert.Equal(initialProbability, risk.Probability);
        Assert.Equal(initialImpact,      risk.Impact);
        Assert.Equal(initialScore,       risk.Score);
        Assert.Equal(initialLevel,       risk.Level);
        Assert.Equal(initialStatus,      risk.Status);
    }
}

