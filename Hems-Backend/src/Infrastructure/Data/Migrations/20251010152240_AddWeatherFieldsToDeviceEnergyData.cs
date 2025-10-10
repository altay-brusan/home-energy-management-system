using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherFieldsToDeviceEnergyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WeatherTemperature",
                table: "DeviceEnergyData",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeatherTemperatureCondition",
                table: "DeviceEnergyData",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeatherTemperature",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "WeatherTemperatureCondition",
                table: "DeviceEnergyData");
        }
    }
}
