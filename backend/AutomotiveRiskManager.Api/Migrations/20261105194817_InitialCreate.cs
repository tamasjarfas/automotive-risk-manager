using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AutomotiveRiskManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Activity = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "suppliers",
                columns: new[] { "Id", "Activity", "CreatedAt", "Description", "Name", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Elektronikai alkatrészgyártás", new DateTimeOffset(new DateTime(2025, 3, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Autóipari elektronikai részegységek, szenzormodulok és vezérlőegységek gyártása. Fiktív tesztbeszállító.", "Elektronikai Beszállító A Kft.", "Active", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Autóipari szoftverfejlesztés", new DateTimeOffset(new DateTime(2025, 5, 22, 11, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Beágyazott szoftverek, diagnosztikai rendszerek és AUTOSAR-alapú szoftverfejlesztés. Fiktív tesztbeszállító.", "Szoftverfejlesztő Beszállító B Zrt.", "Active", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Logisztikai szolgáltatások", new DateTimeOffset(new DateTime(2025, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Alkatrész-szállítás, raktározás és ellátásilánc-kezelés autóipari partnerek részére. Fiktív tesztbeszállító.", "Logisztikai Beszállító C Kft.", "Active", null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_name",
                table: "suppliers",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
