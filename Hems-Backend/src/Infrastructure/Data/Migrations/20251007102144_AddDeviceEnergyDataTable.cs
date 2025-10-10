using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceEnergyDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceEnergyData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PvEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridImportEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridExportEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GeneratorEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryDischargeEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryChargeEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    LoadEnergyKWhToday = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    PvEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridImportEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridExportEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GeneratorEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryDischargeEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryChargeEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    LoadEnergyKWhTotal = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    PvPowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridImportPowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GridExportPowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    GeneratorPowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryDischargePowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    BatteryChargePowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    LoadPowerKw = table.Column<double>(type: "double precision", precision: 18, scale: 3, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceEnergyData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceEnergyData_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_DataDateTime",
                table: "DeviceEnergyData",
                column: "DataDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_DataType",
                table: "DeviceEnergyData",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_DeviceId",
                table: "DeviceEnergyData",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEnergyData_DeviceId_DataDateTime_DataType",
                table: "DeviceEnergyData",
                columns: new[] { "DeviceId", "DataDateTime", "DataType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceEnergyData");
        }
    }
}
