using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true, collation: "case_insensitive"),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "biz",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "biz",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "integer", nullable: false),
                    UsersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RolesId",
                        column: x => x.RolesId,
                        principalSchema: "biz",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UsersId",
                        column: x => x.UsersId,
                        principalSchema: "biz",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "roles",
                columns: new[] { "Id", "Description", "InsertedAt", "InsertedById", "IsActive", "IsSystem", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[,]
                {
                    { 1, "Full system-wide access; manages all users, department pages and approval chains.", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Super Admin" },
                    { 2, "Full operational access; manages managers, employees and departments, but not other admins.", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Admin" },
                    { 3, "Department-scoped: manages own-department employees and approves or alters orders at their stage.", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Manager" },
                    { 4, "Executes and completes orders within their department and adds notes.", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Employee" },
                    { 5, "Read-only access to their own orders; no commissions, margins or internal notes.", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Client" }
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "users",
                columns: new[] { "Id", "DepartmentId", "Email", "FirstName", "InsertedAt", "InsertedById", "IsActive", "LastName", "LastUpdatedAt", "LastUpdatedById", "Mobile" },
                values: new object[,]
                {
                    { 1, 1, "superadmin@orchestrator.local", "Super", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Admin", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "0000000000" },
                    { 2, 2, "admin@orchestrator.local", "Admin", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "User", new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "0000000000" }
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "user_roles",
                columns: new[] { "RolesId", "UsersId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                schema: "biz",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UsersId",
                schema: "biz",
                table: "user_roles",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_users_DepartmentId",
                schema: "biz",
                table: "users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "biz",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "users",
                schema: "biz");
        }
    }
}
