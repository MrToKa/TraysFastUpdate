using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class TrayWeightStringDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResultTrayOwnWeightLoad",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultTrayWeightLoadPerMeter",
                table: "Trays",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultTrayOwnWeightLoad",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultTrayWeightLoadPerMeter",
                table: "Trays");
        }
    }
}
