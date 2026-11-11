// Models/AuditEnums.cs
// Az auditnaplózási modul (AuditLog) felsorolási típusai.
//
// Tervezési elvek:
//   - Az adatbázisban mind az AuditAction, mind az AuditEntityType olvasható
//     szövegként (HasConversion<string>()) tárolódik.
//   - Kizárólag előre definiált eseménytípusok és entitástípusok rögzíthetők.

namespace AutomotiveRiskManager.Api.Models;

/// <summary>
/// A naplózott üzleti vagy adminisztratív művelet típusa.
/// </summary>
public enum AuditAction
{
    /// <summary>Új rekord létrehozása (beszállító, kockázat, intézkedés, felhasználó).</summary>
    Created = 1,

    /// <summary>Meglévő rekord adatainak vagy kockázatértékelésének módosítása.</summary>
    Updated = 2,

    /// <summary>Státusz megváltoztatása (pl. kockázat vagy intézkedés állapotváltása).</summary>
    StatusChanged = 3,

    /// <summary>Beszállító vagy felhasználói fiók inaktiválása.</summary>
    Deactivated = 4,

    /// <summary>Beszállító vagy felhasználói fiók újraaktiválása.</summary>
    Activated = 5,

    /// <summary>Felhasználói szerepkör megváltoztatása.</summary>
    RoleChanged = 6,

    /// <summary>Kockázatcsökkentő intézkedés befejezése (Completed).</summary>
    Completed = 7,

    /// <summary>Korábban befejezett vagy lezárt elem visszanyitása.</summary>
    Reopened = 8
}

/// <summary>
/// Az auditált üzleti vagy biztonsági entitás típusa.
/// </summary>
public enum AuditEntityType
{
    Supplier = 1,
    Risk = 2,
    MitigationAction = 3,
    User = 4
}

