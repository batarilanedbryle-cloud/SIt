using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Batarilan_Exercise1.Migrations
{
    /// <inheritdoc />
    public partial class FixSitInSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeOut",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeOut",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }
    }
}
