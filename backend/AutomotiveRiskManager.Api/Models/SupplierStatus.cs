// Models/SupplierStatus.cs
// A beszállító lehetséges állapotait definiáló enum.
//
// Miért enum?
//   - Típusbiztonságot ad: lehetetlen érvénytelen értéket beállítani.
//   - Az adatbázisban string-ként tárolódik (olvasható, nem numerikus kód).
//   - Könnyen bővíthető, ha a jövőben új állapot kerül bevezetésre.

namespace AutomotiveRiskManager.Api.Models;

public enum SupplierStatus
{
    Active,     // Aktív – jelenlegi, érvényes beszállítói kapcsolat
    Inactive    // Inaktív – felfüggesztett vagy megszűnt kapcsolat
}

