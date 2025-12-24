using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeDeviceLocalTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataDateTime",
                table: "DeviceWeatherData",
                newName: "DeviceLocalTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceWeatherData_ThingsBoardDeviceId_DataDateTime",
                table: "DeviceWeatherData",
                newName: "IX_DeviceWeatherData_ThingsBoardDeviceId_DeviceLocalTime");

            migrationBuilder.RenameColumn(
                name: "DataDateTime",
                table: "DevicePowerData",
                newName: "DeviceLocalTime");

            migrationBuilder.RenameIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DevicePowerData",
                newName: "IX_DevicePowerData_ThingsBoardDeviceId_DeviceLocalTime_DataType");

            migrationBuilder.RenameIndex(
                name: "IX_DevicePowerData_DataDateTime",
                table: "DevicePowerData",
                newName: "IX_DevicePowerData_DeviceLocalTime");

            migrationBuilder.RenameColumn(
                name: "DataDateTime",
                table: "DeviceEnergyData",
                newName: "DeviceLocalTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_ThingsBoardDeviceId_DeviceLocalTime_DataType");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_DataDateTime",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_DeviceLocalTime");

            migrationBuilder.RenameColumn(
                name: "RequestDate",
                table: "DeviceData",
                newName: "DeviceLocalTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceData_RequestDate",
                table: "DeviceData",
                newName: "IX_DeviceData_DeviceLocalTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceData_DeviceId_RequestDate",
                table: "DeviceData",
                newName: "IX_DeviceData_DeviceId_DeviceLocalTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceData_DeviceId_DataType_RequestDate",
                table: "DeviceData",
                newName: "IX_DeviceData_DeviceId_DataType_DeviceLocalTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeviceLocalTime",
                table: "DeviceWeatherData",
                newName: "DataDateTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceWeatherData_ThingsBoardDeviceId_DeviceLocalTime",
                table: "DeviceWeatherData",
                newName: "IX_DeviceWeatherData_ThingsBoardDeviceId_DataDateTime");

            migrationBuilder.RenameColumn(
                name: "DeviceLocalTime",
                table: "DevicePowerData",
                newName: "DataDateTime");

            migrationBuilder.RenameIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DeviceLocalTime_DataType",
                table: "DevicePowerData",
                newName: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_DataType");

            migrationBuilder.RenameIndex(
                name: "IX_DevicePowerData_DeviceLocalTime",
                table: "DevicePowerData",
                newName: "IX_DevicePowerData_DataDateTime");

            migrationBuilder.RenameColumn(
                name: "DeviceLocalTime",
                table: "DeviceEnergyData",
                newName: "DataDateTime");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DeviceLocalTime_DataType",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_DeviceLocalTime",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_DataDateTime");

            migrationBuilder.RenameColumn(
                name: "DeviceLocalTime",
                table: "DeviceData",
                newName: "RequestDate");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceData_DeviceLocalTime",
                table: "DeviceData",
                newName: "IX_DeviceData_RequestDate");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceData_DeviceId_DeviceLocalTime",
                table: "DeviceData",
                newName: "IX_DeviceData_DeviceId_RequestDate");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceData_DeviceId_DataType_DeviceLocalTime",
                table: "DeviceData",
                newName: "IX_DeviceData_DeviceId_DataType_RequestDate");
        }
    }
}
