using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThingsBoardDeviceIdToEnergyAndData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceData_Devices_DeviceId",
                table: "DeviceData");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceEnergyData_Devices_DeviceId",
                table: "DeviceEnergyData");

            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "DeviceEnergyData",
                newName: "ThingsBoardDeviceId");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_DeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_DeviceId",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_ThingsBoardDeviceId");

            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "DeviceData",
                newName: "ThingsBoardDeviceId");

            // Populate ThingsBoardDeviceId with Device.Id for existing devices where ThingsBoardDeviceId is empty
            migrationBuilder.Sql(@"
                UPDATE ""Devices"" 
                SET ""ThingsBoardDeviceId"" = ""Id"" 
                WHERE ""ThingsBoardDeviceId"" = '00000000-0000-0000-0000-000000000000'::uuid 
                   OR ""ThingsBoardDeviceId"" IS NULL;
            ");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Devices_ThingsBoardDeviceId",
                table: "Devices",
                column: "ThingsBoardDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceData_Devices_ThingsBoardDeviceId",
                table: "DeviceData",
                column: "ThingsBoardDeviceId",
                principalTable: "Devices",
                principalColumn: "ThingsBoardDeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceEnergyData_Devices_ThingsBoardDeviceId",
                table: "DeviceEnergyData",
                column: "ThingsBoardDeviceId",
                principalTable: "Devices",
                principalColumn: "ThingsBoardDeviceId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceData_Devices_ThingsBoardDeviceId",
                table: "DeviceData");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceEnergyData_Devices_ThingsBoardDeviceId",
                table: "DeviceEnergyData");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Devices_ThingsBoardDeviceId",
                table: "Devices");

            migrationBuilder.RenameColumn(
                name: "ThingsBoardDeviceId",
                table: "DeviceEnergyData",
                newName: "DeviceId");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_DeviceId_DataDateTime_DataType");

            migrationBuilder.RenameIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId",
                table: "DeviceEnergyData",
                newName: "IX_DeviceEnergyData_DeviceId");

            migrationBuilder.RenameColumn(
                name: "ThingsBoardDeviceId",
                table: "DeviceData",
                newName: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceData_Devices_DeviceId",
                table: "DeviceData",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceEnergyData_Devices_DeviceId",
                table: "DeviceEnergyData",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
