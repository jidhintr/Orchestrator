using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalChains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_chains",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CatalogId = table.Column<int>(type: "integer", nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_chains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_chains_catalogs_CatalogId",
                        column: x => x.CatalogId,
                        principalSchema: "biz",
                        principalTable: "catalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_chain_stages",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApprovalChainId = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_chain_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_chain_stages_approval_chains_ApprovalChainId",
                        column: x => x.ApprovalChainId,
                        principalSchema: "biz",
                        principalTable: "approval_chains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_approval_chain_stages_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "biz",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "biz",
                table: "approval_chains",
                columns: new[] { "Id", "CatalogId", "Description", "InsertedAt", "InsertedById", "IsActive", "IsDefault", "LastUpdatedAt", "LastUpdatedById", "Name" },
                values: new object[] { 1, null, null, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "Default" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_chain_stages_ApprovalChainId",
                schema: "biz",
                table: "approval_chain_stages",
                column: "ApprovalChainId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chain_stages_DepartmentId",
                schema: "biz",
                table: "approval_chain_stages",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_approval_chains_CatalogId",
                schema: "biz",
                table: "approval_chains",
                column: "CatalogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_approval_chains_IsDefault",
                schema: "biz",
                table: "approval_chains",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_chain_stages",
                schema: "biz");

            migrationBuilder.DropTable(
                name: "approval_chains",
                schema: "biz");
        }
    }
}
