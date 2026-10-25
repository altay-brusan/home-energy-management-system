using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceNotificationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Register = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNotificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetupDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceNotificationTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AdditionalData = table.Column<string>(type: "text", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceNotifications_DeviceNotificationTypes_DeviceNotificat~",
                        column: x => x.DeviceNotificationTypeId,
                        principalTable: "DeviceNotificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceNotifications_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceNotifications_SetupDevices_SetupDeviceId",
                        column: x => x.SetupDeviceId,
                        principalTable: "SetupDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceNotifications_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotifications_DeviceId",
                table: "DeviceNotifications",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotifications_DeviceNotificationTypeId",
                table: "DeviceNotifications",
                column: "DeviceNotificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotifications_SetupDeviceId",
                table: "DeviceNotifications",
                column: "SetupDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotifications_SiteId",
                table: "DeviceNotifications",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotificationTypes_Category",
                table: "DeviceNotificationTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotificationTypes_Code",
                table: "DeviceNotificationTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotificationTypes_IsActive",
                table: "DeviceNotificationTypes",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceNotifications");

            migrationBuilder.DropTable(
                name: "DeviceNotificationTypes");
        }
    }
}
