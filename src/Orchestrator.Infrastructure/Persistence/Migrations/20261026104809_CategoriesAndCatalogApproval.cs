using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoriesAndCatalogApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "biz",
                table: "catalogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                schema: "biz",
                table: "catalogs",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE biz.catalogs SET ""ApprovedAt"" = now();");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailableFrom",
                schema: "biz",
                table: "catalogs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailableTo",
                schema: "biz",
                table: "catalogs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BasedOnCatalogId",
                schema: "biz",
                table: "catalogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                schema: "biz",
                table: "catalogs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "biz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, collation: "case_insensitive"),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InsertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InsertedById = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedById = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogs_ApprovedByUserId",
                schema: "biz",
                table: "catalogs",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogs_BasedOnCatalogId",
                schema: "biz",
                table: "catalogs",
                column: "BasedOnCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogs_CategoryId",
                schema: "biz",
                table: "catalogs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                schema: "biz",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogs_catalogs_BasedOnCatalogId",
                schema: "biz",
                table: "catalogs",
                column: "BasedOnCatalogId",
                principalSchema: "biz",
                principalTable: "catalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogs_categories_CategoryId",
                schema: "biz",
                table: "catalogs",
                column: "CategoryId",
                principalSchema: "biz",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogs_users_ApprovedByUserId",
                schema: "biz",
                table: "catalogs",
                column: "ApprovedByUserId",
                principalSchema: "biz",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalogs_catalogs_BasedOnCatalogId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropForeignKey(
                name: "FK_catalogs_categories_CategoryId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropForeignKey(
                name: "FK_catalogs_users_ApprovedByUserId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "biz");

            migrationBuilder.DropIndex(
                name: "IX_catalogs_ApprovedByUserId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropIndex(
                name: "IX_catalogs_BasedOnCatalogId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropIndex(
                name: "IX_catalogs_CategoryId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "AvailableTo",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "BasedOnCatalogId",
                schema: "biz",
                table: "catalogs");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "biz",
                table: "catalogs");
        }
    }
}
