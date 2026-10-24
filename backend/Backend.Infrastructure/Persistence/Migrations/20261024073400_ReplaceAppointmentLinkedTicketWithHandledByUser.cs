using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAppointmentLinkedTicketWithHandledByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_RepairTickets_LinkedTicketId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_LinkedTicketId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LinkedTicketId",
                table: "Appointments");

            migrationBuilder.AddColumn<Guid>(
                name: "HandledByUserId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_HandledByUserId",
                table: "Appointments",
                column: "HandledByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_HandledByUserId",
                table: "Appointments",
                column: "HandledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_HandledByUserId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_HandledByUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "HandledByUserId",
                table: "Appointments");

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedTicketId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LinkedTicketId",
                table: "Appointments",
                column: "LinkedTicketId",
                unique: true,
                filter: "\"LinkedTicketId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_RepairTickets_LinkedTicketId",
                table: "Appointments",
                column: "LinkedTicketId",
                principalTable: "RepairTickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
