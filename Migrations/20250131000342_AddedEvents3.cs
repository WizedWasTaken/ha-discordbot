using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotTemplate.Migrations
{
    /// <inheritdoc />
    public partial class AddedEvents3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageID",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageID",
                table: "Events");
        }
    }
}
