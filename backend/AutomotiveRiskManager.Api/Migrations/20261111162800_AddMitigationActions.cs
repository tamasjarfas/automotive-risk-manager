using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomotiveRiskManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMitigationActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mitigation_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResponsiblePerson = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mitigation_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mitigation_actions_risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mitigation_actions_due_date",
                table: "mitigation_actions",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "ix_mitigation_actions_responsible",
                table: "mitigation_actions",
                column: "ResponsiblePerson");

            migrationBuilder.CreateIndex(
                name: "ix_mitigation_actions_risk_id",
                table: "mitigation_actions",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "ix_mitigation_actions_status",
                table: "mitigation_actions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mitigation_actions");
        }
    }
}
