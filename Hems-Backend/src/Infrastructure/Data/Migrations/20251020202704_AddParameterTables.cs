using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParameterTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParameterGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterGroups_ParameterGroups_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ParameterGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ElementType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoleType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parameters_ParameterGroups_ParameterGroupId",
                        column: x => x.ParameterGroupId,
                        principalTable: "ParameterGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParameterValueSettingSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterValueSettingSchemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterValueSettingSchemes_ParameterGroups_ParameterGroup~",
                        column: x => x.ParameterGroupId,
                        principalTable: "ParameterGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParameterValueSettingSchemes_Parameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "Parameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SetupDeviceParameterValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParameterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetupDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupDeviceParameterValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetupDeviceParameterValues_ParameterGroups_ParameterGroupId",
                        column: x => x.ParameterGroupId,
                        principalTable: "ParameterGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SetupDeviceParameterValues_Parameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "Parameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SetupDeviceParameterValues_SetupDevices_SetupDeviceId",
                        column: x => x.SetupDeviceId,
                        principalTable: "SetupDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterGroups_ParentId",
                table: "ParameterGroups",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterGroups_ParentId_SortOrder",
                table: "ParameterGroups",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterGroups_SortOrder",
                table: "ParameterGroups",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_ParameterGroupId",
                table: "Parameters",
                column: "ParameterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_ParameterGroupId_SortOrder",
                table: "Parameters",
                columns: new[] { "ParameterGroupId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_SortOrder",
                table: "Parameters",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterValueSettingSchemes_ModelNumber",
                table: "ParameterValueSettingSchemes",
                column: "ModelNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterValueSettingSchemes_ModelNumber_SortOrder",
                table: "ParameterValueSettingSchemes",
                columns: new[] { "ModelNumber", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterValueSettingSchemes_ParameterGroupId",
                table: "ParameterValueSettingSchemes",
                column: "ParameterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterValueSettingSchemes_ParameterId",
                table: "ParameterValueSettingSchemes",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterValueSettingSchemes_SortOrder",
                table: "ParameterValueSettingSchemes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SetupDeviceParameterValues_ParameterGroupId",
                table: "SetupDeviceParameterValues",
                column: "ParameterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SetupDeviceParameterValues_ParameterId",
                table: "SetupDeviceParameterValues",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_SetupDeviceParameterValues_SetupDeviceId",
                table: "SetupDeviceParameterValues",
                column: "SetupDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SetupDeviceParameterValues_SetupDeviceId_ParameterId",
                table: "SetupDeviceParameterValues",
                columns: new[] { "SetupDeviceId", "ParameterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParameterValueSettingSchemes");

            migrationBuilder.DropTable(
                name: "SetupDeviceParameterValues");

            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "ParameterGroups");
        }
    }
}
