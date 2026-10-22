using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Orders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                schema: "biz",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "biz",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReturnedToDepartmentId",
                schema: "biz",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "biz",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                schema: "biz",
                table: "clients",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_event_history",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    FromValue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToValue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_event_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_event_history_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "biz",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_event_history_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalSchema: "biz",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_stages",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    JobTypeId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_stages_job_types_JobTypeId",
                        column: x => x.JobTypeId,
                        principalSchema: "biz",
                        principalTable: "job_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_stages_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "biz",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "departments",
                columns: new[] { "Id", "Code", "Description", "InsertedAt", "InsertedById", "IsActive", "IsSystem", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[] { 8, "ESTIMATION", null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Estimation" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_ClientId",
                schema: "biz",
                table: "orders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_ReturnedToDepartmentId",
                schema: "biz",
                table: "orders",
                column: "ReturnedToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_clients_UserId",
                schema: "biz",
                table: "clients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_event_history_ActorUserId",
                schema: "biz",
                table: "order_event_history",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_order_event_history_OrderId",
                schema: "biz",
                table: "order_event_history",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_stages_JobTypeId",
                schema: "biz",
                table: "order_stages",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_order_stages_OrderId_Sequence",
                schema: "biz",
                table: "order_stages",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_clients_users_UserId",
                schema: "biz",
                table: "clients",
                column: "UserId",
                principalSchema: "biz",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_clients_ClientId",
                schema: "biz",
                table: "orders",
                column: "ClientId",
                principalSchema: "biz",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_departments_ReturnedToDepartmentId",
                schema: "biz",
                table: "orders",
                column: "ReturnedToDepartmentId",
                principalSchema: "biz",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clients_users_UserId",
                schema: "biz",
                table: "clients");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_clients_ClientId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_departments_ReturnedToDepartmentId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropTable(
                name: "order_event_history",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "order_stages",
                schema: "biz");

            migrationBuilder.DropIndex(
                name: "IX_orders_ClientId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_ReturnedToDepartmentId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_clients_UserId",
                schema: "biz",
                table: "clients");

            migrationBuilder.DeleteData(
                schema: "biz",
                table: "departments",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReturnedToDepartmentId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "biz",
                table: "clients");
        }
    }
}
