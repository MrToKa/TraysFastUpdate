using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class SpaceCalculations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SpaceAvailable",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpaceOccupied",
                table: "Trays",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpaceAvailable",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "SpaceOccupied",
                table: "Trays");
        }
    }
}
