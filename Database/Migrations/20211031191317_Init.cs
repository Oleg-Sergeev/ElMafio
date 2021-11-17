using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class Init : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Guilds",
            columns: table => new
            {
                Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Guilds", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MafiaStats",
            columns: table => new
            {
                UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                MurderGamesCount = table.Column<int>(type: "int", nullable: false),
                MurderWinsCount = table.Column<int>(type: "int", nullable: false),
                MurderRating = table.Column<float>(type: "real", nullable: false),
                DoctorMovesCount = table.Column<int>(type: "int", nullable: false),
                DoctorSuccessfullMovesCount = table.Column<int>(type: "int", nullable: false),
                DoctorRating = table.Column<float>(type: "real", nullable: false),
                CommissionerMovesCount = table.Column<int>(type: "int", nullable: false),
                CommissionerSuccessfullMovesCount = table.Column<int>(type: "int", nullable: false),
                CommissionerRating = table.Column<float>(type: "real", nullable: false),
                TotalRating = table.Column<float>(type: "real", nullable: false),
                GamesCount = table.Column<int>(type: "int", nullable: false),
                WinsCount = table.Column<int>(type: "int", nullable: false),
                WinRate = table.Column<float>(type: "real", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MafiaStats", x => new { x.UserId, x.GuildId });
                table.ForeignKey(
                    name: "FK_MafiaStats_Guilds_GuildId",
                    column: x => x.GuildId,
                    principalTable: "Guilds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MafiaStats_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RussianRouletteStats",
            columns: table => new
            {
                UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                GuildId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                GamesCount = table.Column<int>(type: "int", nullable: false),
                WinsCount = table.Column<int>(type: "int", nullable: false),
                WinRate = table.Column<float>(type: "real", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RussianRouletteStats", x => new { x.UserId, x.GuildId });
                table.ForeignKey(
                    name: "FK_RussianRouletteStats_Guilds_GuildId",
                    column: x => x.GuildId,
                    principalTable: "Guilds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RussianRouletteStats_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MafiaStats_GuildId",
            table: "MafiaStats",
            column: "GuildId");

        migrationBuilder.CreateIndex(
            name: "IX_RussianRouletteStats_GuildId",
            table: "RussianRouletteStats",
            column: "GuildId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MafiaStats");

        migrationBuilder.DropTable(
            name: "RussianRouletteStats");

        migrationBuilder.DropTable(
            name: "Guilds");

        migrationBuilder.DropTable(
            name: "Users");
    }
}