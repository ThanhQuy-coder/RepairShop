using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarrantyPackageId",
                table: "Warranties",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarrantyPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationMonths = table.Column<int>(type: "integer", nullable: false),
                    Terms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyPackages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "WarrantyPackages",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "DurationMonths", "IsActive", "Name", "Terms", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000201"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Bảo hành lỗi kỹ thuật.", 1, true, "Bảo hành cơ bản", "Bảo hành lỗi kỹ thuật.", null },
                    { new Guid("00000000-0000-0000-0000-000000000202"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Áp dụng cho linh kiện thay thế.", 3, true, "Bảo hành linh kiện 3 tháng", "Không bảo hành hư hỏng vật lý.", null },
                    { new Guid("00000000-0000-0000-0000-000000000203"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Áp dụng cho pin thay thế.", 6, true, "Bảo hành pin 6 tháng", "Không bảo hành chai pin do sử dụng.", null },
                    { new Guid("00000000-0000-0000-0000-000000000204"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Gói bảo hành dài hạn theo chính sách cửa hàng.", 12, true, "Bảo hành linh kiện 12 tháng", "Theo chính sách cửa hàng.", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_WarrantyPackageId",
                table: "Warranties",
                column: "WarrantyPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPackages_DeletedAt",
                table: "WarrantyPackages",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPackages_Name",
                table: "WarrantyPackages",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Warranties_WarrantyPackages_WarrantyPackageId",
                table: "Warranties",
                column: "WarrantyPackageId",
                principalTable: "WarrantyPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warranties_WarrantyPackages_WarrantyPackageId",
                table: "Warranties");

            migrationBuilder.DropTable(
                name: "WarrantyPackages");

            migrationBuilder.DropIndex(
                name: "IX_Warranties_WarrantyPackageId",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "WarrantyPackageId",
                table: "Warranties");
        }
    }
}
