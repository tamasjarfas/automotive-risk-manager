// Services/MitigationPolicy.cs
// Központi, jól tesztelhető üzletiszabály-réteg a kockázatcsökkentő intézkedésekhez.
//
// Szakdolgozati jelentősége:
//   1. Státusz- és prioritásvalidáció: csak a megengedett enum-értékek fogadhatók el.
//   2. Kötelező mezők ellenőrzése: létező Risk, nem üres Title és ResponsiblePerson.
//   3. Határidő-lejárat (IsOverdue) számítása: a határidő lejárta egy külön számított
//      jelző (true/false), amely SOHA nem írja felül a munkafolyamat státuszát
//      (Planned / InProgress). Befejezett (Completed), visszavont (Cancelled) vagy
//      határidő nélküli (DueDate == null) intézkedés nem minősül lejártnak.
//   4. CompletedAt életciklus: Completed állapotba lépéskor rögzíti a teljesítés
//      időpontját; visszanyitáskor (Planned / InProgress / Cancelled) nullázza azt.
//   5. Kockázat-függetlenség: az intézkedés létrehozása, módosítása vagy befejezése
//      NEM változtatja meg automatikusan a kapcsolódó Risk valószínűségét, hatását,
//      pontszámát vagy kockázati szintjét.

using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.Services;

public static class MitigationPolicy
{
    public const int MaxTitleLength       = 200;
    public const int MaxResponsibleLength = 150;
    public const int MaxTextLength        = 2000;

