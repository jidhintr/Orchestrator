using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Catalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogs",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CatalogDiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LaborCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ProfitPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_types",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LaborCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CommissionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ProfitPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CatalogId = table.Column<int>(type: "integer", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_catalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalSchema: "biz",
                        principalTable: "catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_job_steps",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CatalogId = table.Column<int>(type: "integer", nullable: false),
                    JobTypeId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_job_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_job_steps_catalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalSchema: "biz",
                        principalTable: "catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalog_job_steps_job_types_JobTypeId",
                        column: x => x.JobTypeId,
                        principalSchema: "biz",
                        principalTable: "job_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "job_types",
                columns: new[] { "Id", "CommissionPercent", "Description", "InsertedAt", "InsertedById", "IsActive", "LaborCost", "LastUpdatedAt", "LastUpdatedById", "Name", "ProfitPercent" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cutting", null },
                    { 2, null, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Laminating", null },
                    { 3, null, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Quality Assurance", null },
                    { 4, null, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Delivery", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_job_steps_CatalogId",
                schema: "biz",
                table: "catalog_job_steps",
                column: "CatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_job_steps_JobTypeId",
                schema: "biz",
                table: "catalog_job_steps",
                column: "JobTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogs_Sku",
                schema: "biz",
                table: "catalogs",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_types_Name",
                schema: "biz",
                table: "job_types",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_CatalogId",
                schema: "biz",
                table: "orders",
                column: "CatalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_job_steps",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "job_types",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "catalogs",
                schema: "biz");
        }
    }
}
