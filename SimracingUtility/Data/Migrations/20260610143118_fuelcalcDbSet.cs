using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimracingUtility.Data.Migrations
{
    /// <inheritdoc />
    public partial class fuelcalcDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelCalc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    TrackName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PitBoxTime = table.Column<double>(type: "float", nullable: false),
                    FuelPerLap = table.Column<double>(type: "float", nullable: false),
                    DriveThroughTime = table.Column<double>(type: "float", nullable: false),
                    CarName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FuelTankCapacity = table.Column<double>(type: "float", nullable: false),
                    TimePerLap = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelCalc", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelCalc");
        }
    }
}
