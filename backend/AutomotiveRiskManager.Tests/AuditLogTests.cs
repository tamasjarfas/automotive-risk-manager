// AuditLogTests.cs
// Egységtesztek a 7. fejlesztési szakasz (Auditnaplózás – AuditLog modul)
// üzleti, biztonsági (allowlist) és lekérdezési logikájának ellenőrzésére.

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutomotiveRiskManager.Api.Controllers;
using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;
using AutomotiveRiskManager.Api.Services;
using Xunit;

namespace AutomotiveRiskManager.Tests;

public class AuditLogTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 11, 14, 30, 0, TimeSpan.Zero);

    private static readonly AuditActor TestActor =
        new(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "admin@arm.local");

    // ─── 1. Sikeres létrehozás auditbejegyzést hoz létre ──────────────────────

    [Fact]
    public void ForSupplierCreated_ValidSupplier_CreatesAuditEntryWithSafeFields()
    {
        var supplier = new Supplier
        {
            Id          = new Guid("11111111-1111-1111-1111-111111111111"),
            Name        = "Autóipari Szenzor Kft.",
            Activity    = "Szenzorgyártás",
            Description = "Részletes bizalmas beszállítói leírás",
            Status      = SupplierStatus.Active,
            CreatedAt   = FixedNow
        };

        var log = AuditLogBuilder.ForSupplierCreated(TestActor, supplier, FixedNow);
        var dto = AuditLogDto.FromEntity(log);

        Assert.Equal(AuditAction.Created, log.Action);
        Assert.Equal(AuditEntityType.Supplier, log.EntityType);
        Assert.Equal(supplier.Id, log.EntityId);
        Assert.Equal(TestActor.UserId, log.ActorUserId);
        Assert.Equal("admin@arm.local", log.ActorUserName);
        Assert.Equal(FixedNow, log.TimestampUtc);
        Assert.Contains("Autóipari Szenzor Kft.", log.Summary);

        // A szabad szöveges Description mező tartalma nem szivároghat ki, csak a kitöltés ténye
        Assert.DoesNotContain("Részletes bizalmas beszállítói leírás", log.Changes ?? string.Empty);
        Assert.Contains(dto.ChangeItems, c => c.Field == "Name" && c.NewValue == "Autóipari Szenzor Kft.");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Status" && c.NewValue == "Active");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Description" && c.NewValue == "(megadva)");
    }

    [Fact]
    public void ForRiskCreated_ValidRisk_CreatesAuditEntryWithEvaluationData()
    {
        var risk = new Risk
        {
            Id          = new Guid("22222222-2222-2222-2222-222222222222"),
            SupplierId  = new Guid("11111111-1111-1111-1111-111111111111"),
            Title       = "VPN MFA hiánya",
            Description = "Távoli hozzáférés kétfaktoros hitelesítés nélkül.",
            Category    = "Hozzáférés-kezelés",
            Probability = 4,
            Impact      = 5,
            Score       = 20,
            Level       = RiskLevel.Critical,
            Status      = RiskStatus.Open,
            CreatedAt   = FixedNow
        };

        var log = AuditLogBuilder.ForRiskCreated(TestActor, risk, "Autóipari Szenzor Kft.", FixedNow);
        var dto = AuditLogDto.FromEntity(log);

        Assert.Equal(AuditAction.Created, log.Action);
        Assert.Equal(AuditEntityType.Risk, log.EntityType);
        Assert.Equal(risk.Id, log.EntityId);
        Assert.Contains("VPN MFA hiánya", log.Summary);
        Assert.Contains("20", log.Summary);
        Assert.Contains("Critical", log.Summary);

        Assert.Contains(dto.ChangeItems, c => c.Field == "Probability" && c.NewValue == "4");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Impact" && c.NewValue == "5");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Score" && c.NewValue == "20");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Level" && c.NewValue == "Critical");
        Assert.DoesNotContain("Távoli hozzáférés kétfaktoros hitelesítés nélkül.", log.Changes ?? string.Empty);
    }

    // ─── 2. Sikertelen validáció nem hoz létre auditbejegyzést ────────────────

    [Fact]
    public void FailedValidation_ThrowsBeforeAuditEntryCanBeCreated()
    {
        var auditTrail = new List<AuditLog>();

        // Szimuláljuk a vezérlő működését: érvénytelen Probability (6) esetén a RiskCalculator
        // kivételt dob, így az AuditLogBuilder sosem hívódik meg.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var evaluation = RiskCalculator.Evaluate(probability: 6, impact: 4);
            var risk = new Risk
            {
                Id = Guid.NewGuid(),
                SupplierId = Guid.NewGuid(),
                Title = "Teszt",
                Category = "Hálózatbiztonság",
                Probability = 6,
                Impact = 4,
                Score = evaluation.Score,
                Level = evaluation.Level
            };
            auditTrail.Add(AuditLogBuilder.ForRiskCreated(TestActor, risk, "Teszt Kft.", FixedNow));
        });

        Assert.Empty(auditTrail);
    }

    // ─── 3. Kockázatértékelés módosítása naplózza a régi és új értékeket ──────

    [Fact]
    public void ForRiskUpdated_ScoreAndLevelChanged_LogsOldAndNewEvaluationValues()
    {
        var riskId = new Guid("33333333-3333-3333-3333-333333333333");
        var supplierId = new Guid("11111111-1111-1111-1111-111111111111");

        var before = new Risk
        {
            Id          = riskId,
            SupplierId  = supplierId,
            Title       = "Tűzfal szabályok elavultak",
            Description = "Régi leírás",
            Category    = "Hálózatbiztonság",
            Probability = 2,
            Impact      = 3,
            Score       = 6,
            Level       = RiskLevel.Medium,
            Status      = RiskStatus.Open
        };

        var after = new Risk
        {
            Id          = riskId,
            SupplierId  = supplierId,
            Title       = "Tűzfal szabályok elavultak",
            Description = "Új részletes leírás",
            Category    = "Hálózatbiztonság",
            Probability = 4,
            Impact      = 5,
            Score       = 20,
            Level       = RiskLevel.Critical,
            Status      = RiskStatus.UnderReview
        };

        var log = AuditLogBuilder.ForRiskUpdated(TestActor, before, after, "Autóipari Szenzor Kft.", FixedNow);
        var dto = AuditLogDto.FromEntity(log);

        Assert.Equal(AuditAction.Updated, log.Action);
        Assert.Equal(AuditEntityType.Risk, log.EntityType);

        Assert.Contains(dto.ChangeItems, c => c.Field == "Probability" && c.OldValue == "2" && c.NewValue == "4");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Impact" && c.OldValue == "3" && c.NewValue == "5");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Score" && c.OldValue == "6" && c.NewValue == "20");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Level" && c.OldValue == "Medium" && c.NewValue == "Critical");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Status" && c.OldValue == "Open" && c.NewValue == "UnderReview");
        Assert.Contains(dto.ChangeItems, c => c.Field == "Description" && c.OldValue == "(megadva)" && c.NewValue == "(módosítva)");
        Assert.DoesNotContain("Új részletes leírás", log.Changes ?? string.Empty);
    }

    // ─── 4. Intézkedés státuszváltása naplózásra kerül ────────────────────────

    [Fact]
    public void ForMitigationStatusChanged_CompletionAndReopen_LogsAccurateActionAndCompletedAt()
    {
        var completedAt = new DateTimeOffset(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);
        var action = new MitigationAction
        {
            Id                = new Guid("44444444-4444-4444-4444-444444444444"),
            RiskId            = new Guid("22222222-2222-2222-2222-222222222222"),
            Title             = "MFA bevezetése VPN-en",
            ResponsiblePerson = "Kiss János",
            Priority          = MitigationPriority.Critical,
            Status            = MitigationStatus.Completed,
            CompletedAt       = completedAt,
            ResultNotes       = "Sikeresen bevezetve minden beszállítói fiókon."
        };

        var completedLog = AuditLogBuilder.ForMitigationStatusChanged(
            TestActor,
            action,
            previousStatus: MitigationStatus.InProgress,
            previousCompletedAt: null,
            previousResultNotes: null,
            timestampUtc: FixedNow);

        var completedDto = AuditLogDto.FromEntity(completedLog);

        Assert.Equal(AuditAction.Completed, completedLog.Action);
        Assert.Equal(AuditEntityType.MitigationAction, completedLog.EntityType);
        Assert.Contains(completedDto.ChangeItems, c =>
            c.Field == "Status" && c.OldValue == "InProgress" && c.NewValue == "Completed");
        Assert.Contains(completedDto.ChangeItems, c =>
            c.Field == "CompletedAt" && c.OldValue == null && c.NewValue == completedAt.ToString("O"));
        Assert.Contains(completedDto.ChangeItems, c =>
            c.Field == "ResultNotes" && c.OldValue == "(üres)" && c.NewValue == "(megadva)");
        Assert.DoesNotContain("Sikeresen bevezetve minden beszállítói fiókon.", completedLog.Changes ?? string.Empty);

        // Visszanyitás (Completed -> InProgress)
        action.Status = MitigationStatus.InProgress;
        action.CompletedAt = null;

        var reopenedLog = AuditLogBuilder.ForMitigationStatusChanged(
            TestActor,
            action,
            previousStatus: MitigationStatus.Completed,
            previousCompletedAt: completedAt,
            previousResultNotes: action.ResultNotes,
            timestampUtc: FixedNow);

        Assert.Equal(AuditAction.Reopened, reopenedLog.Action);
    }

    // ─── 5. Felhasználói szerepkör- és állapotváltozás jelszó nélkül naplózódik

    [Fact]
    public void ForUserOperations_LogsMetadataOnlyWithoutSensitiveData()
    {
        var user = new ApplicationUser
        {
            Id           = new Guid("55555555-5555-5555-5555-555555555555"),
            UserName     = "risk.elemzo",
            Email        = "elemzo@arm.local",
            DisplayName  = "Kockázatelemző Teszt",
            PasswordHash = "AQAAAAIAAYagAAAAE_TITKOS_JELSZO_HASH_123456",
            SecurityStamp = "TITKOS_SECURITY_STAMP_987654",
            IsActive     = true
        };

        var createLog = AuditLogBuilder.ForUserCreated(TestActor, user, AppRoles.RiskManager, FixedNow);
        var roleLog = AuditLogBuilder.ForUserRoleChanged(TestActor, user, AppRoles.Viewer, AppRoles.RiskManager, FixedNow);
        var deactivateLog = AuditLogBuilder.ForUserStatusChanged(TestActor, user, true, false, FixedNow);

        Assert.Equal(AuditAction.Created, createLog.Action);
        Assert.Equal(AuditAction.RoleChanged, roleLog.Action);
        Assert.Equal(AuditAction.Deactivated, deactivateLog.Action);

        foreach (var log in new[] { createLog, roleLog, deactivateLog })
        {
            Assert.DoesNotContain("AQAAAAIAAYagAAAAE_TITKOS_JELSZO_HASH_123456", log.Changes ?? string.Empty);
            Assert.DoesNotContain("TITKOS_SECURITY_STAMP_987654", log.Changes ?? string.Empty);
            Assert.DoesNotContain("Password", log.Changes ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Token", log.Changes ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        var roleDto = AuditLogDto.FromEntity(roleLog);
        Assert.Contains(roleDto.ChangeItems, c =>
            c.Field == "Role" && c.OldValue == AppRoles.Viewer && c.NewValue == AppRoles.RiskManager);
    }

    // ─── 6. Tiltott érzékeny mezők visszautasítása (Allowlist + Forbidden guard)

    [Theory]
    [InlineData(AuditEntityType.User, "Password")]
    [InlineData(AuditEntityType.User, "PasswordHash")]
    [InlineData(AuditEntityType.User, "SecurityStamp")]
    [InlineData(AuditEntityType.User, "ConcurrencyStamp")]
    [InlineData(AuditEntityType.User, "CsrfToken")]
    [InlineData(AuditEntityType.User, "Cookie")]
    [InlineData(AuditEntityType.User, "SessionId")]
    [InlineData(AuditEntityType.Supplier, "ConnectionString")]
    [InlineData(AuditEntityType.Risk, "Authorization")]
    [InlineData(AuditEntityType.MitigationAction, "SecretKey")]
    [InlineData(AuditEntityType.Supplier, "NemEngedelyezettMezo")]
    public void EnsureFieldAllowed_ForbiddenOrUnlistedField_ThrowsInvalidOperationException(
        AuditEntityType entityType,
        string forbiddenField)
    {
        Assert.Throws<InvalidOperationException>(() =>
            AuditLogBuilder.EnsureFieldAllowed(entityType, forbiddenField));

        Assert.Throws<InvalidOperationException>(() =>
            AuditLogBuilder.CreateEntry(
                TestActor,
                AuditAction.Updated,
                entityType,
                Guid.NewGuid(),
                "Teszt összefoglaló",
                new[] { new AuditFieldChange(forbiddenField, "régi", "új") },
                FixedNow));
    }

    // ─── 7. AuditLogsController csak olvasható és csak Administrator érheti el

    [Fact]
    public void AuditLogsController_IsReadOnlyAndRestrictedToAdministratorPolicy()
    {
        var controllerType = typeof(AuditLogsController);

        // Szabályzat ellenőrzése az osztályon: kizárólag ViewAuditLogs (Administrator)
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AppPolicies.ViewAuditLogs, authorizeAttr!.Policy);

        // Nem létezhet POST, PUT, PATCH vagy DELETE végpont az AuditLogsController-ben
        var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            Assert.Null(method.GetCustomAttribute<HttpPostAttribute>());
            Assert.Null(method.GetCustomAttribute<HttpPutAttribute>());
            Assert.Null(method.GetCustomAttribute<HttpPatchAttribute>());
            Assert.Null(method.GetCustomAttribute<HttpDeleteAttribute>());
        }

        // Pontosan egy GET végpont létezik
        Assert.Single(methods, m => m.GetCustomAttribute<HttpGetAttribute>() is not null);
    }

    // ─── 8. Szűrés, lapozás és időrendi (legfrissebb elöl) rendezés ───────────

    [Fact]
    public void ApplyFiltersAndOrdering_FiltersAndSortsNewestFirst()
    {
        var targetEntityId = new Guid("99999999-9999-9999-9999-999999999999");

        var logs = new List<AuditLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TimestampUtc = FixedNow.AddHours(-3),
                ActorUserName = "admin@arm.local",
                Action = AuditAction.Created,
                EntityType = AuditEntityType.Supplier,
                EntityId = Guid.NewGuid(),
                Summary = "Korábbi beszállító létrehozás"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TimestampUtc = FixedNow.AddHours(-2),
                ActorUserName = "risk.manager",
                Action = AuditAction.Created,
                EntityType = AuditEntityType.Risk,
                EntityId = targetEntityId,
                Summary = "Kockázat rögzítése"
            },
            new()
            {
                Id = Guid.NewGuid(),
                TimestampUtc = FixedNow.AddHours(-1),
                ActorUserName = "risk.manager",
                Action = AuditAction.Updated,
                EntityType = AuditEntityType.Risk,
                EntityId = targetEntityId,
                Summary = "Kockázat újraértékelése"
            }
        }.AsQueryable();

        // Időrendi rendezés: legfrissebb elöl
        var orderedAll = AuditLogBuilder.ApplyFiltersAndOrdering(logs).ToList();
        Assert.Equal(3, orderedAll.Count);
        Assert.Equal("Kockázat újraértékelése", orderedAll[0].Summary);
        Assert.Equal("Kockázat rögzítése", orderedAll[1].Summary);
        Assert.Equal("Korábbi beszállító létrehozás", orderedAll[2].Summary);

        // Szűrés entitástípusra, végrehajtóra és entitásazonosítóra
        var filtered = AuditLogBuilder.ApplyFiltersAndOrdering(
            logs,
            fromUtc: FixedNow.AddHours(-2).AddMinutes(-5),
            toUtc: FixedNow,
            entityType: AuditEntityType.Risk,
            action: AuditAction.Updated,
            actor: "risk",
            entityId: targetEntityId).ToList();

        Assert.Single(filtered);
        Assert.Equal("Kockázat újraértékelése", filtered[0].Summary);

        // Lapozási korlátok ellenőrzése (1..100)
        var (page, pageSize) = AuditLogBuilder.NormalizePaging(-5, 500);
        Assert.Equal(1, page);
        Assert.Equal(AuditLogBuilder.MaxPageSize, pageSize);
    }

    [Fact]
    public void AuditActor_FromPrincipal_ExtractsUserIdAndUserNameOrSystemFallback()
    {
        var userId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "auditor.admin")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var actor = AuditActor.FromPrincipal(principal);
        Assert.Equal(userId, actor.UserId);
        Assert.Equal("auditor.admin", actor.UserName);

        var anonymousActor = AuditActor.FromPrincipal(null);
        Assert.Null(anonymousActor.UserId);
        Assert.Equal("system", anonymousActor.UserName);
    }
}

