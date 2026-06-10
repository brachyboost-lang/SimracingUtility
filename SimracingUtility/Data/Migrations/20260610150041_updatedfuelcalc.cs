using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimracingUtility.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatedfuelcalc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Laps",
                table: "FuelCalc",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPitStops",
                table: "FuelCalc",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TotalFuelNeeded",
                table: "FuelCalc",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalTimeLost",
                table: "FuelCalc",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Laps",
                table: "FuelCalc");

            migrationBuilder.DropColumn(
                name: "NumberOfPitStops",
                table: "FuelCalc");

            migrationBuilder.DropColumn(
                name: "TotalFuelNeeded",
                table: "FuelCalc");

            migrationBuilder.DropColumn(
                name: "TotalTimeLost",
                table: "FuelCalc");
        }
    }
}
