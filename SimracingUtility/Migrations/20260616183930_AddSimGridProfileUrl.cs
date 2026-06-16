using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimracingUtility.Migrations
{
    /// <inheritdoc />
    public partial class AddSimGridProfileUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SimGridProfileUrl",
                table: "LmuDrivers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimGridProfileUrl",
                table: "LmuDrivers");
        }
    }
}
