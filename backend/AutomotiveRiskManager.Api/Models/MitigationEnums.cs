// Models/MitigationEnums.cs
// A kockázatcsökkentő intézkedések (MitigationAction) felsorolási típusai.
//
// Tervezési elvek:
//   - A státusz (MitigationStatus) és a prioritás (MitigationPriority) az adatbázisban
//     olvasható szövegként (HasConversion<string>()) tárolódik.
//   - Fizikai törlés (hard delete) helyett a Cancelled (Visszavont) állapotot
//     használjuk, így az intézkedések történetisége és auditálhatósága megmarad.
//   - A határidő lejárta (IsOverdue) NEM külön státusz, hanem számított jelző,
//     hogy ne írja felül a tényleges munkafolyamat-állapotot (Planned / InProgress).

namespace AutomotiveRiskManager.Api.Models;

/// <summary>
/// A kockázatcsökkentő intézkedés végrehajtási állapota.
/// </summary>
public enum MitigationStatus
{
    /// <summary>Tervezett – az intézkedés ki lett jelölve, a végrehajtás még nem indult el.</summary>
    Planned = 1,

    /// <summary>Folyamatban – a kockázatcsökkentő intézkedés végrehajtása zajlik.</summary>
    InProgress = 2,

    /// <summary>Befejezett – az intézkedés végrehajtásra került (CompletedAt rögzítve).</summary>
    Completed = 3,

    /// <summary>Visszavont – az intézkedést törlés helyett visszavonták / archiválták.</summary>
    Cancelled = 4
}

/// <summary>
/// A kockázatcsökkentő intézkedés végrehajtási prioritása.
/// </summary>
public enum MitigationPriority
{
    /// <summary>Alacsony prioritás.</summary>
    Low = 1,

    /// <summary>Közepes prioritás.</summary>
    Medium = 2,

    /// <summary>Magas prioritás.</summary>
    High = 3,

    /// <summary>Kritikus / azonnali prioritás.</summary>
    Critical = 4
}

