using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedServicesArticlesAndDefaultAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "BasePrice", "CreatedAt", "DeletedAt", "Description", "DeviceType", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), 350000m, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), null, "Thay pin chính hãng/OEM cho các dòng điện thoại phổ biến, bảo hành pin 6 tháng.", "Phone", true, "Thay pin điện thoại", null },
                    { new Guid("00000000-0000-0000-0000-000000000102"), 800000m, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), null, "Thay màn hình mới cho máy bị vỡ, sọc, cảm ứng liệt. Giá tùy dòng máy.", "Phone", true, "Thay màn hình điện thoại", null },
                    { new Guid("00000000-0000-0000-0000-000000000103"), 500000m, new DateTime(2026, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ép lại kính nứt vỡ mà không cần thay nguyên cụm màn hình, tiết kiệm chi phí.", "Phone", true, "Ép kính, thay mặt kính điện thoại", null },
                    { new Guid("00000000-0000-0000-0000-000000000104"), 250000m, new DateTime(2026, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), null, "Vệ sinh bụi bẩn bên trong, tra keo tản nhiệt mới giúp máy mát hơn, giảm tiếng quạt.", "Laptop", true, "Vệ sinh máy, tra keo tản nhiệt laptop", null },
                    { new Guid("00000000-0000-0000-0000-000000000105"), 450000m, new DateTime(2026, 1, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, "Thay bàn phím mới cho máy bị liệt phím, vào nước, mất chữ.", "Laptop", true, "Thay bàn phím laptop", null },
                    { new Guid("00000000-0000-0000-0000-000000000106"), 900000m, new DateTime(2026, 1, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, "Thay pin chính hãng cho laptop bị chai pin, sụt dung lượng nhanh.", "Laptop", true, "Thay pin laptop", null },
                    { new Guid("00000000-0000-0000-0000-000000000107"), 200000m, new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kiểm tra, vệ sinh mainboard cho thiết bị điện tử bị vào nước, chập cháy nhẹ.", "Electronics", true, "Kiểm tra và vệ sinh mainboard", null },
                    { new Guid("00000000-0000-0000-0000-000000000108"), 150000m, new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cài lại hệ điều hành, phần mềm cần thiết, quét và diệt virus. Áp dụng cho mọi loại thiết bị.", null, true, "Cài đặt phần mềm, xử lý virus", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "PasswordHash", "Phone", "RoleId", "UpdatedAt" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@repairshop.local", "Quản trị viên hệ thống", true, "$2b$11$zCBDFaE.ComYvdPfXsGZDesI3XTP7NdwwfdM321U3pv1tMUuD0Gwy", null, 1, null });

            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAt", "DeletedAt", "ImageUrl", "IsPublished", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000201"), new Guid("11111111-1111-1111-1111-111111111111"), "Pin sụt nhanh dù không dùng nhiều ứng dụng nặng, máy nóng bất thường khi sạc, tự động tắt nguồn dù pin còn phần trăm, sạc đầy rất lâu hoặc pin phồng lên là những dấu hiệu rõ ràng nhất cho thấy pin đã chai và cần thay mới. Việc thay pin sớm không chỉ giúp máy hoạt động ổn định hơn mà còn tránh được rủi ro cháy nổ do pin phồng.", new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "5 dấu hiệu cho thấy pin điện thoại của bạn cần được thay thế", null },
                    { new Guid("00000000-0000-0000-0000-000000000202"), new Guid("11111111-1111-1111-1111-111111111111"), "Vệ sinh khe tản nhiệt định kỳ, tránh để laptop trên bề mặt mềm gây bí nguồn, không sạc pin qua đêm liên tục và hạn chế di chuyển máy khi đang hoạt động là những thói quen đơn giản giúp laptop bền hơn. Ngoài ra, nên tra keo tản nhiệt định kỳ 12-18 tháng/lần để tránh tình trạng máy nóng và giảm hiệu năng theo thời gian.", new DateTime(2026, 1, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Hướng dẫn bảo quản laptop đúng cách để tăng tuổi thọ", null },
                    { new Guid("00000000-0000-0000-0000-000000000203"), new Guid("11111111-1111-1111-1111-111111111111"), "Tự tháo máy khi không có đủ dụng cụ chuyên dụng dễ làm hỏng các chân kết nối nhỏ, gãy cáp màn hình hoặc làm mất ốc vít. Ngoài ra, việc thao tác sai cách với pin lithium có thể gây cháy nổ nguy hiểm. Mang thiết bị đến kỹ thuật viên có kinh nghiệm giúp chẩn đoán đúng lỗi và tránh phát sinh thêm hư hỏng không đáng có.", new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Vì sao không nên tự sửa chữa thiết bị điện tử tại nhà", null },
                    { new Guid("00000000-0000-0000-0000-000000000204"), new Guid("11111111-1111-1111-1111-111111111111"), "Khi mang thiết bị đến, nhân viên sẽ ghi nhận tình trạng máy, chụp ảnh hiện trạng và cấp mã phiếu để bạn tra cứu tiến độ. Sau khi kỹ thuật viên chẩn đoán xong, cửa hàng sẽ gửi báo giá để bạn xác nhận trước khi tiến hành sửa chữa. Toàn bộ quá trình đều minh bạch và có thể theo dõi trực tuyến bằng mã phiếu, không phát sinh chi phí ngoài báo giá.", new DateTime(2026, 1, 18, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Quy trình tiếp nhận và sửa chữa tại RepairShop", null }
                });

            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[] { "Id", "AuthorId", "Content", "CreatedAt", "DeletedAt", "ImageUrl", "Title", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000205"), new Guid("11111111-1111-1111-1111-111111111111"), "Bài viết đang soạn thảo, sẽ công bố chi tiết dịch vụ nhận và trả thiết bị tận nơi trong khu vực nội thành. Nội dung sẽ được cập nhật trước khi xuất bản chính thức.", new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "[Nháp] Sắp ra mắt dịch vụ nhận sửa chữa tận nơi", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"));

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"));

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"));

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"));

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000101"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000102"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"));

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
