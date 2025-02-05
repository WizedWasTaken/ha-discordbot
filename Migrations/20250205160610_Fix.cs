using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotTemplate.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaidAmountId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaidAmounts",
                columns: table => new
                {
                    PaidAmountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidByUserId = table.Column<int>(type: "int", nullable: false),
                    BandeBuyEventEventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaidAmounts", x => x.PaidAmountId);
                    table.ForeignKey(
                        name: "FK_PaidAmounts_Events_BandeBuyEventEventId",
                        column: x => x.BandeBuyEventEventId,
                        principalTable: "Events",
                        principalColumn: "EventId");
                    table.ForeignKey(
                        name: "FK_PaidAmounts_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PaidAmountId",
                table: "Users",
                column: "PaidAmountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaidAmounts_BandeBuyEventEventId",
                table: "PaidAmounts",
                column: "BandeBuyEventEventId");

            migrationBuilder.CreateIndex(
                name: "IX_PaidAmounts_PaidByUserId",
                table: "PaidAmounts",
                column: "PaidByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_PaidAmounts_PaidAmountId",
                table: "Users",
                column: "PaidAmountId",
                principalTable: "PaidAmounts",
                principalColumn: "PaidAmountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_PaidAmounts_PaidAmountId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "PaidAmounts");

            migrationBuilder.DropIndex(
                name: "IX_Users_PaidAmountId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PaidAmountId",
                table: "Users");
        }
    }
}