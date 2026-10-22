using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchestrator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                schema: "biz",
                table: "orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "biz",
                table: "orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                schema: "biz",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "QuotedTotal",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                schema: "biz",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                schema: "biz",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogDiscountPercent",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ClientDiscountPercent",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DueDate",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "QuotedTotal",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Reference",
                schema: "biz",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                schema: "biz",
                table: "orders");
        }
    }
}
