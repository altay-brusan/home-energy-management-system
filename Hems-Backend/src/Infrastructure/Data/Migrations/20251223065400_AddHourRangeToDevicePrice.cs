using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHourRangeToDevicePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BeginHour",
                table: "DevicePrices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndHour",
                table: "DevicePrices",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeginHour",
                table: "DevicePrices");

            migrationBuilder.DropColumn(
                name: "EndHour",
                table: "DevicePrices");
        }
    }
}
