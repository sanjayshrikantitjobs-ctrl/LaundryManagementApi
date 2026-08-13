using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGarmentBarcodeAddCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Garments_Barcode",
                table: "Garments");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Garments");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Garments",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill existing garments to the first category (by DisplayOrder) so the
            // column can become required without losing any existing rows.
            migrationBuilder.Sql(@"
                UPDATE g
                SET g.CategoryId = (SELECT TOP 1 Id FROM ServiceCategories ORDER BY DisplayOrder)
                FROM Garments g
                WHERE g.CategoryId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Garments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Garments_CategoryId",
                table: "Garments",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Garments_ServiceCategories_CategoryId",
                table: "Garments",
                column: "CategoryId",
                principalTable: "ServiceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Garments_ServiceCategories_CategoryId",
                table: "Garments");

            migrationBuilder.DropIndex(
                name: "IX_Garments_CategoryId",
                table: "Garments");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Garments");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Garments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Garments_Barcode",
                table: "Garments",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");
        }
    }
}
