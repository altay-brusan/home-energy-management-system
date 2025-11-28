using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerAndWeatherTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceEnergyData_DataType",
                table: "DeviceEnergyData");

            migrationBuilder.DropIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "WeatherTemperature",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "WeatherTemperatureCondition",
                table: "DeviceEnergyData");

            migrationBuilder.RenameColumn(
                name: "PvEnergyKWh",
                table: "DeviceEnergyData",
                newName: "PvTotalEnergyKWh");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "DeviceEnergyData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "DeviceEnergyData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhaseType",
                table: "DeviceEnergyData",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SinglePhase");

            migrationBuilder.AddColumn<double>(
                name: "PvMppt1EnergyKWh",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PvMppt2EnergyKWh",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PvMppt3EnergyKWh",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PvMppt4EnergyKWh",
                table: "DeviceEnergyData",
                type: "double precision",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DevicePowerData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ThingsBoardDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhaseType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SinglePhase"),
                    PvTotalPowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    PvMppt1PowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    PvMppt2PowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    PvMppt3PowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    PvMppt4PowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridTotalPowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    LoadTotalPowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryPowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GeneratorTotalPowerW = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicePowerData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevicePowerData_Devices_ThingsBoardDeviceId",
                        column: x => x.ThingsBoardDeviceId,
                        principalTable: "Devices",
                        principalColumn: "ThingsBoardDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceWeatherData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ThingsBoardDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    TemperatureCondition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Humidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    WindSpeed = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    WindDirection = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Pressure = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    Condition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CloudCover = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    UvIndex = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceWeatherData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceWeatherData_Devices_ThingsBoardDeviceId",
                        column: x => x.ThingsBoardDeviceId,
                        principalTable: "Devices",
                        principalColumn: "ThingsBoardDeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData",
                columns: new[] { "ThingsBoardDeviceId", "DataDateTime", "DataType" });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePowerData_DataDateTime",
                table: "DevicePowerData",
                column: "DataDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId",
                table: "DevicePowerData",
                column: "ThingsBoardDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePowerData_ThingsBoardDeviceId_DataDateTime_MessageType",
                table: "DevicePowerData",
                columns: new[] { "ThingsBoardDeviceId", "DataDateTime", "MessageType" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceWeatherData_ThingsBoardDeviceId",
                table: "DeviceWeatherData",
                column: "ThingsBoardDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceWeatherData_ThingsBoardDeviceId_DataDateTime",
                table: "DeviceWeatherData",
                columns: new[] { "ThingsBoardDeviceId", "DataDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevicePowerData");

            migrationBuilder.DropTable(
                name: "DeviceWeatherData");

            migrationBuilder.DropIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "PhaseType",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "PvMppt1EnergyKWh",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "PvMppt2EnergyKWh",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "PvMppt3EnergyKWh",
                table: "DeviceEnergyData");

            migrationBuilder.DropColumn(
                name: "PvMppt4EnergyKWh",
                table: "DeviceEnergyData");

            migrationBuilder.RenameColumn(
                name: "PvTotalEnergyKWh",
                table: "DeviceEnergyData",
                newName: "PvEnergyKWh");

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

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_DataType",
                table: "DeviceEnergyData",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_ThingsBoardDeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData",
                columns: new[] { "ThingsBoardDeviceId", "DataDateTime", "DataType" },
                unique: true);
        }
    }
}
