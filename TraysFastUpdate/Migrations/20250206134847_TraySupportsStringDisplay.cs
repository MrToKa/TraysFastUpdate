using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class TraySupportsStringDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResultSupportsCount",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSupportsTotalWeight",
                table: "Trays",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSupportsWeightLoadPerMeter",
                table: "Trays",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultSupportsCount",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultSupportsTotalWeight",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "ResultSupportsWeightLoadPerMeter",
                table: "Trays");
        }
    }
}
