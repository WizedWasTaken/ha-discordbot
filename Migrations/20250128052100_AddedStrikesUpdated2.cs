using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotTemplate.Migrations
{
    /// <inheritdoc />
    public partial class AddedStrikesUpdated2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Strikes_Users_GivenByUserId",
                table: "Strikes");

            migrationBuilder.DropForeignKey(
                name: "FK_Strikes_Users_GivenToUserId",
                table: "Strikes");

            migrationBuilder.AddForeignKey(
                name: "FK_Strikes_Users_GivenByUserId",
                table: "Strikes",
                column: "GivenByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Strikes_Users_GivenToUserId",
                table: "Strikes",
                column: "GivenToUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Strikes_Users_GivenByUserId",
                table: "Strikes");

            migrationBuilder.DropForeignKey(
                name: "FK_Strikes_Users_GivenToUserId",
                table: "Strikes");

            migrationBuilder.AddForeignKey(
                name: "FK_Strikes_Users_GivenByUserId",
                table: "Strikes",
                column: "GivenByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Strikes_Users_GivenToUserId",
                table: "Strikes",
                column: "GivenToUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
