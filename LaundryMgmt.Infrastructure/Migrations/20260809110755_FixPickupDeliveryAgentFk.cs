using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPickupDeliveryAgentFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupDeliveries_Employees_DeliveryBoyId",
                table: "PickupDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_PickupDeliveries_DeliveryBoyId",
                table: "PickupDeliveries");

            migrationBuilder.DropColumn(
                name: "DeliveryBoyId",
                table: "PickupDeliveries");

            migrationBuilder.CreateIndex(
                name: "IX_PickupDeliveries_DeliveryBoyEmployeeId",
                table: "PickupDeliveries",
                column: "DeliveryBoyEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupDeliveries_Employees_DeliveryBoyEmployeeId",
                table: "PickupDeliveries",
                column: "DeliveryBoyEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupDeliveries_Employees_DeliveryBoyEmployeeId",
                table: "PickupDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_PickupDeliveries_DeliveryBoyEmployeeId",
                table: "PickupDeliveries");

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryBoyId",
                table: "PickupDeliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupDeliveries_DeliveryBoyId",
                table: "PickupDeliveries",
                column: "DeliveryBoyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupDeliveries_Employees_DeliveryBoyId",
                table: "PickupDeliveries",
                column: "DeliveryBoyId",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
