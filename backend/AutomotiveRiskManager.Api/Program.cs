// Program.cs – az ASP.NET Core alkalmazás belépési pontja.
// Itt állítjuk be az összes middleware-t, szolgáltatást és konfigurációt.
//
// Adatbázis-jelszó kezelése:
//   A kapcsolati sztring (ConnectionStrings:DefaultConnection) a következő
//   prioritási sorrendben töltődik be (minden szint felülírja az előzőt):
//     1. appsettings.json         – alapértelmezett (placeholder, nincs jelszó)
//     2. appsettings.Development.json – fejlesztői konfig (nincs Git-ben)
//     3. .NET User Secrets        – jelszó biztonságos helyi tárolásra (ajánlott)
//     4. Környezeti változó       – éles (production) deploymentre
//
// Hitelesítés és jogosultság (3. fejlesztési szakasz):
//   - ASP.NET Core Identity (felhasználók, szerepkörök, jelszókivonat) PostgreSQL-ben
//   - Cookie-alapú munkamenet: HTTP-only, Secure, SameSite=Strict
//   - CSRF-védelem (Antiforgery) minden állapotmódosító kérésnél
//   - Alapértelmezetten MINDEN végpont hitelesítést igényel (FallbackPolicy);
//     kivétel csak az explicit [AllowAnonymous] végpontok (health, csrf, login, logout)
//   - A jogosultságokat szabályzatok (policy) írják le, szerveroldalon érvényesülnek
//
// MEGJEGYZÉS – Swagger/OpenAPI:
//   Ideiglenesen eltávolítva: Swashbuckle 6.x és Microsoft.AspNetCore.OpenApi 10.x
//   inkompatibilis Microsoft.OpenApi verziókat húznak be (CS7069 + NU1903).

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Data;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// ─── Szolgáltatások regisztrálása ───────────────────────────────────────────

// MVC kontrollerek. Az AutoValidateAntiforgeryToken szűrő minden állapotmódosító
// kérésnél (POST, PUT, PATCH, DELETE) megköveteli a CSRF-tokent; a GET kérések szabadok.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// ─── Adatbázis-kapcsolat (Entity Framework Core + PostgreSQL) ───────────────
// A kapcsolati sztring a konfigurációs rendszerből (User Secrets / env) olvasódik.
// Soha ne kerüljön jelszó a forráskódba!
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A 'DefaultConnection' kapcsolati sztring nincs beállítva. " +
        "Futtasd: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\""
    );

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Ha a kapcsolat ideiglenesen megszakad, az EF Core automatikusan újrapróbál
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );
    });

    // Részletes hibaüzenetek fejlesztéskor. A SensitiveDataLogging SZÁNDÉKOSAN ki van
    // kapcsolva: bekapcsolva a SQL-paraméterek (pl. e-mail-címek, azonosítók) is bekerülnének a naplóba.
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

// ─── ASP.NET Core Identity ──────────────────────────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Jelszószabályok (a jelszavak kivonatolva tárolódnak, nyílt szövegben soha)
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 4;

        // Zárolás: 5 sikertelen próbálkozás után 15 percre (az új fiókokra is érvényes)
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // Egyedi e-mail-cím (a bejelentkezés e-mail-címmel is működik)
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<HungarianIdentityErrorDescriber>();

// A munkamenet-cookie beállításai
builder.Services.ConfigureApplicationCookie(options =>
{
    // A "__Host-" előtag: a böngésző csak Secure, Path=/ és Domain nélküli cookie-t fogad el.
    options.Cookie.Name = "__Host-ARM.Auth";
    options.Cookie.HttpOnly = true;                              // JavaScript nem olvashatja
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;     // csak HTTPS-en (a Codespaces HTTPS-en át éri el)
    options.Cookie.SameSite = SameSiteMode.Strict;               // más oldalról indított kérésekhez nem küldi el
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);           // 30 perc tétlenség után lejár
    options.SlidingExpiration = true;

    // API-ról van szó: átirányítás helyett tiszta HTTP-állapotkódot adunk vissza.
    // Az Events objektumot NEM cseréljük le, mert az Identity már beállította benne a
    // biztonságipecsét-ellenőrzést (OnValidatePrincipal).
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// A biztonsági pecsétet minden kérésnél ellenőrizzük: inaktiválás, szerepkörváltás és
// kijelentkezés után a régi munkamenet azonnal érvénytelen (nincs 30 perces várakozás).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// ─── CSRF-védelem (Antiforgery) ─────────────────────────────────────────────
// A kliens a /api/auth/csrf végpontról kap tokent, és az X-CSRF-TOKEN fejlécben küldi vissza.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-ARM.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "X-CSRF-TOKEN";
});

// ─── Jogosultsági szabályzatok ──────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    // Alapértelmezés: minden végponthoz bejelentkezés kell, hacsak nincs [AllowAnonymous].
    // Így egy később elfelejtett [Authorize] sem hagy védtelen végpontot.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AppPolicies.ViewSuppliers, policy =>
        policy.RequireRole(AppRoles.Administrator, AppRoles.RiskManager, AppRoles.Viewer));

    options.AddPolicy(AppPolicies.ManageSuppliers, policy =>
        policy.RequireRole(AppRoles.Administrator, AppRoles.RiskManager));

    options.AddPolicy(AppPolicies.ViewRisks, policy =>
        policy.RequireRole(AppRoles.Administrator, AppRoles.RiskManager, AppRoles.Viewer));

    options.AddPolicy(AppPolicies.ManageRisks, policy =>
        policy.RequireRole(AppRoles.Administrator, AppRoles.RiskManager));

    options.AddPolicy(AppPolicies.ManageUsers, policy =>
        policy.RequireRole(AppRoles.Administrator));
});

// ─── CORS ───────────────────────────────────────────────────────────────────
// A frontend a Vite proxyn át, azonos origin-ről éri el az API-t, ezért a böngészőben
// nincs szükség CORS-ra. A szabály fejlesztési tartalék; hitelesítő adatokat (cookie)
// cross-origin kérésekre szándékosan NEM engedünk (nincs AllowCredentials).
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ─── Alkalmazás felépítése ───────────────────────────────────────────────────

var app = builder.Build();
app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto, KnownProxies = { System.Net.IPAddress.Loopback, System.Net.IPAddress.IPv6Loopback } });

// Fejlesztői módban automatikus migráció és kezdeti adatok (szerepkörök, első Administrator).
// A MigrateAsync csak a függőben lévő migrációkat futtatja – nem töröl adatot!
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

// Az API-válaszok (bejelentkezett felhasználók adatai) ne kerüljenek gyorsítótárba.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }

    await next();
});

app.UseCors("FrontendDevPolicy");
app.UseAuthentication();   // előbb azonosítás (ki vagy?) ...
app.UseAuthorization();    // ... aztán jogosultság (mit tehetsz?)
app.MapControllers();

app.Run();
