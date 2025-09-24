using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixOneToOneRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Setups_SiteId",
                table: "Setups");

            migrationBuilder.RenameIndex(
                name: "IX_SetupDevices_SetupId",
                table: "SetupDevices",
                newName: "IX_SetupDevices_SetupId_Unique");

            migrationBuilder.CreateIndex(
                name: "IX_Setups_SiteId_Unique",
                table: "Setups",
                column: "SiteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Setups_SiteId_Unique",
                table: "Setups");

            migrationBuilder.RenameIndex(
                name: "IX_SetupDevices_SetupId_Unique",
                table: "SetupDevices",
                newName: "IX_SetupDevices_SetupId");

            migrationBuilder.CreateIndex(
                name: "IX_Setups_SiteId",
                table: "Setups",
                column: "SiteId");
        }
    }
}
