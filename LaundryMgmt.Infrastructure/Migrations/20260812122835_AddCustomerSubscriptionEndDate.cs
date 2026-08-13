using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSubscriptionEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ServiceCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Promotions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "PickupDeliveries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "OrderGarmentImages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Machines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "InventoryItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Garments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "CustomerSubscriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "CustomerSubscriptions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "CustomerAddresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "ContactMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "AddOns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "PickupDeliveries");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "OrderGarmentImages");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Garments");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "CustomerSubscriptions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "CustomerSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "AddOns");
        }
    }
}
