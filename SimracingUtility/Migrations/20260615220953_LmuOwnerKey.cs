using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimracingUtility.Migrations
{
    /// <inheritdoc />
    public partial class LmuOwnerKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LmuDrivers_DriverName",
                table: "LmuDrivers");

            migrationBuilder.AddColumn<string>(
                name: "OwnerKey",
                table: "LmuDrivers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LmuDrivers_DriverName",
                table: "LmuDrivers",
                column: "DriverName");

            migrationBuilder.CreateIndex(
                name: "IX_LmuDrivers_OwnerKey",
                table: "LmuDrivers",
                column: "OwnerKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LmuDrivers_DriverName",
                table: "LmuDrivers");

            migrationBuilder.DropIndex(
                name: "IX_LmuDrivers_OwnerKey",
                table: "LmuDrivers");

            migrationBuilder.DropColumn(
                name: "OwnerKey",
                table: "LmuDrivers");

            migrationBuilder.CreateIndex(
                name: "IX_LmuDrivers_DriverName",
                table: "LmuDrivers",
                column: "DriverName",
                unique: true);
        }
    }
}
