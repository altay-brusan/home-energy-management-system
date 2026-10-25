using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceGuidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BMSCardGuid",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "BatteryGuid",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "InvertorGuid",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "MPPSGuid",
                table: "Devices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BMSCardGuid",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatteryGuid",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvertorGuid",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MPPSGuid",
                table: "Devices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
