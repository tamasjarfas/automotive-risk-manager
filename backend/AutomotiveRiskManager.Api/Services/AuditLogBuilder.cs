// Services/AuditLogBuilder.cs
// Központi, biztonságos (allowlist-alapú) auditnapló-építő és szűrő/lapozó szolgáltatás.
//
// Biztonsági garanciák (3. és 8. pont):
//   1. Kifejezetten engedélyezett mezőlista (Allowlist) entitástípusonként.
//   2. Tiltott kulcsszavak (password, hash, token, csrf, cookie, secret, stamp,
//      connection, credential, header) aktív visszautasítása – még véletlenül sem
//      kerülhet érzékeny adat a Changes JSON-ba vagy a Summary mezőbe.
//   3. Hosszú szabad szöveges mezőknél (Description, ResultNotes) nem tároljuk a
//      teljes régi és új szöveget, csak a változás tényét ("módosítva" / "megadva" / "törölve").
//   4. Determinisztikus, memóriában is tesztelhető építő- és lekérdezőmetódusok.

using System.Security.Claims;
using System.Text.Json;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.Services;

/// <summary>
/// Egyetlen mezőváltozás biztonságos, szerializálható reprezentációja.
/// </summary>
public sealed record AuditFieldChange(
    string Field,
    string? OldValue,
    string? NewValue);

/// <summary>
/// A műveletet végrehajtó felhasználó (Actor) biztonságos azonosító adatai.
/// </summary>
public readonly record struct AuditActor(Guid? UserId, string UserName)
{
    public static AuditActor FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new AuditActor(null, "system");
        }

        Guid? userId = null;
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var parsedId))
        {
            userId = parsedId;
        }

        var userName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity?.Name
            ?? (userId.HasValue ? userId.Value.ToString() : "authenticated-user");

        return new AuditActor(userId, Truncate(userName, 150));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

