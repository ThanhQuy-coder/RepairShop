using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepairShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentAndTimeSlotConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeSlotConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    SlotStart = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    SlotEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlotConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssueDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_RepairTickets_LinkedTicketId",
                        column: x => x.LinkedTicketId,
                        principalTable: "RepairTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_TimeSlotConfigs_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlotConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TimeSlotConfigs",
                columns: new[] { "Id", "DayOfWeek", "IsActive", "MaxCapacity", "SlotEnd", "SlotStart" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), null, true, 5, new TimeOnly(10, 0, 0), new TimeOnly(9, 0, 0) },
                    { new Guid("30000000-0000-0000-0000-000000000002"), null, true, 5, new TimeOnly(11, 0, 0), new TimeOnly(10, 0, 0) },
                    { new Guid("30000000-0000-0000-0000-000000000003"), null, true, 5, new TimeOnly(15, 0, 0), new TimeOnly(14, 0, 0) },
                    { new Guid("30000000-0000-0000-0000-000000000004"), null, true, 3, new TimeOnly(16, 0, 0), new TimeOnly(15, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_AppointmentDate_TimeSlotId",
                table: "Appointments",
                columns: new[] { "AppointmentDate", "TimeSlotId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ConfirmedByUserId",
                table: "Appointments",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CustomerId",
                table: "Appointments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_LinkedTicketId",
                table: "Appointments",
                column: "LinkedTicketId",
                unique: true,
                filter: "\"LinkedTicketId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Phone",
                table: "Appointments",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TimeSlotId",
                table: "Appointments",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotConfigs_DayOfWeek_IsActive",
                table: "TimeSlotConfigs",
                columns: new[] { "DayOfWeek", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "TimeSlotConfigs");
        }
    }
}
