using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeviceStructureWithComponentsAndSetupDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextCheckoutDate",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "Specifications",
                table: "Devices",
                type: "jsonb",
                nullable: true);

            // Mevcut kayıtları güncelle
            migrationBuilder.Sql("UPDATE \"Devices\" SET \"Specifications\" = '{}' WHERE \"Specifications\" IS NULL");

            // Şimdi NOT NULL constraint ekle
            migrationBuilder.AlterColumn<JsonDocument>(
                name: "Specifications",
                table: "Devices",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyPeriod",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyStartDate",
                table: "Devices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarrantyUnit",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specifications = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    WarrantyPeriod = table.Column<int>(type: "integer", nullable: true),
                    WarrantyUnit = table.Column<int>(type: "integer", nullable: true),
                    WarrantyStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceComponents_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SetupDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SetupId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetupDevices_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SetupDevices_Setups_SetupId",
                        column: x => x.SetupId,
                        principalTable: "Setups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SetupDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Specifications = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalDevices_SetupDevices_SetupDeviceId",
                        column: x => x.SetupDeviceId,
                        principalTable: "SetupDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceComponents_DeviceId",
                table: "DeviceComponents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceComponents_DeviceType",
                table: "DeviceComponents",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceComponents_IsDeleted",
                table: "DeviceComponents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDevices_DeviceType",
                table: "ExternalDevices",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDevices_SetupDeviceId",
                table: "ExternalDevices",
                column: "SetupDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SetupDevices_DeviceId",
                table: "SetupDevices",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SetupDevices_SetupId",
                table: "SetupDevices",
                column: "SetupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SetupDevices_SetupId_DeviceId",
                table: "SetupDevices",
                columns: new[] { "SetupId", "DeviceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceComponents");

            migrationBuilder.DropTable(
                name: "ExternalDevices");

            migrationBuilder.DropTable(
                name: "SetupDevices");

            migrationBuilder.DropColumn(
                name: "NextCheckoutDate",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WarrantyPeriod",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WarrantyStartDate",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WarrantyUnit",
                table: "Devices");
        }
    }
}
