using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotTemplate.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscordId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    IngameName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Strikes",
                columns: table => new
                {
                    StrikeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GivenToUserId = table.Column<int>(type: "int", nullable: false),
                    GivenByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strikes", x => x.StrikeId);
                    table.ForeignKey(
                        name: "FK_Strikes_Users_GivenByUserId",
                        column: x => x.GivenByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Strikes_Users_GivenToUserId",
                        column: x => x.GivenToUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Strikes_GivenByUserId",
                table: "Strikes",
                column: "GivenByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Strikes_GivenToUserId",
                table: "Strikes",
                column: "GivenToUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Strikes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