    /// <summary>
    /// Értelmezi és ellenőrzi a szöveges státuszértéket (Planned, InProgress, Completed, Cancelled).
    /// Érvénytelen érték esetén ArgumentException kivételt dob.
    /// </summary>
    public static MitigationStatus ParseStatus(string? statusText)
    {
        if (string.IsNullOrWhiteSpace(statusText) ||
            !Enum.TryParse<MitigationStatus>(statusText.Trim(), ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new ArgumentException(
                $"Érvénytelen intézkedési státusz: '{statusText}'. Elfogadott értékek: Planned, InProgress, Completed, Cancelled.",
                nameof(statusText));
        }

        return parsed;
    }

    /// <summary>
    /// Értelmezi és ellenőrzi a szöveges prioritásértéket (Low, Medium, High, Critical).
    /// Érvénytelen érték esetén ArgumentException kivételt dob.
    /// </summary>
    public static MitigationPriority ParsePriority(string? priorityText)
    {
        if (string.IsNullOrWhiteSpace(priorityText) ||
            !Enum.TryParse<MitigationPriority>(priorityText.Trim(), ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new ArgumentException(
                $"Érvénytelen prioritásérték: '{priorityText}'. Elfogadott értékek: Low, Medium, High, Critical.",
                nameof(priorityText));
        }

        return parsed;
    }

    /// <summary>
    /// Meghatározza, hogy egy intézkedés határideje lejárt-e (IsOverdue).
    /// Szabályok:
    ///   - Ha nincs határidő (DueDate == null) → false.
    ///   - Ha az intézkedés már Completed vagy Cancelled → false.
    ///   - Ha Planned vagy InProgress, és a határidő UTC naptári napja megelőzi az aktuális UTC napot → true.
    /// Fontos: ez a számítás nem módosítja az intézkedés Status mezőjét!
    /// </summary>
    public static bool IsOverdue(
        DateTimeOffset? dueDate,
        MitigationStatus status,
        DateTimeOffset referenceUtcNow)
    {
        if (!dueDate.HasValue)
        {
            return false;
        }

        if (status is MitigationStatus.Completed or MitigationStatus.Cancelled)
        {
            return false;
        }

        return dueDate.Value.UtcDateTime.Date < referenceUtcNow.UtcDateTime.Date;
    }

    /// <summary>
    /// Ellenőrzi a kötelező szöveges mezőket (Title, ResponsiblePerson) és a hosszkorlátokat.
    /// </summary>
    public static void ValidateCoreFields(
        string? title,
        string? responsiblePerson,
        string? description = null,
        string? resultNotes = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Az intézkedés megnevezése nem lehet üres.", nameof(title));
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            throw new ArgumentException(
                $"Az intézkedés megnevezése legfeljebb {MaxTitleLength} karakter lehet.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(responsiblePerson))
        {
            throw new ArgumentException("A felelős megadása kötelező.", nameof(responsiblePerson));
        }

        if (responsiblePerson.Trim().Length > MaxResponsibleLength)
        {
            throw new ArgumentException(
                $"A felelős megnevezése legfeljebb {MaxResponsibleLength} karakter lehet.",
                nameof(responsiblePerson));
        }

        if (description is not null && description.Trim().Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"A részletes leírás legfeljebb {MaxTextLength} karakter lehet.",
                nameof(description));
        }

        if (resultNotes is not null && resultNotes.Trim().Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"Az eredmény / megjegyzés legfeljebb {MaxTextLength} karakter lehet.",
                nameof(resultNotes));
        }
    }

    /// <summary>
    /// Létrehoz egy új, validált MitigationAction példányt egy létező Risk rekordhoz.
    /// Nem módosítja a kapcsolódó Risk egyetlen mezőjét sem!
    /// </summary>
    public static MitigationAction CreateAction(
        Risk? targetRisk,
        string? title,
        string? description,
        string? responsiblePerson,
        DateTimeOffset? dueDate,
        string? priorityText,
        string? statusText,
        string? resultNotes,
        DateTimeOffset nowUtc)
    {
        if (targetRisk is null || targetRisk.Id == Guid.Empty)
        {
            throw new ArgumentException("Új intézkedés csak létező kockázathoz rögzíthető.", nameof(targetRisk));
        }

        ValidateCoreFields(title, responsiblePerson, description, resultNotes);

        var priority = string.IsNullOrWhiteSpace(priorityText)
            ? MitigationPriority.Medium
            : ParsePriority(priorityText);

        var status = string.IsNullOrWhiteSpace(statusText)
            ? MitigationStatus.Planned
            : ParseStatus(statusText);

        var utcNow = nowUtc.ToUniversalTime();

        return new MitigationAction
        {
            Id                = Guid.NewGuid(),
            RiskId            = targetRisk.Id,
            Title             = title!.Trim(),
            Description       = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ResponsiblePerson = responsiblePerson!.Trim(),
            DueDate           = dueDate?.ToUniversalTime(),
            Priority          = priority,
            Status            = status,
            CompletedAt       = status == MitigationStatus.Completed ? utcNow : null,
            ResultNotes       = string.IsNullOrWhiteSpace(resultNotes) ? null : resultNotes.Trim(),
            CreatedAt         = utcNow,
            UpdatedAt         = null
        };
    }

    /// <summary>
    /// Frissíti egy meglévő intézkedés adatait és következetesen kezeli a CompletedAt időbélyeget.
    /// Nem módosítja a kapcsolódó Risk pontszámát vagy kockázati szintjét!
    /// </summary>
    public static void UpdateAction(
        MitigationAction action,
        Risk? targetRisk,
        string? title,
        string? description,
        string? responsiblePerson,
        DateTimeOffset? dueDate,
        string? priorityText,
        string? statusText,
        string? resultNotes,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (targetRisk is null || targetRisk.Id == Guid.Empty)
        {
            throw new ArgumentException("Az intézkedés csak létező kockázathoz rendelhető.", nameof(targetRisk));
        }

        ValidateCoreFields(title, responsiblePerson, description, resultNotes);

        var newPriority = ParsePriority(priorityText);
        var newStatus   = string.IsNullOrWhiteSpace(statusText)
            ? action.Status
            : ParseStatus(statusText);

        var utcNow = nowUtc.ToUniversalTime();

        action.RiskId            = targetRisk.Id;
        action.Title             = title!.Trim();
        action.Description       = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        action.ResponsiblePerson = responsiblePerson!.Trim();
        action.DueDate           = dueDate?.ToUniversalTime();
        action.Priority          = newPriority;
        action.ResultNotes       = string.IsNullOrWhiteSpace(resultNotes) ? null : resultNotes.Trim();

        ApplyStatusTransitionInternal(action, newStatus, utcNow);
        action.UpdatedAt = utcNow;
    }

    /// <summary>
    /// Állapotváltás végrehajtása (pl. PATCH /api/mitigations/{id}/status).
    /// Befejezéskor (Completed) rögzíti a CompletedAt időpontot; visszanyitáskor nullázza.
    /// Opcionálisan frissíti az eredményt / megjegyzést (ResultNotes).
    /// </summary>
    public static void ApplyStatusTransition(
        MitigationAction action,
        string? newStatusText,
        string? resultNotes,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(action);

        var newStatus = ParseStatus(newStatusText);

        if (resultNotes is not null)
        {
            if (resultNotes.Trim().Length > MaxTextLength)
            {
                throw new ArgumentException(
                    $"Az eredmény / megjegyzés legfeljebb {MaxTextLength} karakter lehet.",
                    nameof(resultNotes));
            }

            action.ResultNotes = string.IsNullOrWhiteSpace(resultNotes) ? null : resultNotes.Trim();
        }

        var utcNow = nowUtc.ToUniversalTime();
        ApplyStatusTransitionInternal(action, newStatus, utcNow);
        action.UpdatedAt = utcNow;
    }

    private static void ApplyStatusTransitionInternal(
        MitigationAction action,
        MitigationStatus newStatus,
        DateTimeOffset utcNow)
    {
        if (newStatus == MitigationStatus.Completed)
        {
            // Ha most válik befejezetté (vagy még nem volt CompletedAt), rögzítjük az időpontot
            action.CompletedAt ??= utcNow;
        }
        else
        {
            // Visszanyitáskor (Planned / InProgress) vagy visszavonáskor (Cancelled) töröljük a teljesítési időpontot
            action.CompletedAt = null;
        }

        action.Status = newStatus;
    }
}

