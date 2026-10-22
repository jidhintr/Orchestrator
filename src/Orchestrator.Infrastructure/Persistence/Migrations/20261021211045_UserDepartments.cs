using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_departments",
                schema: "biz",
                columns: table => new
                {
                    DepartmentsId = table.Column<int>(type: "integer", nullable: false),
                    UsersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_departments", x => new { x.DepartmentsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_user_departments_departments_DepartmentsId",
                        column: x => x.DepartmentsId,
                        principalSchema: "biz",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_departments_users_UsersId",
                        column: x => x.UsersId,
                        principalSchema: "biz",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "department_roles",
                columns: new[] { "DepartmentsId", "RolesId" },
                values: new object[] { 6, 5 });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "departments",
                columns: new[] { "Id", "Code", "Description", "InsertedAt", "InsertedById", "IsActive", "IsSystem", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[] { 7, "UNASSIGNED", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Unassigned" });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "user_departments",
                columns: new[] { "DepartmentsId", "UsersId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_departments_UsersId",
                schema: "biz",
                table: "user_departments",
                column: "UsersId");

            migrationBuilder.Sql("INSERT INTO biz.user_departments (\"DepartmentsId\", \"UsersId\") SELECT \"DepartmentId\", \"Id\" FROM biz.users ON CONFLICT DO NOTHING;");

            migrationBuilder.DropForeignKey(
                name: "FK_users_departments_DepartmentId",
                schema: "biz",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_DepartmentId",
                schema: "biz",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "biz",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_departments",
                schema: "biz");

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "department_roles",
                keyColumns: new[] { "DepartmentsId", "RolesId" },
                keyValues: new object[] { 6, 5 });

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "biz",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "users",
                keyColumn: "Id",
                keyValue: 1,
                column: "DepartmentId",
                value: 1);

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "users",
                keyColumn: "Id",
                keyValue: 2,
                column: "DepartmentId",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "IX_users_DepartmentId",
                schema: "biz",
                table: "users",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_departments_DepartmentId",
                schema: "biz",
                table: "users",
                column: "DepartmentId",
                principalSchema: "biz",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
