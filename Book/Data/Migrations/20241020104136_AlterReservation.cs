using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareInvest.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlterReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Policy",
                table: "Reservations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Policy",
                table: "Reservations");
        }
    }
}
