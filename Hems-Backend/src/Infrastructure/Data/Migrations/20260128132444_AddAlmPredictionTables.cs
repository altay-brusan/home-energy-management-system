using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hems_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlmPredictionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlmPredictionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThingsBoardDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlmJobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModelType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    DataStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataEndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlmPredictionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlmPredictionJobs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlmPredictionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlmPredictionJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<int>(type: "integer", nullable: false),
                    PredictedTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PredictedValue = table.Column<double>(type: "double precision", nullable: false),
                    FeatureName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlmPredictionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlmPredictionHistories_AlmPredictionJobs_AlmPredictionJobId",
                        column: x => x.AlmPredictionJobId,
                        principalTable: "AlmPredictionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlmPredictionHistories_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlmPredictionHistories_AlmPredictionJobId",
                table: "AlmPredictionHistories",
                column: "AlmPredictionJobId");

            migrationBuilder.CreateIndex(
                name: "IX_AlmPredictionHistories_DeviceId",
                table: "AlmPredictionHistories",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AlmPredictionJobs_DeviceId",
                table: "AlmPredictionJobs",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlmPredictionHistories");

            migrationBuilder.DropTable(
                name: "AlmPredictionJobs");
        }
    }
}
