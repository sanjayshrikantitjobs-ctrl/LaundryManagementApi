using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupDeliveryAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Otp",
                table: "PickupDeliveries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "PickupDeliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupDeliveries_AddressId",
                table: "PickupDeliveries",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupDeliveries_CustomerAddresses_AddressId",
                table: "PickupDeliveries",
                column: "AddressId",
                principalTable: "CustomerAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupDeliveries_CustomerAddresses_AddressId",
                table: "PickupDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_PickupDeliveries_AddressId",
                table: "PickupDeliveries");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "PickupDeliveries");

            migrationBuilder.AlterColumn<string>(
                name: "Otp",
                table: "PickupDeliveries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
