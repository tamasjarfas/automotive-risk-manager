// Data/ApplicationDbContext.cs
// Az Entity Framework Core adatbázis-kontextus osztály.
//
// Az ApplicationDbContext a kapcsolat az alkalmazás és a PostgreSQL
// adatbázis között. Minden adatbázis-műveletet ezen keresztül végzünk.
//
// Felelősségei:
//   1. DbSet<T> tulajdonságok: ezek teszik elérhetővé a táblákat LINQ-ból.
//   2. OnModelCreating: finomhangolja az entitás-tábla leképezést,
//      és beágyazza a kezdeti tesztadatokat (seed data).
//   3. Tranzakciókezelés: az EF Core automatikusan kezeli a tranzakciókat.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AutomotiveRiskManager.Api.Models;

namespace AutomotiveRiskManager.Api.Data;

// Az IdentityDbContext hozzáadja az Identity táblákat (felhasználók, szerepkörök, ...)
// a meglévő Suppliers tábla mellé. A Suppliers leképezése és adatai változatlanok.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    // A konstruktor a konfigurációt a dependency injection rendszerből kapja.
    // Ezt a Program.cs regisztrálja az IoC konténerbe.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ─── Táblák (DbSet-ek) ────────────────────────────────────────────────────

    /// <summary>A „suppliers" tábla – a Supplier entitások gyűjteménye.</summary>
    public DbSet<Supplier> Suppliers { get; set; }

    // ─── Modell konfiguráció ──────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── Identity táblák: az egységes (snake_case) elnevezéshez igazítva ──────
        // A "suppliers" táblához hasonlóan kisbetűs táblanevek; a Suppliers érintetlen.
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(u => u.DisplayName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone");
        });
        modelBuilder.Entity<ApplicationRole>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        modelBuilder.Entity<Supplier>(entity =>
        {
            // Elsődleges kulcs: az Id mező UUID, az adatbázis generálja
            entity.HasKey(s => s.Id);

            // A SupplierStatus enum adatbázisban olvasható string-ként tárolódik
            // (pl. "Active" / "Inactive" – nem numerikus kód)
            entity.Property(s => s.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Az időbélyeg UTC-ben kerül tárolásra, típusa: timestamp with time zone
            entity.Property(s => s.CreatedAt)
                  .HasColumnType("timestamp with time zone");

            entity.Property(s => s.UpdatedAt)
                  .HasColumnType("timestamp with time zone");

            // Névindex a gyorsabb kereséshez
            entity.HasIndex(s => s.Name).HasDatabaseName("ix_suppliers_name");

            // ─── Seed data: kezdeti fiktív tesztadatok ────────────────────────
            // Ezek az adatok az első migráció futtatásakor kerülnek be.
            // A Guid értékek és időpontok fixek, hogy a migráció idempotens legyen.
            entity.HasData(
                new Supplier
                {
                    Id = new Guid("11111111-1111-1111-1111-111111111111"),
                    Name = "Elektronikai Beszállító A Kft.",
                    Activity = "Elektronikai alkatrészgyártás",
                    Description = "Autóipari elektronikai részegységek, szenzormodulok és vezérlőegységek gyártása. Fiktív tesztbeszállító.",
                    Status = SupplierStatus.Active,
                    CreatedAt = new DateTimeOffset(2025, 3, 10, 9, 0, 0, TimeSpan.Zero),
                    UpdatedAt = null
                },
                new Supplier
                {
                    Id = new Guid("22222222-2222-2222-2222-222222222222"),
                    Name = "Szoftverfejlesztő Beszállító B Zrt.",
                    Activity = "Autóipari szoftverfejlesztés",
                    Description = "Beágyazott szoftverek, diagnosztikai rendszerek és AUTOSAR-alapú szoftverfejlesztés. Fiktív tesztbeszállító.",
                    Status = SupplierStatus.Active,
                    CreatedAt = new DateTimeOffset(2025, 5, 22, 11, 30, 0, TimeSpan.Zero),
                    UpdatedAt = null
                },
                new Supplier
                {
                    Id = new Guid("33333333-3333-3333-3333-333333333333"),
                    Name = "Logisztikai Beszállító C Kft.",
                    Activity = "Logisztikai szolgáltatások",
                    Description = "Alkatrész-szállítás, raktározás és ellátásilánc-kezelés autóipari partnerek részére. Fiktív tesztbeszállító.",
                    Status = SupplierStatus.Active,
                    CreatedAt = new DateTimeOffset(2025, 7, 1, 8, 0, 0, TimeSpan.Zero),
                    UpdatedAt = null
                }
            );
        });
    }
}

