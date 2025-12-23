using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeverityToDeviceNotificationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "DeviceNotificationTypes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "INFO");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotificationTypes_Severity",
                table: "DeviceNotificationTypes",
                column: "Severity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceNotificationTypes_Severity",
                table: "DeviceNotificationTypes");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "DeviceNotificationTypes");
        }
    }
}
