// GymFlow Lite - Schema Versioning (HU-017)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymFlow.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaVersionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schema_version",
                columns: table => new
                {
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MigrationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RollbackSql = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_version", x => x.Version);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schema_version_module_name",
                table: "schema_version",
                column: "ModuleName");

            migrationBuilder.CreateIndex(
                name: "IX_schema_version_applied_at",
                table: "schema_version",
                column: "AppliedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schema_version");
        }
    }
}
