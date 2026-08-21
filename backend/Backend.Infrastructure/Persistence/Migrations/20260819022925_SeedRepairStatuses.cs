using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepairShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedRepairStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RepairStatuses",
                columns: new[] { "Id", "Code", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "CHECKED_IN", "Đã tiếp nhận", 1 },
                    { 2, "ASSIGNED", "Đã phân công", 2 },
                    { 3, "DIAGNOSING", "Đang chẩn đoán", 3 },
                    { 4, "WAITING_APPROVAL", "Chờ khách hàng duyệt", 4 },
                    { 5, "ON_HOLD", "Tạm dừng", 5 },
                    { 6, "WAITING_PARTS", "Chờ linh kiện", 6 },
                    { 7, "IN_REPAIR", "Đang sửa chữa", 7 },
                    { 8, "QA_TESTING", "Đang kiểm tra chất lượng", 8 },
                    { 9, "READY_FOR_PICKUP", "Sẵn sàng giao khách", 9 },
                    { 10, "DELIVERED", "Đã giao khách", 10 },
                    { 11, "CLOSED_REJECTED", "Đã đóng - từ chối sửa", 11 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "RepairStatuses",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
