using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddQuizStats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QuizStats",
            columns: table => new
            {
                UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                GuildSettingsId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                GamesCount = table.Column<int>(type: "int", nullable: false),
                WinsCount = table.Column<int>(type: "int", nullable: false),
                WinRate = table.Column<float>(type: "real", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuizStats", x => new { x.UserId, x.GuildSettingsId });
                table.ForeignKey(
                    name: "FK_QuizStats_GuildSettings_GuildSettingsId",
                    column: x => x.GuildSettingsId,
                    principalTable: "GuildSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_QuizStats_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_QuizStats_GuildSettingsId",
            table: "QuizStats",
            column: "GuildSettingsId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "QuizStats");
    }
}
