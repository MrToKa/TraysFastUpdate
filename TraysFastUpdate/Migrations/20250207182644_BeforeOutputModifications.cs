using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraysFastUpdate.Migrations
{
    /// <inheritdoc />
    public partial class BeforeOutputModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CableTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Diameter = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CableTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<double>(type: "double precision", nullable: false),
                    Height = table.Column<double>(type: "double precision", nullable: false),
                    Length = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    SupportsCount = table.Column<int>(type: "integer", nullable: true),
                    SupportsTotalWeight = table.Column<double>(type: "double precision", nullable: true),
                    SupportsWeightLoadPerMeter = table.Column<double>(type: "double precision", nullable: true),
                    ResultSupportsCount = table.Column<string>(type: "text", nullable: true),
                    ResultSupportsTotalWeight = table.Column<string>(type: "text", nullable: true),
                    ResultSupportsWeightLoadPerMeter = table.Column<string>(type: "text", nullable: true),
                    TrayWeightLoadPerMeter = table.Column<double>(type: "double precision", nullable: true),
                    TrayOwnWeightLoad = table.Column<double>(type: "double precision", nullable: true),
                    ResultTrayWeightLoadPerMeter = table.Column<string>(type: "text", nullable: true),
                    ResultTrayOwnWeightLoad = table.Column<string>(type: "text", nullable: true),
                    CablesWeightPerMeter = table.Column<double>(type: "double precision", nullable: true),
                    CablesWeightLoad = table.Column<double>(type: "double precision", nullable: true),
                    ResultCablesWeightPerMeter = table.Column<string>(type: "text", nullable: true),
                    ResultCablesWeightLoad = table.Column<string>(type: "text", nullable: true),
                    TotalWeightLoadPerMeter = table.Column<double>(type: "double precision", nullable: true),
                    TotalWeightLoad = table.Column<double>(type: "double precision", nullable: true),
                    ResultTotalWeightLoadPerMeter = table.Column<string>(type: "text", nullable: true),
                    ResultTotalWeightLoad = table.Column<string>(type: "text", nullable: true),
                    SpaceOccupied = table.Column<double>(type: "double precision", nullable: true),
                    SpaceAvailable = table.Column<double>(type: "double precision", nullable: true),
                    ResultSpaceOccupied = table.Column<string>(type: "text", nullable: true),
                    ResultSpaceAvailable = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    CableTypeId = table.Column<int>(type: "integer", nullable: false),
                    FromLocation = table.Column<string>(type: "text", nullable: true),
                    ToLocation = table.Column<string>(type: "text", nullable: true),
                    Routing = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cables_CableTypes_CableTypeId",
                        column: x => x.CableTypeId,
                        principalTable: "CableTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cables_CableTypeId",
                table: "Cables",
                column: "CableTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cables");

            migrationBuilder.DropTable(
                name: "Trays");

            migrationBuilder.DropTable(
                name: "CableTypes");
        }
    }
}
