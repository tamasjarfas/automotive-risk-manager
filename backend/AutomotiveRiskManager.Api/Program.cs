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
// MEGJEGYZÉS – Swagger/OpenAPI:
//   Ideiglenesen eltávolítva: Swashbuckle 6.x és Microsoft.AspNetCore.OpenApi 10.x
//   inkompatibilis Microsoft.OpenApi verziókat húznak be (CS7069 + NU1903).

using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// ─── Szolgáltatások regisztrálása ───────────────────────────────────────────

// MVC kontrollerek engedélyezése
builder.Services.AddControllers();

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

    // Fejlesztői módban részletes SQL-naplózás (segít a hibakeresésben)
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();  // FIGYELEM: soha ne legyen éles környezetben!
    }
});

// ─── CORS ───────────────────────────────────────────────────────────────────
// Fejlesztés alatt engedélyezzük a Vite dev szerver (5173-as port) elérését.
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

// Fejlesztői módban automatikus migráció alkalmazása induláskor.
// Ez biztosítja, hogy az adatbázis mindig naprakész legyen a kód állapotával.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Csak a függőben lévő migrációkat futtatja – nem töröl adatot!
    await db.Database.MigrateAsync();
}

app.UseCors("FrontendDevPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();

