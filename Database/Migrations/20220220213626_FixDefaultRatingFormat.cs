using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class FixDefaultRatingFormat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount] * 100.0, 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
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
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            oldStored: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<float>(
            name: "Rating",
            table: "RussianRouletteStats",
            type: "real",
            nullable: false,
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
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
            computedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount], 0.0)",
            stored: true,
            oldClrType: typeof(float),
            oldType: "real",
            oldComputedColumnSql: "IIF([GamesCount] != 0, CAST([WinsCount] AS REAL) / [GamesCount] * 100.0, 0.0)",
            oldStored: true);
    }
}
