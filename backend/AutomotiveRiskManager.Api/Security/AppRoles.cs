// Security/AppRoles.cs
// A szerepkörök és jogosultsági szabályzatok (policy-k) nevei egy helyen.
// Így nincs elírható "magic string" a vezérlőkben.

namespace AutomotiveRiskManager.Api.Security;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string RiskManager = "RiskManager";
    public const string Viewer = "Viewer";

    public static readonly string[] All = { Administrator, RiskManager, Viewer };
}

/// <summary>
/// Jogosultsági szabályzatok. A vezérlők ezekre hivatkoznak: [Authorize(Policy = ...)].
/// A szabályzat → szerepkör hozzárendelés a Program.cs-ben van (egyetlen helyen).
/// </summary>
public static class AppPolicies
{
    /// <summary>Beszállítók megtekintése: minden bejelentkezett szerepkör.</summary>
    public const string ViewSuppliers = "ViewSuppliers";

    /// <summary>Beszállítók létrehozása, módosítása, inaktiválása: Administrator, RiskManager.</summary>
    public const string ManageSuppliers = "ManageSuppliers";

    /// <summary>Kockázatok megtekintése: minden bejelentkezett szerepkör (Administrator, RiskManager, Viewer).</summary>
    public const string ViewRisks = "ViewRisks";

    /// <summary>Kockázatok létrehozása, módosítása, értékelése és státuszváltása: Administrator, RiskManager.</summary>
    public const string ManageRisks = "ManageRisks";

    /// <summary>Kockázatcsökkentő intézkedések megtekintése: minden bejelentkezett szerepkör (Administrator, RiskManager, Viewer).</summary>
    public const string ViewMitigations = "ViewMitigations";

    /// <summary>Kockázatcsökkentő intézkedések létrehozása, módosítása, státuszváltása és befejezése: Administrator, RiskManager.</summary>
    public const string ManageMitigations = "ManageMitigations";

    /// <summary>Felhasználók és szerepkörök kezelése: csak Administrator.</summary>
    public const string ManageUsers = "ManageUsers";
}

