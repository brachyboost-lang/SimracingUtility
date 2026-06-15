using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimracingUtility.Migrations
{
    /// <inheritdoc />
    public partial class AddLmuStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LmuDriverStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TotalRaces = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Podiums = table.Column<int>(type: "integer", nullable: false),
                    Top5 = table.Column<int>(type: "integer", nullable: false),
                    Top10 = table.Column<int>(type: "integer", nullable: false),
                    TopHalf = table.Column<int>(type: "integer", nullable: false),
                    Dnf = table.Column<int>(type: "integer", nullable: false),
                    BestPosition = table.Column<int>(type: "integer", nullable: false),
                    FastestLapTime = table.Column<double>(type: "double precision", nullable: false),
                    LastRaceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmuDriverStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmuCompanions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LmuDriverStatsId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RacesShared = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmuCompanions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LmuCompanions_LmuDriverStats_LmuDriverStatsId",
                        column: x => x.LmuDriverStatsId,
                        principalTable: "LmuDriverStats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LmuCompanions_LmuDriverStatsId",
                table: "LmuCompanions",
                column: "LmuDriverStatsId");

            migrationBuilder.CreateIndex(
                name: "IX_LmuDriverStats_DriverName",
                table: "LmuDriverStats",
                column: "DriverName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LmuCompanions");

            migrationBuilder.DropTable(
                name: "LmuDriverStats");
        }
    }
}
