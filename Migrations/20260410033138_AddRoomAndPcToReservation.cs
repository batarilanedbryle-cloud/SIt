using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Batarilan_Exercise1.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomAndPcToReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PcNumber",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PcNumber",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Room",
                table: "Reservations");
        }
    }
}
