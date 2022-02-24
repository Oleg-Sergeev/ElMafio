using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddGameRating : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldStored: null);

        migrationBuilder.AddColumn<float>(
            name: "Rating",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            stored: true);

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "QuizStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldStored: null);

        migrationBuilder.AddColumn<float>(
            name: "Rating",
            table: "QuizStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            stored: true);

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldStored: null);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Rating",
            table: "RussianRouletteStats");

        migrationBuilder.DropColumn(
            name: "Rating",
            table: "QuizStats");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "QuizStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)");

        migrationBuilder.AlterColumn<float>(
            name: "WinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)");
    }
}
