using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisAndQuoteWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecommendedRepair",
                table: "RepairTickets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredPartsNote",
                table: "RepairTickets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "RepairTickets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "QuoteItems",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "QuoteItems",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "QuoteItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PartId",
                table: "QuoteItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_PartId",
                table: "QuoteItems",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Parts_PartId",
                table: "QuoteItems",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Parts_PartId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_PartId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "RecommendedRepair",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "RequiredPartsNote",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "RootCause",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "PartId",
                table: "QuoteItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "QuoteItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "QuoteItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
