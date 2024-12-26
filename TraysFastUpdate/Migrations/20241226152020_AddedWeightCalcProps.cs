using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class AddedWeightCalcProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CablesWeightLoad",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CablesWeightPerMeter",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SupportsWeightLoadPerMeter",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalWeightLoad",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalWeightLoadPerMeter",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrayOwnWeightLoad",
                table: "Trays",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrayWeightLoadPerMeter",
                table: "Trays",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CablesWeightLoad",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "CablesWeightPerMeter",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "SupportsWeightLoadPerMeter",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "TotalWeightLoad",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "TotalWeightLoadPerMeter",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "TrayOwnWeightLoad",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "TrayWeightLoadPerMeter",
                table: "Trays");
        }
    }
}
