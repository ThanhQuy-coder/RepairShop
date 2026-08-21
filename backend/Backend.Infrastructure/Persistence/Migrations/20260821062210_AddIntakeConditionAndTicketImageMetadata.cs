using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeConditionAndTicketImageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "TicketImages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedByUserId",
                table: "TicketImages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ConditionNotes",
                table: "RepairTickets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskWarning",
                table: "RepairTickets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketImages_UploadedByUserId",
                table: "TicketImages",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketImages_Users_UploadedByUserId",
                table: "TicketImages",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketImages_Users_UploadedByUserId",
                table: "TicketImages");

            migrationBuilder.DropIndex(
                name: "IX_TicketImages_UploadedByUserId",
                table: "TicketImages");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "TicketImages");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "TicketImages");

            migrationBuilder.DropColumn(
                name: "ConditionNotes",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "RiskWarning",
                table: "RepairTickets");
        }
    }
}
