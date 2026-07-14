using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CycleSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    AzureMapsId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationIntelligence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClimateSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BestTimesToVisit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TravelTips = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisaNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<byte>(type: "tinyint", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationIntelligence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationIntelligence_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationIntelligence_LocationId",
                table: "LocationIntelligence",
                column: "LocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_AzureMapsId",
                table: "Locations",
                column: "AzureMapsId",
                unique: true,
                filter: "[AzureMapsId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name_Country",
                table: "Locations",
                columns: new[] { "Name", "Country" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationIntelligence");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
