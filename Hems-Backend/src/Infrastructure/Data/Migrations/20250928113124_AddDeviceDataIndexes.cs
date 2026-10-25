using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceDataIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "DeviceData",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "DeviceData",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "DeviceData",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceData_DataType",
                table: "DeviceData",
                column: "DataType");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceData_DeviceId_DataType_RequestDate",
                table: "DeviceData",
                columns: new[] { "DeviceId", "DataType", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceData_DeviceId_RequestDate",
                table: "DeviceData",
                columns: new[] { "DeviceId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceData_RequestDate",
                table: "DeviceData",
                column: "RequestDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceData_DataType",
                table: "DeviceData");

            migrationBuilder.DropIndex(
                name: "IX_DeviceData_DeviceId_DataType_RequestDate",
                table: "DeviceData");

            migrationBuilder.DropIndex(
                name: "IX_DeviceData_DeviceId_RequestDate",
                table: "DeviceData");

            migrationBuilder.DropIndex(
                name: "IX_DeviceData_RequestDate",
                table: "DeviceData");

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                table: "DeviceData",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "DeviceData",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "DeviceData",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
        }
    }
}
