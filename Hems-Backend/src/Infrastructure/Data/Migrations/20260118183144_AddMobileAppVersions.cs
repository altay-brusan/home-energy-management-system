using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMobileAppVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobileAppVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VersionMajor = table.Column<int>(type: "integer", nullable: false),
                    VersionMinor = table.Column<int>(type: "integer", nullable: false),
                    VersionPatch = table.Column<int>(type: "integer", nullable: false),
                    BuildNumber = table.Column<int>(type: "integer", nullable: true),
                    StoreUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MinimumRequiredVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileAppVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersions_IsActive",
                table: "MobileAppVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersions_IsLatest",
                table: "MobileAppVersions",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersions_Platform",
                table: "MobileAppVersions",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersions_Platform_IsLatest",
                table: "MobileAppVersions",
                columns: new[] { "Platform", "IsLatest" });

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersions_Platform_Version",
                table: "MobileAppVersions",
                columns: new[] { "Platform", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobileAppVersions_Platform_VersionMajor_VersionMinor_Versio~",
                table: "MobileAppVersions",
                columns: new[] { "Platform", "VersionMajor", "VersionMinor", "VersionPatch" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileAppVersions");
        }
    }
}
