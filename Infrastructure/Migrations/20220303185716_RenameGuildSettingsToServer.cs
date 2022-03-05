using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class RenameGuildSettingsToServer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MafiaSettings_GuildSettings_GuildSettingsId",
            table: "MafiaSettings");

        migrationBuilder.DropForeignKey(
            name: "FK_MafiaStats_GuildSettings_GuildSettingsId",
            table: "MafiaStats");

        migrationBuilder.DropForeignKey(
            name: "FK_QuizStats_GuildSettings_GuildSettingsId",
            table: "QuizStats");

        migrationBuilder.DropForeignKey(
            name: "FK_RussianRouletteSettings_GuildSettings_GuildSettingsId",
            table: "RussianRouletteSettings");

        migrationBuilder.DropForeignKey(
            name: "FK_RussianRouletteStats_GuildSettings_GuildSettingsId",
            table: "RussianRouletteStats");

        migrationBuilder.RenameTable(
            name: "GuildSettings",
            newName: "Servers");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "RussianRouletteStats",
            newName: "ServerId");

        migrationBuilder.RenameIndex(
            name: "IX_RussianRouletteStats_GuildSettingsId",
            table: "RussianRouletteStats",
            newName: "IX_RussianRouletteStats_ServerId");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "RussianRouletteSettings",
            newName: "ServerId");

        migrationBuilder.RenameIndex(
            name: "IX_RussianRouletteSettings_GuildSettingsId",
            table: "RussianRouletteSettings",
            newName: "IX_RussianRouletteSettings_ServerId");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "QuizStats",
            newName: "ServerId");

        migrationBuilder.RenameIndex(
            name: "IX_QuizStats_GuildSettingsId",
            table: "QuizStats",
            newName: "IX_QuizStats_ServerId");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "MafiaStats",
            newName: "ServerId");

        migrationBuilder.RenameIndex(
            name: "IX_MafiaStats_GuildSettingsId",
            table: "MafiaStats",
            newName: "IX_MafiaStats_ServerId");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "MafiaSettings",
            newName: "ServerId");

        migrationBuilder.RenameIndex(
            name: "IX_MafiaSettings_GuildSettingsId",
            table: "MafiaSettings",
            newName: "IX_MafiaSettings_ServerId");

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaSettings_Servers_ServerId",
            table: "MafiaSettings",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaStats_Servers_ServerId",
            table: "MafiaStats",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_QuizStats_Servers_ServerId",
            table: "QuizStats",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RussianRouletteSettings_Servers_ServerId",
            table: "RussianRouletteSettings",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RussianRouletteStats_Servers_ServerId",
            table: "RussianRouletteStats",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MafiaSettings_Servers_ServerId",
            table: "MafiaSettings");

        migrationBuilder.DropForeignKey(
            name: "FK_MafiaStats_Servers_ServerId",
            table: "MafiaStats");

        migrationBuilder.DropForeignKey(
            name: "FK_QuizStats_Servers_ServerId",
            table: "QuizStats");

        migrationBuilder.DropForeignKey(
            name: "FK_RussianRouletteSettings_Servers_ServerId",
            table: "RussianRouletteSettings");

        migrationBuilder.DropForeignKey(
            name: "FK_RussianRouletteStats_Servers_ServerId",
            table: "RussianRouletteStats");

        migrationBuilder.RenameTable(
            name: "Servers",
            newName: "GuildSettings");

        migrationBuilder.RenameColumn(
            name: "ServerId",
            table: "RussianRouletteStats",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_RussianRouletteStats_ServerId",
            table: "RussianRouletteStats",
            newName: "IX_RussianRouletteStats_GuildSettingsId");

        migrationBuilder.RenameColumn(
            name: "ServerId",
            table: "RussianRouletteSettings",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_RussianRouletteSettings_ServerId",
            table: "RussianRouletteSettings",
            newName: "IX_RussianRouletteSettings_GuildSettingsId");

        migrationBuilder.RenameColumn(
            name: "ServerId",
            table: "QuizStats",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_QuizStats_ServerId",
            table: "QuizStats",
            newName: "IX_QuizStats_GuildSettingsId");

        migrationBuilder.RenameColumn(
            name: "ServerId",
            table: "MafiaStats",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_MafiaStats_ServerId",
            table: "MafiaStats",
            newName: "IX_MafiaStats_GuildSettingsId");

        migrationBuilder.RenameColumn(
            name: "ServerId",
            table: "MafiaSettings",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_MafiaSettings_ServerId",
            table: "MafiaSettings",
            newName: "IX_MafiaSettings_GuildSettingsId");

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaSettings_GuildSettings_GuildSettingsId",
            table: "MafiaSettings",
            column: "GuildSettingsId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaStats_GuildSettings_GuildSettingsId",
            table: "MafiaStats",
            column: "GuildSettingsId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_QuizStats_GuildSettings_GuildSettingsId",
            table: "QuizStats",
            column: "GuildSettingsId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RussianRouletteSettings_GuildSettings_GuildSettingsId",
            table: "RussianRouletteSettings",
            column: "GuildSettingsId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RussianRouletteStats_GuildSettings_GuildSettingsId",
            table: "RussianRouletteStats",
            column: "GuildSettingsId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
