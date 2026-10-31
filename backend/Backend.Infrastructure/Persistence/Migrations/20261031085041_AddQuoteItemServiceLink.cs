using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteItemServiceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "QuoteItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ServiceId",
                table: "QuoteItems",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Services_ServiceId",
                table: "QuoteItems",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Services_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "QuoteItems");
        }
    }
}
