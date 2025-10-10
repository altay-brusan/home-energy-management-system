using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateDeviceEnergyDataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryChargeEnergyKWhToday",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "BatteryChargeEnergyKWhTotal",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "BatteryChargePowerKw",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "BatteryDischargeEnergyKWhToday",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "BatteryDischargeEnergyKWhTotal",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "BatteryDischargePowerKw",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GeneratorEnergyKWhToday",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GeneratorEnergyKWhTotal",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GeneratorPowerKw",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GridExportEnergyKWhToday",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GridExportEnergyKWhTotal",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GridExportPowerKw",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GridImportEnergyKWhToday",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "GridImportEnergyKWhTotal",
                table: "DeviceEnergyData");

            migrationBuilder.RenameColumn(
                name: "PvPowerKw",
                table: "DeviceEnergyData",
                newName: "PvEnergyKWh");

            migrationBuilder.RenameColumn(
                name: "PvEnergyKWhTotal",
                table: "DeviceEnergyData",
                newName: "LoadEnergyKWh");

            migrationBuilder.RenameColumn(
                name: "PvEnergyKWhToday",
                table: "DeviceEnergyData",
                newName: "GridImportEnergyKWh");

            migrationBuilder.RenameColumn(
                name: "LoadPowerKw",
                table: "DeviceEnergyData",
                newName: "GridExportEnergyKWh");

            migrationBuilder.RenameColumn(
                name: "LoadEnergyKWhTotal",
                table: "DeviceEnergyData",
                newName: "GeneratorEnergyKWh");

            migrationBuilder.RenameColumn(
                name: "LoadEnergyKWhToday",
                table: "DeviceEnergyData",
                newName: "BatteryDischargeEnergyKWh");

            migrationBuilder.RenameColumn(
                name: "GridImportPowerKw",
                table: "DeviceEnergyData",
                newName: "BatteryChargeEnergyKWh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PvEnergyKWh",
                table: "DeviceEnergyData",
                newName: "PvPowerKw");

            migrationBuilder.RenameColumn(
                name: "LoadEnergyKWh",
                table: "DeviceEnergyData",
                newName: "PvEnergyKWhTotal");

            migrationBuilder.RenameColumn(
                name: "GridImportEnergyKWh",
                table: "DeviceEnergyData",
                newName: "PvEnergyKWhToday");

            migrationBuilder.RenameColumn(
                name: "GridExportEnergyKWh",
                table: "DeviceEnergyData",
                newName: "LoadPowerKw");

            migrationBuilder.RenameColumn(
                name: "GeneratorEnergyKWh",
                table: "DeviceEnergyData",
                newName: "LoadEnergyKWhTotal");

            migrationBuilder.RenameColumn(
                name: "BatteryDischargeEnergyKWh",
                table: "DeviceEnergyData",
                newName: "LoadEnergyKWhToday");

            migrationBuilder.RenameColumn(
                name: "BatteryChargeEnergyKWh",
                table: "DeviceEnergyData",
                newName: "GridImportPowerKw");

            migrationBuilder.AddColumn<double>(
                name: "BatteryChargeEnergyKWhToday",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BatteryChargeEnergyKWhTotal",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BatteryChargePowerKw",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BatteryDischargeEnergyKWhToday",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BatteryDischargeEnergyKWhTotal",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BatteryDischargePowerKw",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeneratorEnergyKWhToday",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeneratorEnergyKWhTotal",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GeneratorPowerKw",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GridExportEnergyKWhToday",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GridExportEnergyKWhTotal",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GridExportPowerKw",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GridImportEnergyKWhToday",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "GridImportEnergyKWhTotal",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);
        }
    }
}
