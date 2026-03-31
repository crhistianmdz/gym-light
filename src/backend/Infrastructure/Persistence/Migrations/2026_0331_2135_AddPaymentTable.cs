using Microsoft.EntityFrameworkCore.Migrations;

namespace GymFlow.Infrastructure.Persistence.Migrations;

/// <summary>
/// Añade la tabla Payments, incluyendo índices y claves foráneas.
/// Ver PRD §4 para reglas de negocio aplicadas.
/// </summary>
public partial class AddPaymentTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                SaleId = table.Column<Guid>(type: "uuid", nullable: true),
                ClientGuid = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_AppUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Payments_ClientGuid",
            table: "Payments",
            column: "ClientGuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_Timestamp",
            table: "Payments",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_MemberId_Timestamp",
            table: "Payments",
            columns: new[] { "MemberId", "Timestamp" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Payments");
    }
}