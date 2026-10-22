using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "department_roles",
                schema: "biz",
                columns: table => new
                {
                    DepartmentsId = table.Column<int>(type: "integer", nullable: false),
                    RolesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_roles", x => new { x.DepartmentsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_department_roles_departments_DepartmentsId",
                        column: x => x.DepartmentsId,
                        principalSchema: "biz",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_department_roles_roles_RolesId",
                        column: x => x.RolesId,
                        principalSchema: "biz",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "department_roles",
                columns: new[] { "DepartmentsId", "RolesId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_department_roles_RolesId",
                schema: "biz",
                table: "department_roles",
                column: "RolesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "department_roles",
                schema: "biz");
        }
    }
}
