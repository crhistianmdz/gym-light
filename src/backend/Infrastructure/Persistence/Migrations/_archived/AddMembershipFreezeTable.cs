using Microsoft.EntityFrameworkCore.Migrations;

namespace GymFlow.Infrastructure.Persistence.Migrations;

/// <summary>
/// HU-07 — Crea la tabla MembershipFreezes para registrar eventos de congelamiento.
///
/// Para aplicar:
///   dotnet ef migrations add AddMembershipFreezeTable \
///     --project src/backend/Infrastructure \
///     --startup-project src/backend/WebAPI
///   dotnet ef database update --project src/backend/Infrastructure
///
/// Índice: IX_MembershipFreezes_MemberId_StartDate — optimiza consultas por socio+año.
/// FK: MemberId → Members(Id) ON DELETE CASCADE.
/// </summary>
public partial class AddMembershipFreezeTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MembershipFreezes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                DurationDays = table.Column<int>(type: "integer", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MembershipFreezes", x => x.Id);
                table.ForeignKey(
                    name: "FK_MembershipFreezes_Members_MemberId",
                    column: x => x.MemberId,
                    principalTable: "Members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MembershipFreezes_MemberId_StartDate",
            table: "MembershipFreezes",
            columns: new[] { "MemberId", "StartDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MembershipFreezes");
    }
}
