using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BetterFurniture.Migrations
{
    public partial class addCustomerInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerAge",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerDOB",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CustomerFullName",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerAge",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerDOB",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerFullName",
                table: "AspNetUsers");
        }
    }
}
