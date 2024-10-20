using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareInvest.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Policy",
                columns: table => new
                {
                    ResortId = table.Column<string>(type: "TEXT", nullable: false),
                    ResortName = table.Column<string>(type: "TEXT", nullable: false),
                    Reservation = table.Column<string>(type: "TEXT", nullable: true),
                    Cabin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Campsite = table.Column<bool>(type: "INTEGER", nullable: false),
                    Wait = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policy", x => x.ResortId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Policy");
        }
    }
}
