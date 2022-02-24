using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class MinorChanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MafiaStats_Guilds_GuildId",
            table: "MafiaStats");

        migrationBuilder.DropForeignKey(
            name: "FK_RussianRouletteStats_Guilds_GuildId",
            table: "RussianRouletteStats");

        migrationBuilder.RenameColumn(
            name: "GuildId",
            table: "RussianRouletteStats",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_RussianRouletteStats_GuildId",
            table: "RussianRouletteStats",
            newName: "IX_RussianRouletteStats_GuildSettingsId");

        migrationBuilder.RenameColumn(
            name: "GuildId",
            table: "MafiaStats",
            newName: "GuildSettingsId");

        migrationBuilder.RenameIndex(
            name: "IX_MafiaStats_GuildId",
            table: "MafiaStats",
            newName: "IX_MafiaStats_GuildSettingsId");

        migrationBuilder.RenameColumn(
            name: "ReplyMessagesOnError",
            table: "MafiaSettings",
            newName: "ReplyMessagesOnSetupError");

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaStats_GuildSettings_GuildSettingsId",
            table: "MafiaStats",
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MafiaStats_GuildSettings_GuildSettingsId",
            table: "MafiaStats");

        migrationBuilder.DropForeignKey(
            name: "FK_RussianRouletteStats_GuildSettings_GuildSettingsId",
            table: "RussianRouletteStats");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "RussianRouletteStats",
            newName: "GuildId");

        migrationBuilder.RenameIndex(
            name: "IX_RussianRouletteStats_GuildSettingsId",
            table: "RussianRouletteStats",
            newName: "IX_RussianRouletteStats_GuildId");

        migrationBuilder.RenameColumn(
            name: "GuildSettingsId",
            table: "MafiaStats",
            newName: "GuildId");

        migrationBuilder.RenameIndex(
            name: "IX_MafiaStats_GuildSettingsId",
            table: "MafiaStats",
            newName: "IX_MafiaStats_GuildId");

        migrationBuilder.RenameColumn(
            name: "ReplyMessagesOnSetupError",
            table: "MafiaSettings",
            newName: "ReplyMessagesOnError");

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaStats_GuildSettings_GuildId",
            table: "MafiaStats",
            column: "GuildId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_RussianRouletteStats_GuildSettings_GuildId",
            table: "RussianRouletteStats",
            column: "GuildId",
            principalTable: "GuildSettings",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
