using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SimracingUtility.Migrations
{
    /// <inheritdoc />
    public partial class RedesignLmuStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LmuCompanions");

            migrationBuilder.DropTable(
                name: "LmuDriverStats");

            migrationBuilder.CreateTable(
                name: "LmuDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmuDrivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmuCategoryStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LmuDriverId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalRaces = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Podiums = table.Column<int>(type: "integer", nullable: false),
                    Top5 = table.Column<int>(type: "integer", nullable: false),
                    Top10 = table.Column<int>(type: "integer", nullable: false),
                    TopHalf = table.Column<int>(type: "integer", nullable: false),
                    Dnf = table.Column<int>(type: "integer", nullable: false),
                    BestPosition = table.Column<int>(type: "integer", nullable: false),
                    LastRaceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmuCategoryStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LmuCategoryStats_LmuDrivers_LmuDriverId",
                        column: x => x.LmuDriverId,
                        principalTable: "LmuDrivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LmuRacedWith",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LmuDriverId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RacesShared = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmuRacedWith", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LmuRacedWith_LmuDrivers_LmuDriverId",
                        column: x => x.LmuDriverId,
                        principalTable: "LmuDrivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LmuTrackBests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LmuDriverId = table.Column<int>(type: "integer", nullable: false),
                    Track = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BestLapTime = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmuTrackBests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LmuTrackBests_LmuDrivers_LmuDriverId",
                        column: x => x.LmuDriverId,
                        principalTable: "LmuDrivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LmuCategoryStats_LmuDriverId",
                table: "LmuCategoryStats",
                column: "LmuDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_LmuDrivers_DriverName",
                table: "LmuDrivers",
                column: "DriverName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LmuRacedWith_LmuDriverId",
                table: "LmuRacedWith",
                column: "LmuDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_LmuTrackBests_LmuDriverId",
                table: "LmuTrackBests",
                column: "LmuDriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LmuCategoryStats");

            migrationBuilder.DropTable(
                name: "LmuRacedWith");

            migrationBuilder.DropTable(
                name: "LmuTrackBests");

            migrationBuilder.DropTable(
                name: "LmuDrivers");

            migrationBuilder.CreateTable(
                name: "LmuDriverStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BestPosition = table.Column<int>(type: "integer", nullable: false),
                    Dnf = table.Column<int>(type: "integer", nullable: false),
                    DriverName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FastestLapTime = table.Column<double>(type: "double precision", nullable: false),
                    LastRaceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Podiums = table.Column<int>(type: "integer", nullable: false),
                    Top10 = table.Column<int>(type: "integer", nullable: false),
                    Top5 = table.Column<int>(type: "integer", nullable: false),
                    TopHalf = table.Column<int>(type: "integer", nullable: false),
                    TotalRaces = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false)
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
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
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
    }
}
