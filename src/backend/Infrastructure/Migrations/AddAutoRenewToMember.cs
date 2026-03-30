using Microsoft.EntityFrameworkCore.Migrations;

namespace GymFlow.Infrastructure.Persistence.Migrations
{
    public partial class AddAutoRenewAndCancelledAtToMember : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenewEnabled",
                table: "Members",
                type: "BIT",
                nullable: false,
                defaultValue: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Members",
                type: "DATETIME2",
                nullable: true
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AutoRenewEnabled", table: "Members");
            migrationBuilder.DropColumn(name: "CancelledAt", table: "Members");
        }
    }
}