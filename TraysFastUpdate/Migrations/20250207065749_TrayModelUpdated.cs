using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class TrayModelUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResultCablesWeightLoad",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultCablesWeightPerMeter",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSpaceAvailable",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSpaceOccupied",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultTotalWeightLoad",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultTotalWeightLoadPerMeter",
                table: "Trays",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultCablesWeightLoad",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultCablesWeightPerMeter",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultSpaceAvailable",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultSpaceOccupied",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultTotalWeightLoad",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultTotalWeightLoadPerMeter",
                table: "Trays");
        }
    }
}
