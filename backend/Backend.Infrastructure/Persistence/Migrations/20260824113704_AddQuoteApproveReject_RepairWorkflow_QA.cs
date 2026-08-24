using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteApproveReject_RepairWorkflow_QA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPriceAtUse",
                table: "TicketParts",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "RepairTickets",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "Parts",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Parts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "cái",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Parts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Parts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_TicketParts_PartId",
                table: "TicketParts",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_Sku",
                table: "Parts",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_PartId",
                table: "Inventories",
                column: "PartId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Parts_PartId",
                table: "Inventories",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketParts_Parts_PartId",
                table: "TicketParts",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Parts_PartId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketParts_Parts_PartId",
                table: "TicketParts");

            migrationBuilder.DropIndex(
                name: "IX_TicketParts_PartId",
                table: "TicketParts");

            migrationBuilder.DropIndex(
                name: "IX_Parts_Sku",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_PartId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "RepairTickets");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPriceAtUse",
                table: "TicketParts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "Parts",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Parts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "cái");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Parts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Parts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
