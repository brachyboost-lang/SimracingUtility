using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimracingUtility.Migrations
{
    /// <inheritdoc />
    public partial class LmuRacedWithKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "LmuRacedWith",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "LmuRacedWith");
        }
    }
}
