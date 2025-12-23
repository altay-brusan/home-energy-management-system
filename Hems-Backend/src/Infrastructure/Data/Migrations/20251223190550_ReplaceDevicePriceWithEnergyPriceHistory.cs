using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDevicePriceWithEnergyPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevicePrices");

            // WorkMode and Visible columns already exist in the database
            // They were added manually or in a previous migration

            migrationBuilder.CreateTable(
                name: "EnergyPriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "kWh"),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "EUR"),
                    SellPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    BuyPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    BeginHour = table.Column<int>(type: "integer", nullable: true),
                    EndHour = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnergyPriceHistory_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyPriceHistory_DeviceId",
                table: "EnergyPriceHistory",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_EnergyPriceHistory_DeviceId_IsActive",
                table: "EnergyPriceHistory",
                columns: new[] { "DeviceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyPriceHistory_IsActive",
                table: "EnergyPriceHistory",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnergyPriceHistory");

            // WorkMode and Visible columns are not dropped as they exist independently

            migrationBuilder.CreateTable(
                name: "DevicePrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeginHour = table.Column<int>(type: "integer", nullable: true),
                    BuyPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "EUR"),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndHour = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    SellPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "kWh")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevicePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevicePrices_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePrices_DeviceId",
                table: "DevicePrices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePrices_DeviceId_EffectiveFrom",
                table: "DevicePrices",
                columns: new[] { "DeviceId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_DevicePrices_EffectiveFrom",
                table: "DevicePrices",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_DevicePrices_IsActive",
                table: "DevicePrices",
                column: "IsActive");
        }
    }
}
