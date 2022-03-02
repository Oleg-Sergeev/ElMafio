using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class RemoveMuteRoleIdAndMisc : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MafiaSettingsTemplates_MafiaSettingsId",
            table: "MafiaSettingsTemplates");

        migrationBuilder.DropIndex(
            name: "IX_MafiaSettingsTemplates_Name",
            table: "MafiaSettingsTemplates");

        migrationBuilder.DropColumn(
            name: "RoleMuteId",
            table: "GuildSettings");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "MafiaSettingsTemplates",
            type: "nvarchar(450)",
            nullable: false,
            defaultValue: "__Default",
            oldClrType: typeof(string),
            oldType: "nvarchar(450)");

        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0,  CAST([WinsCount] AS REAL) / [GamesCount] * [WinsCount] * 100.0, 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount] * 100.0, 0.0)",
            oldStored: true);

        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "QuizStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0,  CAST([WinsCount] AS REAL) / [GamesCount] * [WinsCount] * 100.0, 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount] * 100.0, 0.0)",
            oldStored: true);

        migrationBuilder.CreateIndex(
            name: "IX_MafiaSettingsTemplates_MafiaSettingsId_Name",
            table: "MafiaSettingsTemplates",
            columns: new[] { "MafiaSettingsId", "Name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MafiaSettingsTemplates_MafiaSettingsId_Name",
            table: "MafiaSettingsTemplates");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "MafiaSettingsTemplates",
            type: "nvarchar(450)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)",
            oldDefaultValue: "__Default");

        migrationBuilder.AddColumn<decimal>(
            name: "RoleMuteId",
            table: "GuildSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount] * 100.0, 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0,  CAST([WinsCount] AS REAL) / [GamesCount] * [WinsCount] * 100.0, 0.0)",
            oldStored: true);

        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "QuizStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount] * 100.0, 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0,  CAST([WinsCount] AS REAL) / [GamesCount] * [WinsCount] * 100.0, 0.0)",
            oldStored: true);

        migrationBuilder.CreateIndex(
            name: "IX_MafiaSettingsTemplates_MafiaSettingsId",
            table: "MafiaSettingsTemplates",
            column: "MafiaSettingsId");

        migrationBuilder.CreateIndex(
            name: "IX_MafiaSettingsTemplates_Name",
            table: "MafiaSettingsTemplates",
            column: "Name",
            unique: true);
    }
}
