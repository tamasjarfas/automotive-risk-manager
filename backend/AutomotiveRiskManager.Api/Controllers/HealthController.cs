// HealthController.cs – egyszerű állapot-ellenőrző végpont.
//
// Ez az első és legegyszerűbb API végpontunk. Célja:
//   1. Ellenőrizni, hogy a backend szerver fut-e.
//   2. Megmutatni, hogy a frontend képes-e elérni a backendet.
//
// Elérhető: GET /api/health

using Microsoft.AspNetCore.Mvc;

namespace AutomotiveRiskManager.Api.Controllers;

/// <summary>
/// Az alkalmazás elérhetőségét ellenőrző végpont.
/// Nem igényel hitelesítést – bárki lekérdezheti.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Az alkalmazás állapotát jelzi vissza.
    /// </summary>
    /// <returns>JSON objektum az aktuális állapottal és időbélyeggel.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Application = "Automotive Risk Manager API",
            Version = "0.1.0",
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Message = "A backend szerver sikeresen fut és elérhető."
        });
    }
}

