// Controllers/AuthController.cs
// Hitelesítési végpontok: bejelentkezés, kijelentkezés, aktuális felhasználó, CSRF-token.
//
//   GET  /api/auth/csrf    – CSRF-token lekérése (névtelenül is)
//   POST /api/auth/login   – bejelentkezés (felhasználónév vagy e-mail + jelszó)
//   POST /api/auth/logout  – kijelentkezés
//   GET  /api/auth/me      – a bejelentkezett felhasználó adatai
//
// Biztonsági megoldások:
//   - A munkamenet HTTP-only, Secure, SameSite=Strict cookie-ban él (lásd Program.cs).
//   - Minden állapotmódosító kérés (POST/PUT/PATCH/DELETE) CSRF-tokent igényel,
//     a login és a logout is (globális AutoValidateAntiforgeryToken szűrő).
//   - Hibás azonosító, hibás jelszó, zárolt vagy inaktív fiók esetén UGYANAZ a válasz
//     érkezik (nincs felhasználó-felderítés), és az időzítés is kiegyenlített.
//   - 5 sikertelen próbálkozás után a fiók 15 percre zárolódik (Program.cs).
//   - A naplóba sem a jelszó, sem a megadott azonosító nem kerül.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutomotiveRiskManager.Api.DTOs;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Időzítés-kiegyenlítés: ismeretlen felhasználónál is elvégzünk egy jelszókivonat-ellenőrzést,
    // hogy a válaszidőből ne derüljön ki, létezik-e a fiók.
    private static readonly PasswordHasher<ApplicationUser> DummyHasher = new();
    private static readonly ApplicationUser DummyUser = new();
    private static readonly string DummyHash =
        DummyHasher.HashPassword(DummyUser, "Dummy-Password-Only-For-Timing-1");

    private const string LoginFailedMessage =
        "Hibás azonosító vagy jelszó, vagy a fiók jelenleg nem használható.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // ─── GET /api/auth/csrf ───────────────────────────────────────────────────
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Headers.CacheControl = "no-store";
        return Ok(new CsrfTokenDto { Token = tokens.RequestToken ?? string.Empty });
    }

    // ─── POST /api/auth/login ─────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var identifier = request.Identifier.Trim();

        // A felhasználónév nem tartalmazhat '@'-ot, ezért az '@' alapján egyértelmű a keresés.
        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        if (user is null)
        {
            DummyHasher.VerifyHashedPassword(DummyUser, DummyHash, request.Password);
            return LoginFailed();
        }

        // lockoutOnFailure: true → a sikertelen próbálkozások számolódnak, 5 után zárolás.
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded || !user.IsActive)
        {
            return LoginFailed();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        var roles = await _userManager.GetRolesAsync(user);
        Response.Headers.CacheControl = "no-store";
        return Ok(ToCurrentUser(user, roles));
    }

    // ─── POST /api/auth/logout ────────────────────────────────────────────────
    // [AllowAnonymous]: lejárt munkamenetnél is lehessen "kijelentkezni" (a cookie törlődik).
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            // A biztonsági pecsét cseréje a kiszivárgott/másolt cookie-t is érvényteleníti
            // (minden eszközön megszűnik a munkamenet), nem csak a böngészőből törli.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await _signInManager.SignOutAsync();
        return NoContent();
    }

    // ─── GET /api/auth/me ─────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        Response.Headers.CacheControl = "no-store";
        return Ok(ToCurrentUser(user, roles));
    }

    // ─── Segédfüggvények ──────────────────────────────────────────────────────
    private IActionResult LoginFailed()
    {
        _logger.LogWarning("Sikertelen bejelentkezési kísérlet.");
        return Unauthorized(new { message = LoginFailedMessage });
    }

    private static CurrentUserDto ToCurrentUser(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        Roles = roles.ToArray()
    };
}

