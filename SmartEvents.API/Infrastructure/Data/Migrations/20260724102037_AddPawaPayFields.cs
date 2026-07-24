using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEvents.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPawaPayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PawaPayDepositId",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PawaPayDepositId",
                table: "Payments");
        }
    }
}
