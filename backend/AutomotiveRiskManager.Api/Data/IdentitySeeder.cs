// Data/IdentitySeeder.cs
// Induláskor (Development módban) biztosítja a három szerepkör meglétét, és
// szükség esetén létrehozza az ELSŐ Administrator felhasználót.
//
// Biztonsági elvek:
//   - A jelszó SEHOL nincs a forráskódban. Konfigurációból jön (User Secrets vagy
//     Codespaces-titok), és a naplóba soha nem kerül ki.
//   - Csak akkor jön létre rendszergazda, ha MÉG NINCS egyetlen Administrator sem.
//     Ha már van, a konfigurációt figyelmen kívül hagyja (nem írja felül a jelszót).
//   - Nincs nyilvános regisztrációs végpont: Administrator csak ezen a úton, illetve
//     egy már bejelentkezett Administrator által jöhet létre.
//
// Konfigurációs kulcsok (bármelyik forrásból, pl. User Secrets):
//   SeedAdmin:UserName    (vagy környezeti változó: ARM_ADMIN_USERNAME)
//   SeedAdmin:Email       (vagy: ARM_ADMIN_EMAIL)
//   SeedAdmin:Password    (vagy: ARM_ADMIN_PASSWORD)
//   SeedAdmin:DisplayName (opcionális)

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using AutomotiveRiskManager.Api.Models;
using AutomotiveRiskManager.Api.Security;

namespace AutomotiveRiskManager.Api.Data;

public static class IdentitySeeder
{
    // A felhasználónév nem tartalmazhat '@'-ot, hogy ne lehessen összekeverni egy e-mail-címmel.
    private static readonly Regex UserNamePattern = new("^[A-Za-z0-9._-]{3,64}$", RegexOptions.Compiled);

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1) Szerepkörök (idempotens: csak a hiányzókat hozza létre)
        foreach (var roleName in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var created = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"A(z) {roleName} szerepkör létrehozása sikertelen: {ErrorCodes(created)}");
            }

            logger.LogInformation("Szerepkör létrehozva: {Role}", roleName);
        }

        // 2) Első Administrator – csak ha még nincs
        var existingAdmins = await userManager.GetUsersInRoleAsync(AppRoles.Administrator);
        if (existingAdmins.Count > 0)
        {
            logger.LogInformation("Van már Administrator felhasználó, a SeedAdmin konfiguráció nem kerül felhasználásra.");
            return;
        }

        var userName = Read(config, "SeedAdmin:UserName", "ARM_ADMIN_USERNAME");
        var email = Read(config, "SeedAdmin:Email", "ARM_ADMIN_EMAIL");
        var password = Read(config, "SeedAdmin:Password", "ARM_ADMIN_PASSWORD");
        var displayName = Read(config, "SeedAdmin:DisplayName", "ARM_ADMIN_DISPLAYNAME") ?? "Rendszergazda";

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Még nincs Administrator felhasználó, és a SeedAdmin konfiguráció (UserName, Email, Password) hiányos. " +
                "Állítsd be a User Secrets-ben vagy Codespaces-titokként, majd indítsd újra az alkalmazást.");
            return;
        }

        userName = userName.Trim();
        if (!UserNamePattern.IsMatch(userName))
        {
            logger.LogError("A SeedAdmin felhasználónév érvénytelen (3–64 karakter: betű, szám, '.', '_', '-').");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = userName,
            Email = email.Trim(),
            EmailConfirmed = true,
            DisplayName = displayName.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            // Csak a hibakódokat naplózzuk, a jelszót és a kapcsolódó értékeket nem.
            logger.LogError("Az első Administrator létrehozása sikertelen: {Errors}", ErrorCodes(createResult));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(admin);   // ne maradjon szerepkör nélküli fiók
            logger.LogError("Az Administrator szerepkör hozzárendelése sikertelen: {Errors}", ErrorCodes(roleResult));
            return;
        }

        logger.LogInformation(
            "Az első Administrator létrehozva: {UserName}. A SeedAdmin titkokat most már törölheted a konfigurációból.",
            admin.UserName);
    }

    private static string? Read(IConfiguration config, string key, string envKey) =>
        config[key] ?? config[envKey];

    private static string ErrorCodes(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Code));
}

