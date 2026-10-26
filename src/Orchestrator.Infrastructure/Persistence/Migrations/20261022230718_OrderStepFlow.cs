using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderStepFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedUserId",
                schema: "biz",
                table: "order_item_job_steps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanReject",
                schema: "biz",
                table: "order_item_job_steps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPercent",
                schema: "biz",
                table: "order_item_job_steps",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "biz",
                table: "order_item_job_steps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedByUserId",
                schema: "biz",
                table: "order_item_job_steps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "biz",
                table: "order_item_job_steps",
                type: "integer",
                nullable: false,
                defaultValue: 9);

            migrationBuilder.AddColumn<decimal>(
                name: "EarnedAmount",
                schema: "biz",
                table: "order_item_job_steps",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborCost",
                schema: "biz",
                table: "order_item_job_steps",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitPercent",
                schema: "biz",
                table: "order_item_job_steps",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                schema: "biz",
                table: "order_item_job_steps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                schema: "biz",
                table: "order_item_job_steps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StandardMinutes",
                schema: "biz",
                table: "order_item_job_steps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                schema: "biz",
                table: "order_item_job_steps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "biz",
                table: "job_types",
                type: "integer",
                nullable: false,
                defaultValue: 9);

            migrationBuilder.AddColumn<int>(
                name: "StandardMinutes",
                schema: "biz",
                table: "catalog_job_steps",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_approvals",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DecidedByUserId = table.Column<int>(type: "integer", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_approvals_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "biz",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_approvals_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "biz",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_approvals_users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "biz",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_job_step_logs",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemJobStepId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_job_step_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_item_job_step_logs_order_item_job_steps_OrderItemJobS~",
                        column: x => x.OrderItemJobStepId,
                        principalSchema: "biz",
                        principalTable: "order_item_job_steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_item_job_step_logs_users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "biz",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"CREATE TEMP TABLE ops_old ON COMMIT DROP AS SELECT ""Id"", ""Description"" FROM biz.departments WHERE ""Id"" <> 9 AND (""Name"" = 'Operations' OR ""Code"" = 'OPERATIONS');
UPDATE biz.departments d SET ""Name"" = d.""Name"" || ' (old)', ""Code"" = LEFT(d.""Code"", 27) || '_OLD' FROM ops_old o WHERE d.""Id"" = o.""Id"";");

            migrationBuilder.InsertData(
                schema: "biz",
                table: "departments",
                columns: new[] { "Id", "Code", "Description", "InsertedAt", "InsertedById", "IsActive", "IsSystem", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[] { 9, "OPERATIONS", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Operations" });

            migrationBuilder.Sql(@"UPDATE biz.departments d SET ""Description"" = o.""Description"" FROM ops_old o WHERE d.""Id"" = 9;
UPDATE biz.user_departments x SET ""DepartmentsId"" = 9 FROM ops_old o WHERE x.""DepartmentsId"" = o.""Id"";
UPDATE biz.department_roles x SET ""DepartmentsId"" = 9 FROM ops_old o WHERE x.""DepartmentsId"" = o.""Id"";
UPDATE biz.approval_chain_stages x SET ""DepartmentId"" = 9 FROM ops_old o WHERE x.""DepartmentId"" = o.""Id"";
UPDATE biz.orders x SET ""ReturnedToDepartmentId"" = 9 FROM ops_old o WHERE x.""ReturnedToDepartmentId"" = o.""Id"";
DELETE FROM biz.departments d USING ops_old o WHERE d.""Id"" = o.""Id"";");

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "job_types",
                keyColumn: "Id",
                keyValue: 1,
                column: "DepartmentId",
                value: 9);

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "job_types",
                keyColumn: "Id",
                keyValue: 2,
                column: "DepartmentId",
                value: 9);

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "job_types",
                keyColumn: "Id",
                keyValue: 3,
                column: "DepartmentId",
                value: 9);

            migrationBuilder.UpdateData(
                schema: "biz",
                table: "job_types",
                keyColumn: "Id",
                keyValue: 4,
                column: "DepartmentId",
                value: 9);

            migrationBuilder.Sql(@"UPDATE biz.order_item_job_steps SET ""CanReject"" = ""SortOrder"" > 1;");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_steps_AssignedUserId",
                schema: "biz",
                table: "order_item_job_steps",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_steps_CompletedByUserId",
                schema: "biz",
                table: "order_item_job_steps",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_steps_DepartmentId_Status",
                schema: "biz",
                table: "order_item_job_steps",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_job_types_DepartmentId",
                schema: "biz",
                table: "job_types",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_order_approvals_DecidedByUserId",
                schema: "biz",
                table: "order_approvals",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_order_approvals_DepartmentId",
                schema: "biz",
                table: "order_approvals",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_order_approvals_OrderId_Sequence",
                schema: "biz",
                table: "order_approvals",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_step_logs_AuthorUserId",
                schema: "biz",
                table: "order_item_job_step_logs",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_step_logs_OrderItemJobStepId",
                schema: "biz",
                table: "order_item_job_step_logs",
                column: "OrderItemJobStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_job_types_departments_DepartmentId",
                schema: "biz",
                table: "job_types",
                column: "DepartmentId",
                principalSchema: "biz",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_item_job_steps_departments_DepartmentId",
                schema: "biz",
                table: "order_item_job_steps",
                column: "DepartmentId",
                principalSchema: "biz",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_item_job_steps_users_AssignedUserId",
                schema: "biz",
                table: "order_item_job_steps",
                column: "AssignedUserId",
                principalSchema: "biz",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_item_job_steps_users_CompletedByUserId",
                schema: "biz",
                table: "order_item_job_steps",
                column: "CompletedByUserId",
                principalSchema: "biz",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        // Down fails when users, chains or orders still reference the Operations department (id 9).
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_types_departments_DepartmentId",
                schema: "biz",
                table: "job_types");

            migrationBuilder.DropForeignKey(
                name: "FK_order_item_job_steps_departments_DepartmentId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropForeignKey(
                name: "FK_order_item_job_steps_users_AssignedUserId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropForeignKey(
                name: "FK_order_item_job_steps_users_CompletedByUserId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropTable(
                name: "order_approvals",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "order_item_job_step_logs",
                schema: "biz");

            migrationBuilder.DropIndex(
                name: "IX_order_item_job_steps_AssignedUserId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropIndex(
                name: "IX_order_item_job_steps_CompletedByUserId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropIndex(
                name: "IX_order_item_job_steps_DepartmentId_Status",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropIndex(
                name: "IX_job_types_DepartmentId",
                schema: "biz",
                table: "job_types");

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "CanReject",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "CommissionPercent",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "EarnedAmount",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "LaborCost",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "ProfitPercent",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "Progress",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "StandardMinutes",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "biz",
                table: "order_item_job_steps");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "biz",
                table: "job_types");

            migrationBuilder.DropColumn(
                name: "StandardMinutes",
                schema: "biz",
                table: "catalog_job_steps");
        }
    }
}
