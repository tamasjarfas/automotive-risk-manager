// Models/RiskEnums.cs
// A kockázatértékelési modulban használt felsorolási típusok (enumok)
// és az ajánlott autóipari információbiztonsági kategóriák.
//
// Tervezési elvek:
//   - Az adatbázisban mind a RiskLevel, mind a RiskStatus olvasható szövegként
//     (HasConversion<string>()) tárolódik, így az SQL-lekérdezések és naplók
//     közvetlenül értelmezhetők.
//   - Fizikai törlés (hard delete) helyett a Closed (Lezárt / Archivált)
//     állapotot használjuk az auditálhatóság megőrzésére.

namespace AutomotiveRiskManager.Api.Models;

/// <summary>
/// Automatikusan számított kockázati szint a (Valószínűség × Hatás) pontszám alapján:
///   1–4   → Low (Alacsony)
///   5–9   → Medium (Közepes)
///   10–16 → High (Magas)
///   17–25 → Critical (Kritikus)
/// </summary>
public enum RiskLevel
{
    /// <summary>Alacsony kockázat (1–4 pont).</summary>
    Low = 1,

    /// <summary>Közepes kockázat (5–9 pont).</summary>
    Medium = 2,

    /// <summary>Magas kockázat (10–16 pont).</summary>
    High = 3,

    /// <summary>Kritikus kockázat (17–25 pont).</summary>
    Critical = 4
}

/// <summary>
/// A kockázat életciklus-állapota.
/// Fizikai törlés helyett a kockázatok lezárhatók (Closed), így a történetük megmarad.
/// </summary>
public enum RiskStatus
{
    /// <summary>Nyitott (aktív, újonnan azonosított kockázat).</summary>
    Open = 1,

    /// <summary>Felülvizsgálat alatt (elemzés vagy intézkedéstervezés folyamatban).</summary>
    UnderReview = 2,

    /// <summary>Csökkentett / kezelt (a védőintézkedések bevezetve).</summary>
    Mitigated = 3,

    /// <summary>Lezárt / archivált (már nem fennálló vagy elfogadott/lezárt kockázat).</summary>
    Closed = 4
}

/// <summary>
/// Egységes autóipari információbiztonsági (ISMS / TISAX jellegű) kockázati kategóriák.
/// </summary>
public static class RiskCategories
{
    public const string DataProtection       = "Adatvédelem";
    public const string AccessControl        = "Hozzáférés-kezelés";
    public const string BusinessContinuity   = "Üzletmenet-folytonosság";
    public const string NetworkSecurity      = "Hálózatbiztonság";
    public const string SupplierCompliance   = "Beszállítói megfelelőség";
    public const string PhysicalSecurity     = "Fizikai biztonság";
    public const string IncidentManagement   = "Incidenskezelés";
    public const string SoftwareAndSystemSec = "Szoftver- és rendszerbiztonság";

    public static readonly string[] All =
    {
        DataProtection,
        AccessControl,
        BusinessContinuity,
        NetworkSecurity,
        SupplierCompliance,
        PhysicalSecurity,
        IncidentManagement,
        SoftwareAndSystemSec
    };
}

