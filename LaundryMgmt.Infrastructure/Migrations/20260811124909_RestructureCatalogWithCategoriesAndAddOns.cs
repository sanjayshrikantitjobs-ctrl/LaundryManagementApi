using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestructureCatalogWithCategoriesAndAddOns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DESTRUCTIVE, INTENTIONAL: this restructures the catalog (Garment.Category
            // removed, Service gains a required CategoryId) in a way that's incompatible
            // with any existing rows, and the user explicitly approved wiping catalog +
            // order data as part of this change (dev environment only — do not run this
            // against a database with data you need). Clears every table that transitively
            // FKs to Orders/OrderItems/Garments/Services, in dependency order (children
            // before parents), before the structural changes below.
            // OrderItemAddOns doesn't exist yet — this migration creates it below, so
            // there's nothing to clear there.
            migrationBuilder.Sql("DELETE FROM [OrderGarmentImages];");
            migrationBuilder.Sql("DELETE FROM [Payments];");
            migrationBuilder.Sql("DELETE FROM [Complaints];");
            migrationBuilder.Sql("DELETE FROM [OrderStatusHistory];");
            migrationBuilder.Sql("DELETE FROM [OrderItems];");
            migrationBuilder.Sql("DELETE FROM [PickupDeliveries];");
            migrationBuilder.Sql("DELETE FROM [Invoices];");
            migrationBuilder.Sql("DELETE FROM [Orders];");
            migrationBuilder.Sql("DELETE FROM [GarmentServicePrices];");
            migrationBuilder.Sql("DELETE FROM [Garments];");
            migrationBuilder.Sql("DELETE FROM [Services];");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Garments");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Services",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsSameDay",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedTimeHours",
                table: "GarmentServicePrices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpressEtaHours",
                table: "GarmentServicePrices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpressPrice",
                table: "GarmentServicePrices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GstPercentage",
                table: "GarmentServicePrices",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "GarmentServicePrices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AddOns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconOrImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemAddOns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddOnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemAddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemAddOns_AddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "AddOns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemAddOns_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId",
                table: "Services",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AddOns_Name",
                table: "AddOns",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAddOns_AddOnId",
                table: "OrderItemAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAddOns_OrderItemId",
                table: "OrderItemAddOns",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_Name",
                table: "ServiceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_ServiceCategories_CategoryId",
                table: "Services",
                column: "CategoryId",
                principalTable: "ServiceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_ServiceCategories_CategoryId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "OrderItemAddOns");

            migrationBuilder.DropTable(
                name: "ServiceCategories");

            migrationBuilder.DropTable(
                name: "AddOns");

            migrationBuilder.DropIndex(
                name: "IX_Services_CategoryId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsSameDay",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedTimeHours",
                table: "GarmentServicePrices");

            migrationBuilder.DropColumn(
                name: "ExpressEtaHours",
                table: "GarmentServicePrices");

            migrationBuilder.DropColumn(
                name: "ExpressPrice",
                table: "GarmentServicePrices");

            migrationBuilder.DropColumn(
                name: "GstPercentage",
                table: "GarmentServicePrices");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "GarmentServicePrices");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Garments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
