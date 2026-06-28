using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEvents.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VenueText",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VenueText",
                table: "Events");
        }
    }
}
