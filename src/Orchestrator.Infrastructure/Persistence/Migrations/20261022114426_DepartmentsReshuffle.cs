using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentsReshuffle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "biz",
                table: "department_roles",
                keyColumns: new[] { "DepartmentsId", "RolesId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "user_departments",
                keyColumns: new[] { "DepartmentsId", "UsersId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Name" },
                values: new object[] { "ADMINISTRATIVES", "Administratives" });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Name" },
                values: new object[] { "ACCOUNTING", "Accounting" });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Name" },
                values: new object[] { "QUALITY_CHECK", "Quality Check" });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Name" },
                values: new object[] { "CUSTOMERS", "Customers" });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "user_departments",
                columns: new[] { "DepartmentsId", "UsersId" },
                values: new object[] { 2, 1 });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "user_roles",
                columns: new[] { "RolesId", "UsersId" },
                values: new object[] { 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "biz",
                table: "user_departments",
                keyColumns: new[] { "DepartmentsId", "UsersId" },
                keyValues: new object[] { 2, 1 });

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "user_roles",
                keyColumns: new[] { "RolesId", "UsersId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Name" },
                values: new object[] { "ADMIN", "Admin" });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Name" },
                values: new object[] { "ACCOUNTS", "Accounts" });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Name" },
                values: new object[] { "QA", "QA" });

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Name" },
                values: new object[] { "CLIENTS", "Clients" });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "departments",
                columns: new[] { "Id", "Code", "Description", "InsertedAt", "InsertedById", "IsActive", "IsSystem", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[,]
                {
                    { 1, "SUPER_ADMIN", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Super Admin" },
                    { 5, "DELIVERY", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Delivery" }
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "department_roles",
                columns: new[] { "DepartmentsId", "RolesId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "user_departments",
                columns: new[] { "DepartmentsId", "UsersId" },
                values: new object[] { 1, 1 });
        }
    }
}
