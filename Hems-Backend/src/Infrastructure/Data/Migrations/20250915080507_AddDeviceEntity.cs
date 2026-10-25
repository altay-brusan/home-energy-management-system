using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TypeNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BatteryGuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BMSCardGuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvertorGuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MPPSGuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QRCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    InstallationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CalibrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_IsDeleted",
                table: "Devices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Manufacturer",
                table: "Devices",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_QRCode",
                table: "Devices",
                column: "QRCode");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_SerialNumber",
                table: "Devices",
                column: "SerialNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
