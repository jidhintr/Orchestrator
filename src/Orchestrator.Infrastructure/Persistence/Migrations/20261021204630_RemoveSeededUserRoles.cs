using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "biz",
                table: "user_roles",
                keyColumns: new[] { "RolesId", "UsersId" },
                keyValues: new object[] { 1, 1 });

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "user_roles",
                keyColumns: new[] { "RolesId", "UsersId" },
                keyValues: new object[] { 2, 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "biz",
                table: "user_roles",
                columns: new[] { "RolesId", "UsersId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 2 }
                });
        }
    }
}
