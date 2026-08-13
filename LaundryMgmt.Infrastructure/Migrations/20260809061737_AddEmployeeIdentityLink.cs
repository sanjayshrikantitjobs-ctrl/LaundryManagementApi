using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaundryMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeIdentityLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Shift",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Employees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityUserId",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IdentityUserId",
                table: "Employees",
                column: "IdentityUserId",
                unique: true,
                filter: "[IdentityUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_IdentityUserId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "Shift",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
