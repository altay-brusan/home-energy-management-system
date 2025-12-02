using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDevicePowerDataSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_MessageType",
                table: "DevicePowerData");

            migrationBuilder.RenameColumn(
                name: "Kind",
                table: "DevicePowerData",
                newName: "DataType");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DevicePowerData",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DevicePowerData",
                columns: new[] { "ThingsBoardDeviceId", "DataDateTime", "DataType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DevicePowerData");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DevicePowerData");

            migrationBuilder.RenameColumn(
                name: "DataType",
                table: "DevicePowerData",
                newName: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_MessageType",
                table: "DevicePowerData",
                columns: new[] { "ThingsBoardDeviceId", "DataDateTime", "MessageType" });
        }
    }
}
