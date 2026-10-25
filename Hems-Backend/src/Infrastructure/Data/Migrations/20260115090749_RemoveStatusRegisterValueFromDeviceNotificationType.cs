using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStatusRegisterValueFromDeviceNotificationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Register",
                table: "DeviceNotificationTypes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DeviceNotificationTypes");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "DeviceNotificationTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Register",
                table: "DeviceNotificationTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DeviceNotificationTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "DeviceNotificationTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
