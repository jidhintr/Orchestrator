using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "biz");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:public.case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "departments",
                columns: new[] { "Id", "Code", "Description", "InsertedAt", "InsertedById", "IsActive", "IsSystem", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[,]
                {
                    { 1, "SUPER_ADMIN", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Super Admin" },
                    { 2, "ADMIN", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Admin" },
                    { 3, "ACCOUNTS", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Accounts" },
                    { 4, "QA", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "QA" },
                    { 5, "DELIVERY", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Delivery" },
                    { 6, "CLIENTS", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Clients" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                schema: "biz",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_Name",
                schema: "biz",
                table: "departments",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "departments",
                schema: "biz");
        }
    }
}
