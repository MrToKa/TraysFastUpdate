using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class SupportsCountAndTotalWeightAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupportsCount",
                table: "Trays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SupportsTotalWeight",
                table: "Trays",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportsCount",
                table: "Trays");

            migrationBuilder.DropColumn(
                name: "SupportsTotalWeight",
                table: "Trays");
        }
    }
}
