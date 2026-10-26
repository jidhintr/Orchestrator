using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemsAndPriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "priorities",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priorities", x => x.Id);
                });


            migrationBuilder.InsertData(
                schema: "biz",
                table: "priorities",
                columns: new[] { "Id", "Cost", "InsertedAt", "InsertedById", "IsActive", "LastUpdatedAt", "LastUpdatedById", "Name", "Rank" },
                values: new object[,]
                {
                    { 1, 0m, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, "Normal", 1 },
                    { 2, 100m, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, "High", 2 },
                    { 3, 250m, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, "Premium", 3 },
                    { 4, 500m, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 10, 23, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ultra High", 4 }
                });


            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                schema: "biz",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "PriorityCost",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountTotal",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.RenameColumn(
                name: "DueDate",
                schema: "biz",
                table: "orders",
                newName: "CommitmentDeadline");

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    CatalogId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CatalogSku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CatalogName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CatalogDiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ClientDiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LineNet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_catalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalSchema: "biz",
                        principalTable: "catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "biz",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateTable(
                name: "order_item_job_steps",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    JobTypeId = table.Column<int>(type: "integer", nullable: false),
                    JobTypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_job_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_item_job_steps_job_types_JobTypeId",
                        column: x => x.JobTypeId,
                        principalSchema: "biz",
                        principalTable: "job_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_item_job_steps_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "biz",
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.Sql(@"UPDATE biz.orders o SET ""PriorityId"" = p.""Id"", ""PriorityCost"" = p.""Cost"" FROM biz.priorities p WHERE p.""Name"" = CASE o.""Priority"" WHEN 'UltraHigh' THEN 'Ultra High' ELSE o.""Priority"" END;
UPDATE biz.orders SET ""SubTotal"" = ""UnitPrice"" * ""Quantity"", ""DiscountTotal"" = ""UnitPrice"" * ""Quantity"" - ""QuotedTotal"", ""NetAmount"" = ""QuotedTotal"" + ""PriorityCost"";
INSERT INTO biz.order_items (""OrderId"", ""CatalogId"", ""Quantity"", ""CatalogSku"", ""CatalogName"", ""UnitPrice"", ""CatalogDiscountPercent"", ""ClientDiscountPercent"", ""LineNet"", ""InsertedAt"", ""LastUpdatedAt"", ""IsActive"")
SELECT o.""Id"", o.""CatalogId"", o.""Quantity"", c.""Sku"", c.""Name"", o.""UnitPrice"", o.""CatalogDiscountPercent"", o.""ClientDiscountPercent"", o.""QuotedTotal"", o.""InsertedAt"", o.""LastUpdatedAt"", o.""IsActive"" FROM biz.orders o JOIN biz.catalogs c ON c.""Id"" = o.""CatalogId"";
INSERT INTO biz.order_item_job_steps (""OrderItemId"", ""JobTypeId"", ""JobTypeName"", ""SortOrder"", ""Status"", ""InsertedAt"", ""LastUpdatedAt"", ""IsActive"")
SELECT i.""Id"", s.""JobTypeId"", j.""Name"", s.""Sequence"", CASE WHEN s.""ExitedAt"" IS NOT NULL THEN 'Completed' WHEN s.""EnteredAt"" IS NOT NULL THEN 'InProgress' ELSE 'Waiting' END, s.""InsertedAt"", s.""LastUpdatedAt"", s.""IsActive""
FROM biz.order_stages s JOIN biz.order_items i ON i.""OrderId"" = s.""OrderId"" JOIN biz.job_types j ON j.""Id"" = s.""JobTypeId"";");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_catalogs_CatalogId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropTable(
                name: "order_stages",
                schema: "biz");

            migrationBuilder.DropIndex(
                name: "IX_orders_CatalogId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CatalogDiscountPercent",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CatalogId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ClientDiscountPercent",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "QuotedTotal",
                schema: "biz",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_PriorityId",
                schema: "biz",
                table: "orders",
                column: "PriorityId");


            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_steps_JobTypeId",
                schema: "biz",
                table: "order_item_job_steps",
                column: "JobTypeId");


            migrationBuilder.CreateIndex(
                name: "IX_order_item_job_steps_OrderItemId_SortOrder",
                schema: "biz",
                table: "order_item_job_steps",
                columns: new[] { "OrderItemId", "SortOrder" },
                unique: true);


            migrationBuilder.CreateIndex(
                name: "IX_order_items_CatalogId",
                schema: "biz",
                table: "order_items",
                column: "CatalogId");


            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                schema: "biz",
                table: "order_items",
                column: "OrderId");


            migrationBuilder.CreateIndex(
                name: "IX_priorities_Name",
                schema: "biz",
                table: "priorities",
                column: "Name",
                unique: true);


            migrationBuilder.AddForeignKey(
                name: "FK_orders_priorities_PriorityId",
                schema: "biz",
                table: "orders",
                column: "PriorityId",
                principalSchema: "biz",
                principalTable: "priorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        
        }

        // Down keeps only the first item of each order; later items are lost.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_priorities_PriorityId",
                schema: "biz",
                table: "orders");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuotedTotal",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                schema: "biz",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CatalogId",
                schema: "biz",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CatalogDiscountPercent",
                schema: "biz",
                table: "orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ClientDiscountPercent",
                schema: "biz",
                table: "orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "biz",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "order_stages",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobTypeId = table.Column<int>(type: "integer", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
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


            migrationBuilder.Sql(@"UPDATE biz.orders o SET ""Priority"" = CASE p.""Name"" WHEN 'Ultra High' THEN 'UltraHigh' ELSE p.""Name"" END, ""QuotedTotal"" = o.""NetAmount"" - o.""PriorityCost"" FROM biz.priorities p WHERE p.""Id"" = o.""PriorityId"";
UPDATE biz.orders o SET ""CatalogId"" = i.""CatalogId"", ""Quantity"" = i.""Quantity"", ""UnitPrice"" = i.""UnitPrice"", ""CatalogDiscountPercent"" = i.""CatalogDiscountPercent"", ""ClientDiscountPercent"" = i.""ClientDiscountPercent""
FROM (SELECT DISTINCT ON (""OrderId"") * FROM biz.order_items ORDER BY ""OrderId"", ""Id"") i WHERE i.""OrderId"" = o.""Id"";
INSERT INTO biz.order_stages (""OrderId"", ""JobTypeId"", ""Sequence"", ""InsertedAt"", ""LastUpdatedAt"", ""IsActive"")
SELECT i.""OrderId"", s.""JobTypeId"", s.""SortOrder"", s.""InsertedAt"", s.""LastUpdatedAt"", s.""IsActive"" FROM biz.order_item_job_steps s JOIN (SELECT DISTINCT ON (""OrderId"") * FROM biz.order_items ORDER BY ""OrderId"", ""Id"") i ON i.""Id"" = s.""OrderItemId"";");

            migrationBuilder.DropTable(
                name: "order_item_job_steps",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "biz");

            migrationBuilder.DropIndex(
                name: "IX_orders_PriorityId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropTable(
                name: "priorities",
                schema: "biz");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PriorityCost",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DiscountTotal",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                schema: "biz",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "CommitmentDeadline",
                schema: "biz",
                table: "orders",
                newName: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CatalogId",
                schema: "biz",
                table: "orders",
                column: "CatalogId");


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
                name: "FK_orders_catalogs_CatalogId",
                schema: "biz",
                table: "orders",
                column: "CatalogId",
                principalSchema: "biz",
                principalTable: "catalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
