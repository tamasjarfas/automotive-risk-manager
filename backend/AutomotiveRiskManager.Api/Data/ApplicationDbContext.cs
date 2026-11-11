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

    /// <summary>A „risks" tábla – a beszállítói információbiztonsági kockázatok gyűjteménye.</summary>
    public DbSet<Risk> Risks { get; set; }

    /// <summary>A „mitigation_actions" tábla – a kockázatcsökkentő intézkedések gyűjteménye.</summary>
    public DbSet<MitigationAction> MitigationActions { get; set; }

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

        // ─── Kockázatok (risks tábla) ─────────────────────────────────────────────
        modelBuilder.Entity<Risk>(entity =>
        {
            entity.ToTable("risks");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Title)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(r => r.Description)
                  .HasMaxLength(2000);

            entity.Property(r => r.Category)
                  .HasMaxLength(100)
                  .IsRequired();

            // A számított kockázati szint (Low, Medium, High, Critical) és a státusz
            // (Open, UnderReview, Mitigated, Closed) olvasható stringként tárolódik.
            entity.Property(r => r.Level)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(r => r.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(r => r.CreatedAt)
                  .HasColumnType("timestamp with time zone");

            entity.Property(r => r.UpdatedAt)
                  .HasColumnType("timestamp with time zone");

            // Idegen kulcs a suppliers táblára.
            // Restrict: olyan beszállító, amelyhez kockázat tartozik, nem törölhető fizikailag.
            entity.HasOne(r => r.Supplier)
                  .WithMany()
                  .HasForeignKey(r => r.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexek a gyors beszállítói és kockázatiszint-alapú szűréshez
            entity.HasIndex(r => r.SupplierId).HasDatabaseName("ix_risks_supplier_id");
            entity.HasIndex(r => r.Level).HasDatabaseName("ix_risks_level");
            entity.HasIndex(r => r.Status).HasDatabaseName("ix_risks_status");
        });

        // ─── Kockázatcsökkentő intézkedések (mitigation_actions tábla) ────────────
        modelBuilder.Entity<MitigationAction>(entity =>
        {
            entity.ToTable("mitigation_actions");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Title)
                  .HasMaxLength(200)
                  .IsRequired();

            entity.Property(m => m.Description)
                  .HasMaxLength(2000);

            entity.Property(m => m.ResponsiblePerson)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(m => m.ResultNotes)
                  .HasMaxLength(2000);

            // A státusz (Planned, InProgress, Completed, Cancelled) és a prioritás
            // (Low, Medium, High, Critical) olvasható stringként tárolódik.
            entity.Property(m => m.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(m => m.Priority)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(m => m.DueDate)
                  .HasColumnType("timestamp with time zone");

            entity.Property(m => m.CompletedAt)
                  .HasColumnType("timestamp with time zone");

            entity.Property(m => m.CreatedAt)
                  .HasColumnType("timestamp with time zone");

            entity.Property(m => m.UpdatedAt)
                  .HasColumnType("timestamp with time zone");

            // Kötelező idegen kulcs a risks táblára (1:N kapcsolat: egy kockázathoz több intézkedés).
            // Restrict: a kockázat nem törölhető véletlenül, kaszkád módon az intézkedésekkel együtt.
            entity.HasOne(m => m.Risk)
                  .WithMany(r => r.MitigationActions)
                  .HasForeignKey(m => m.RiskId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Indexek a kockázat, státusz, határidő és felelős szerinti gyors szűréshez
            entity.HasIndex(m => m.RiskId).HasDatabaseName("ix_mitigation_actions_risk_id");
            entity.HasIndex(m => m.Status).HasDatabaseName("ix_mitigation_actions_status");
            entity.HasIndex(m => m.DueDate).HasDatabaseName("ix_mitigation_actions_due_date");
            entity.HasIndex(m => m.ResponsiblePerson).HasDatabaseName("ix_mitigation_actions_responsible");
        });
    }
}

