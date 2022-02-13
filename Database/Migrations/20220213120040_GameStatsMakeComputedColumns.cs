using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class GameStatsMakeComputedColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SheriffKillsCount",
            table: "MafiaStats");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "QuizStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "SheriffEfficiency",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([SheriffMovesCount] != 0, CAST([SheriffRevealsCount] AS REAL) / [SheriffMovesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "Scores",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "CAST(([WinsCount] + [BlacksWinsCount] * 1.25 + [DoctorHealsCount] * 1.5 + [SheriffRevealsCount] * 1.3 + [DonRevealsCount] * 1.3) AS REAL)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, 100.0 * (CAST(([WinsCount] + [BlacksWinsCount] * 1.25 + [DoctorHealsCount] * 1.5 + [SheriffRevealsCount] * 1.3 + [DonRevealsCount] * 1.3) AS REAL) + [ExtraScores] - [PenaltyScores]) * (CAST([WinsCount] AS REAL) / [GamesCount]), 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "DonEfficiency",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([DonMovesCount] != 0, CAST([DonRevealsCount] AS REAL) / [DonMovesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "DoctorEfficiency",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([DoctorMovesCount] != 0, CAST([DoctorHealsCount] AS REAL) / [DoctorMovesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real");

        migrationBuilder.AlterColumn<float>(
            name: "BlacksWinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([BlacksGamesCount] != 0, CAST([BlacksWinsCount] AS REAL) / [BlacksGamesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "QuizStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "SheriffEfficiency",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([SheriffMovesCount] != 0, CAST([SheriffRevealsCount] AS REAL) / [SheriffMovesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "Scores",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "CAST(([WinsCount] + [BlacksWinsCount] * 1.25 + [DoctorHealsCount] * 1.5 + [SheriffRevealsCount] * 1.3 + [DonRevealsCount] * 1.3) AS REAL)");

        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, 100.0 * (CAST(([WinsCount] + [BlacksWinsCount] * 1.25 + [DoctorHealsCount] * 1.5 + [SheriffRevealsCount] * 1.3 + [DonRevealsCount] * 1.3) AS REAL) + [ExtraScores] - [PenaltyScores]) * (CAST([WinsCount] AS REAL) / [GamesCount]), 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "DonEfficiency",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([DonMovesCount] != 0, CAST([DonRevealsCount] AS REAL) / [DonMovesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "DoctorEfficiency",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([DoctorMovesCount] != 0, CAST([DoctorHealsCount] AS REAL) / [DoctorMovesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "BlacksWinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([BlacksGamesCount] != 0, CAST([BlacksWinsCount] AS REAL) / [BlacksGamesCount], 0.0)");

        migrationBuilder.AddColumn<int>(
            name: "SheriffKillsCount",
            table: "MafiaStats",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }
}
