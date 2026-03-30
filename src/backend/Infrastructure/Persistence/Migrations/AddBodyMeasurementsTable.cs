using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.Infrastructure.Persistence.Migrations;

/// <summary>
/// EF Core migration to add the BodyMeasurements table.
/// </summary>
public partial class AddBodyMeasurementsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BodyMeasurements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecordedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                WeightKg = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                BodyFatPct = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                ChestCm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                WaistCm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                HipCm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                ArmCm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                LegCm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                UnitSystem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ClientGuid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BodyMeasurements", x => x.Id);
                table.ForeignKey(
                    name: "FK_BodyMeasurements_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BodyMeasurements_ClientGuid",
            table: "BodyMeasurements",
            column: "ClientGuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BodyMeasurements_MemberId_RecordedAt",
            table: "BodyMeasurements",
            columns: new[] { "MemberId", "RecordedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BodyMeasurements");
    }
}