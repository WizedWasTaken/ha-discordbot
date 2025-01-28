using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotTemplate.Migrations
{
    /// <inheritdoc />
    public partial class AddedStrikesUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GivenToUserId",
                table: "Strikes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Strikes_GivenToUserId",
                table: "Strikes",
                column: "GivenToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Strikes_Users_GivenToUserId",
                table: "Strikes",
                column: "GivenToUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Strikes_Users_GivenToUserId",
                table: "Strikes");

            migrationBuilder.DropIndex(
                name: "IX_Strikes_GivenToUserId",
                table: "Strikes");

            migrationBuilder.DropColumn(
                name: "GivenToUserId",
                table: "Strikes");
        }
    }
}
