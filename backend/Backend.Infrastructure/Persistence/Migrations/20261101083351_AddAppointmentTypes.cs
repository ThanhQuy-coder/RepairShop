using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentTicketId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Appointments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Repair");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ParentTicketId",
                table: "Appointments",
                column: "ParentTicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_RepairTickets_ParentTicketId",
                table: "Appointments",
                column: "ParentTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_RepairTickets_ParentTicketId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ParentTicketId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ParentTicketId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Appointments");
        }
    }
}