public static class AuditLogBuilder
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize     = 100;
    public const int MaxSummaryLength = 500;
    public const int MaxFieldValueLength = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Entitástípusonként engedélyezett mezőnevek (Allowlist)
    private static readonly IReadOnlyDictionary<AuditEntityType, IReadOnlySet<string>> AllowedFieldsByEntity =
        new Dictionary<AuditEntityType, IReadOnlySet<string>>
        {
            [AuditEntityType.Supplier] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Name",
                "Activity",
                "Status",
                "Description"
            },
            [AuditEntityType.Risk] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SupplierId",
                "SupplierName",
                "Title",
                "Category",
                "Probability",
                "Impact",
                "Score",
                "Level",
                "Status",
                "Description"
            },
            [AuditEntityType.MitigationAction] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "RiskId",
                "RiskTitle",
                "Title",
                "ResponsiblePerson",
                "DueDate",
                "Priority",
                "Status",
                "CompletedAt",
                "Description",
                "ResultNotes"
            },
            [AuditEntityType.User] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "UserName",
                "Role",
                "IsActive"
            }
        };

    // Soha nem naplózható érzékeny mezőnév-részletek (Defense-in-depth)
    private static readonly string[] ForbiddenFieldSubstrings =
    {
        "password",
        "pwd",
        "hash",
        "token",
        "csrf",
        "antiforgery",
        "cookie",
        "session",
        "secret",
        "connectionstring",
        "securitystamp",
        "concurrencystamp",
        "authorization",
        "header",
        "credential",
        "privatekey"
    };

    /// <summary>
    /// Ellenőrzi, hogy egy mezőnév szerepel-e az adott entitástípus allowlistjén,
    /// és nem tartalmaz-e tiltott (érzékeny) kulcsszót.
    /// </summary>
    public static void EnsureFieldAllowed(AuditEntityType entityType, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("A naplózott mezőnév nem lehet üres.", nameof(fieldName));
        }

        string normalized = fieldName.Trim();

        foreach (var forbidden in ForbiddenFieldSubstrings)
        {
            if (normalized.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Biztonsági szabály megsértése: a(z) '{fieldName}' érzékeny mező nem naplózható az AuditLogban.");
            }
        }

        if (!AllowedFieldsByEntity.TryGetValue(entityType, out var allowedSet) ||
            !allowedSet.Contains(normalized))
        {
            throw new InvalidOperationException(
                $"A(z) '{fieldName}' mező nincs engedélyezve (allowlist) a(z) '{entityType}' entitás auditnaplózásához.");
        }
    }

    /// <summary>
    /// Hosszú szabad szöveges mező (pl. Description, ResultNotes) változásának
    /// adatvédelmi/biztonsági összefoglalása a teljes szöveg tárolása nélkül.
    /// </summary>
    public static AuditFieldChange? DescribeFreeTextChange(
        string fieldName,
        string? oldText,
        string? newText)
    {
        string normalizedOld = (oldText ?? string.Empty).Trim();
        string normalizedNew = (newText ?? string.Empty).Trim();

        if (string.Equals(normalizedOld, normalizedNew, StringComparison.Ordinal))
        {
            return null;
        }

        string oldIndicator = string.IsNullOrEmpty(normalizedOld) ? "(üres)" : "(korábbi szöveg)";
        string newIndicator = string.IsNullOrEmpty(normalizedNew) ? "(törölve)" : "(módosítva)";

        return new AuditFieldChange(fieldName, oldIndicator, newIndicator);
    }

    /// <summary>
    /// Alacsony szintű, allowlist-ellenőrzött AuditLog példány építése.
    /// </summary>
    public static AuditLog CreateEntry(
        AuditActor actor,
        AuditAction action,
        AuditEntityType entityType,
        Guid entityId,
        string summary,
        IEnumerable<AuditFieldChange>? changes = null,
        DateTimeOffset? timestampUtc = null)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Az érintett entitás azonosítója (EntityId) nem lehet Guid.Empty.", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Az auditnapló összefoglalója (Summary) nem lehet üres.", nameof(summary));
        }

        var sanitizedChanges = new List<AuditFieldChange>();
        if (changes is not null)
        {
            foreach (var change in changes)
            {
                if (change is null) continue;
                EnsureFieldAllowed(entityType, change.Field);

                sanitizedChanges.Add(new AuditFieldChange(
                    Field:    change.Field.Trim(),
                    OldValue: TruncateValue(change.OldValue),
                    NewValue: TruncateValue(change.NewValue)));
            }
        }

        string actorName = string.IsNullOrWhiteSpace(actor.UserName)
            ? "system"
            : actor.UserName.Trim();
        if (actorName.Length > 150)
        {
            actorName = actorName[..150];
        }

        string cleanSummary = summary.Trim();
        if (cleanSummary.Length > MaxSummaryLength)
        {
            cleanSummary = cleanSummary[..MaxSummaryLength];
        }

        return new AuditLog
        {
            Id            = Guid.NewGuid(),
            TimestampUtc  = (timestampUtc ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            ActorUserId   = actor.UserId,
            ActorUserName = actorName,
            Action        = action,
            EntityType    = entityType,
            EntityId      = entityId,
            Summary       = cleanSummary,
            Changes       = sanitizedChanges.Count > 0
                ? JsonSerializer.Serialize(sanitizedChanges, JsonOptions)
                : null
        };
    }

    // ─── 1. Beszállító (Supplier) auditnapló-építők ───────────────────────────

    public static AuditLog ForSupplierCreated(
        AuditActor actor,
        Supplier supplier,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>
        {
            new("Name",     null, supplier.Name),
            new("Activity", null, supplier.Activity),
            new("Status",   null, supplier.Status.ToString())
        };

        if (!string.IsNullOrWhiteSpace(supplier.Description))
        {
            changes.Add(new AuditFieldChange("Description", "(üres)", "(megadva)"));
        }

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.Created,
            entityType:   AuditEntityType.Supplier,
            entityId:     supplier.Id,
            summary:      $"Új beszállító létrehozva: {supplier.Name} ({supplier.Activity}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForSupplierUpdated(
        AuditActor actor,
        Supplier before,
        Supplier after,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>();

        AddIfChanged(changes, "Name",     before.Name,     after.Name);
        AddIfChanged(changes, "Activity", before.Activity, after.Activity);
        AddIfChanged(changes, "Status",   before.Status.ToString(), after.Status.ToString());

        var descChange = DescribeFreeTextChange("Description", before.Description, after.Description);
        if (descChange is not null) changes.Add(descChange);

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.Updated,
            entityType:   AuditEntityType.Supplier,
            entityId:     after.Id,
            summary:      $"Beszállító adatai módosítva: {after.Name}.",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForSupplierStatusChanged(
        AuditActor actor,
        Supplier supplier,
        SupplierStatus previousStatus,
        SupplierStatus newStatus,
        DateTimeOffset? nowUtc = null)
    {
        var action = newStatus == SupplierStatus.Inactive
            ? AuditAction.Deactivated
            : AuditAction.Activated;

        var changes = new List<AuditFieldChange>
        {
            new("Status", previousStatus.ToString(), newStatus.ToString())
        };

        string actionText = newStatus == SupplierStatus.Inactive ? "inaktiválva" : "aktiválva";

        return CreateEntry(
            actor:        actor,
            action:       action,
            entityType:   AuditEntityType.Supplier,
            entityId:     supplier.Id,
            summary:      $"Beszállító {actionText}: {supplier.Name} ({previousStatus} → {newStatus}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    // ─── 2. Kockázat (Risk) auditnapló-építők ─────────────────────────────────

    public static AuditLog ForRiskCreated(
        AuditActor actor,
        Risk risk,
        string supplierName,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>
        {
            new("Title",        null, risk.Title),
            new("SupplierName", null, supplierName),
            new("Category",     null, risk.Category),
            new("Probability",  null, risk.Probability.ToString()),
            new("Impact",       null, risk.Impact.ToString()),
            new("Score",        null, risk.Score.ToString()),
            new("Level",        null, risk.Level.ToString()),
            new("Status",       null, risk.Status.ToString())
        };

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.Created,
            entityType:   AuditEntityType.Risk,
            entityId:     risk.Id,
            summary:      $"Új kockázat rögzítve: {risk.Title} ({supplierName}) – Pontszám: {risk.Score} ({risk.Level}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForRiskUpdated(
        AuditActor actor,
        Risk before,
        Risk after,
        string supplierName,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>();

        AddIfChanged(changes, "SupplierId",  before.SupplierId.ToString(),  after.SupplierId.ToString());
        AddIfChanged(changes, "Title",       before.Title,                  after.Title);
        AddIfChanged(changes, "Category",    before.Category,               after.Category);
        AddIfChanged(changes, "Probability", before.Probability.ToString(), after.Probability.ToString());
        AddIfChanged(changes, "Impact",      before.Impact.ToString(),      after.Impact.ToString());
        AddIfChanged(changes, "Score",       before.Score.ToString(),       after.Score.ToString());
        AddIfChanged(changes, "Level",       before.Level.ToString(),       after.Level.ToString());
        AddIfChanged(changes, "Status",      before.Status.ToString(),      after.Status.ToString());

        var descChange = DescribeFreeTextChange("Description", before.Description, after.Description);
        if (descChange is not null) changes.Add(descChange);

        string evalNote = before.Score != after.Score || before.Level != after.Level
            ? $" (Újraértékelve: {before.Score}/{before.Level} → {after.Score}/{after.Level})"
            : $" (Pontszám: {after.Score}/{after.Level})";

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.Updated,
            entityType:   AuditEntityType.Risk,
            entityId:     after.Id,
            summary:      $"Kockázat módosítva: {after.Title} ({supplierName}){evalNote}.",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForRiskStatusChanged(
        AuditActor actor,
        Risk risk,
        RiskStatus previousStatus,
        RiskStatus newStatus,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>
        {
            new("Status", previousStatus.ToString(), newStatus.ToString())
        };

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.StatusChanged,
            entityType:   AuditEntityType.Risk,
            entityId:     risk.Id,
            summary:      $"Kockázat státusza megváltozott: {risk.Title} ({previousStatus} → {newStatus}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    // ─── 3. Kockázatcsökkentő intézkedés (MitigationAction) auditnapló-építők ─

    public static AuditLog ForMitigationCreated(
        AuditActor actor,
        MitigationAction mitigation,
        string riskTitle,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>
        {
            new("Title",             null, mitigation.Title),
            new("RiskTitle",         null, riskTitle),
            new("ResponsiblePerson", null, mitigation.ResponsiblePerson),
            new("DueDate",           null, FormatDate(mitigation.DueDate)),
            new("Priority",          null, mitigation.Priority.ToString()),
            new("Status",            null, mitigation.Status.ToString())
        };

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.Created,
            entityType:   AuditEntityType.MitigationAction,
            entityId:     mitigation.Id,
            summary:      $"Új intézkedés rögzítve: {mitigation.Title} (Felelős: {mitigation.ResponsiblePerson}, Státusz: {mitigation.Status}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForMitigationUpdated(
        AuditActor actor,
        MitigationAction before,
        MitigationAction after,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>();

        AddIfChanged(changes, "RiskId",            before.RiskId.ToString(),      after.RiskId.ToString());
        AddIfChanged(changes, "Title",             before.Title,                  after.Title);
        AddIfChanged(changes, "ResponsiblePerson", before.ResponsiblePerson,      after.ResponsiblePerson);
        AddIfChanged(changes, "DueDate",           FormatDate(before.DueDate),    FormatDate(after.DueDate));
        AddIfChanged(changes, "Priority",          before.Priority.ToString(),    after.Priority.ToString());
        AddIfChanged(changes, "Status",            before.Status.ToString(),      after.Status.ToString());
        AddIfChanged(changes, "CompletedAt",       FormatDate(before.CompletedAt), FormatDate(after.CompletedAt));

        var descChange = DescribeFreeTextChange("Description", before.Description, after.Description);
        if (descChange is not null) changes.Add(descChange);

        var resultChange = DescribeFreeTextChange("ResultNotes", before.ResultNotes, after.ResultNotes);
        if (resultChange is not null) changes.Add(resultChange);

        var auditAction = DetermineMitigationAuditAction(before.Status, after.Status, isFullUpdate: true);

        return CreateEntry(
            actor:        actor,
            action:       auditAction,
            entityType:   AuditEntityType.MitigationAction,
            entityId:     after.Id,
            summary:      $"Intézkedés módosítva: {after.Title} (Státusz: {after.Status}, Prioritás: {after.Priority}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForMitigationStatusChanged(
        AuditActor actor,
        MitigationAction mitigation,
        MitigationStatus previousStatus,
        DateTimeOffset? previousCompletedAt,
        string? previousResultNotes,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>
        {
            new("Status", previousStatus.ToString(), mitigation.Status.ToString())
        };

        AddIfChanged(
            changes,
            "CompletedAt",
            FormatDate(previousCompletedAt),
            FormatDate(mitigation.CompletedAt));

        var resultChange = DescribeFreeTextChange("ResultNotes", previousResultNotes, mitigation.ResultNotes);
        if (resultChange is not null) changes.Add(resultChange);

        var auditAction = DetermineMitigationAuditAction(previousStatus, mitigation.Status, isFullUpdate: false);

        return CreateEntry(
            actor:        actor,
            action:       auditAction,
            entityType:   AuditEntityType.MitigationAction,
            entityId:     mitigation.Id,
            summary:      $"Intézkedés státusza megváltozott: {mitigation.Title} ({previousStatus} → {mitigation.Status}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    private static AuditAction DetermineMitigationAuditAction(
        MitigationStatus before,
        MitigationStatus after,
        bool isFullUpdate)
    {
        if (before != after)
        {
            if (after == MitigationStatus.Completed)
            {
                return AuditAction.Completed;
            }
            if (before == MitigationStatus.Completed &&
                after is MitigationStatus.Planned or MitigationStatus.InProgress)
            {
                return AuditAction.Reopened;
            }
            if (!isFullUpdate)
            {
                return AuditAction.StatusChanged;
            }
        }

        return isFullUpdate ? AuditAction.Updated : AuditAction.StatusChanged;
    }

    // ─── 4. Felhasználókezelés (User) auditnapló-építők (kizárólag metaadat!) ──

    public static AuditLog ForUserCreated(
        AuditActor actor,
        Guid createdUserId,
        string createdUserName,
        string assignedRole,
        bool isActive = true,
        DateTimeOffset? nowUtc = null)
    {
        // SZIGORÚ BIZTONSÁGI SZABÁLY: jelszó, jelszóhash, e-mail vagy token SOHA nem kerül be!
        var changes = new List<AuditFieldChange>
        {
            new("UserName", null, createdUserName),
            new("Role",     null, assignedRole),
            new("IsActive", null, isActive ? "true" : "false")
        };

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.Created,
            entityType:   AuditEntityType.User,
            entityId:     createdUserId,
            summary:      $"Új felhasználói fiók létrehozva: {createdUserName} (Szerepkör: {assignedRole}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForUserRoleChanged(
        AuditActor actor,
        Guid targetUserId,
        string targetUserName,
        string previousRole,
        string newRole,
        DateTimeOffset? nowUtc = null)
    {
        var changes = new List<AuditFieldChange>
        {
            new("Role", previousRole, newRole)
        };

        return CreateEntry(
            actor:        actor,
            action:       AuditAction.RoleChanged,
            entityType:   AuditEntityType.User,
            entityId:     targetUserId,
            summary:      $"Felhasználó szerepköre módosítva: {targetUserName} ({previousRole} → {newRole}).",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    public static AuditLog ForUserStatusChanged(
        AuditActor actor,
        Guid targetUserId,
        string targetUserName,
        bool previousIsActive,
        bool newIsActive,
        DateTimeOffset? nowUtc = null)
    {
        var action = newIsActive ? AuditAction.Activated : AuditAction.Deactivated;
        string stateText = newIsActive ? "aktiválva" : "inaktiválva";

        var changes = new List<AuditFieldChange>
        {
            new("IsActive", previousIsActive ? "true" : "false", newIsActive ? "true" : "false")
        };

        return CreateEntry(
            actor:        actor,
            action:       action,
            entityType:   AuditEntityType.User,
            entityId:     targetUserId,
            summary:      $"Felhasználói fiók {stateText}: {targetUserName}.",
            changes:      changes,
            timestampUtc: nowUtc);
    }

    // ─── Segédfüggvények ──────────────────────────────────────────────────────

    private static void AddIfChanged(
        List<AuditFieldChange> list,
        string field,
        string? oldValue,
        string? newValue)
    {
        if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
        {
            list.Add(new AuditFieldChange(field, oldValue, newValue));
        }
    }

    private static string? FormatDate(DateTimeOffset? dt) =>
        dt.HasValue ? dt.Value.ToUniversalTime().ToString("yyyy-MM-dd") : null;

    private static string? TruncateValue(string? value)
    {
        if (value is null) return null;
        string trimmed = value.Trim();
        return trimmed.Length <= MaxFieldValueLength
            ? trimmed
            : trimmed[..MaxFieldValueLength] + "…";
    }

    // ─── Szűrési, rendezési és lapozási segédmetódusok ────────────────────────

    /// <summary>
    /// Értelmezi és ellenőrzi az AuditEntityType szűrőparamétert.
    /// </summary>
    public static AuditEntityType ParseEntityType(string? entityTypeText)
    {
        if (string.IsNullOrWhiteSpace(entityTypeText) ||
            !Enum.TryParse<AuditEntityType>(entityTypeText.Trim(), ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new ArgumentException(
                $"Érvénytelen entitástípus szűrő: '{entityTypeText}'. Elfogadott értékek: Supplier, Risk, MitigationAction, User.",
                nameof(entityTypeText));
        }

        return parsed;
    }

    /// <summary>
    /// Értelmezi és ellenőrzi az AuditAction szűrőparamétert.
    /// </summary>
    public static AuditAction ParseAction(string? actionText)
    {
        if (string.IsNullOrWhiteSpace(actionText) ||
            !Enum.TryParse<AuditAction>(actionText.Trim(), ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new ArgumentException(
                $"Érvénytelen eseménytípus szűrő: '{actionText}'. Elfogadott értékek: Created, Updated, StatusChanged, Deactivated, Activated, RoleChanged, Completed, Reopened.",
                nameof(actionText));
        }

        return parsed;
    }

    /// <summary>
    /// Normalizálja a lapozási paramétereket (page &gt;= 1, 1 &lt;= pageSize &lt;= MaxPageSize).
    /// </summary>
    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        int safePage = page < 1 ? 1 : page;
        int safePageSize = pageSize < 1
            ? DefaultPageSize
            : Math.Min(pageSize, MaxPageSize);

        return (safePage, safePageSize);
    }

    /// <summary>
    /// Alkalmazza a szűrőket és a csökkenő időrendi rendezést (legújabb események elöl)
    /// egy IQueryable&lt;AuditLog&gt; lekérdezésre (EF Core és memóriabeli tesztekhez egyaránt).
    /// </summary>
    public static IQueryable<AuditLog> ApplyFiltersAndOrdering(
        IQueryable<AuditLog> query,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string? entityType = null,
        string? action = null,
        string? actor = null,
        Guid? entityId = null)
    {
        if (fromUtc.HasValue)
        {
            var start = fromUtc.Value.ToUniversalTime();
            query = query.Where(a => a.TimestampUtc >= start);
        }

        if (toUtc.HasValue)
        {
            var end = toUtc.Value.ToUniversalTime();
            query = query.Where(a => a.TimestampUtc <= end);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var parsedEntityType = ParseEntityType(entityType);
            query = query.Where(a => a.EntityType == parsedEntityType);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var parsedAction = ParseAction(action);
            query = query.Where(a => a.Action == parsedAction);
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            string search = actor.Trim().ToLower();
            if (Guid.TryParse(search, out var actorGuid))
            {
                query = query.Where(a =>
                    a.ActorUserId == actorGuid ||
                    a.ActorUserName.ToLower().Contains(search));
            }
            else
            {
                query = query.Where(a => a.ActorUserName.ToLower().Contains(search));
            }
        }

        if (entityId.HasValue && entityId.Value != Guid.Empty)
        {
            var targetId = entityId.Value;
            query = query.Where(a => a.EntityId == targetId);
        }

        return query
            .OrderByDescending(a => a.TimestampUtc)
            .ThenByDescending(a => a.Id);
    }
}
