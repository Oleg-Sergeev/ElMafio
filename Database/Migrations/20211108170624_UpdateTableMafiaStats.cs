using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class UpdateTableMafiaStats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "TotalWinRate",
            table: "MafiaStats",
            newName: "PenaltyScores");

        migrationBuilder.RenameColumn(
            name: "MurderWinsCount",
            table: "MafiaStats",
            newName: "DonSuccessfullMovesCount");

        migrationBuilder.RenameColumn(
            name: "MurderWinRate",
            table: "MafiaStats",
            newName: "DonEfficiency");

        migrationBuilder.RenameColumn(
            name: "MurderGamesCount",
            table: "MafiaStats",
            newName: "DonMovesCount");

        migrationBuilder.RenameColumn(
            name: "DoctorWinRate",
            table: "MafiaStats",
            newName: "DoctorEfficiency");

        migrationBuilder.RenameColumn(
            name: "CommissionerWinRate",
            table: "MafiaStats",
            newName: "CommissionerEfficiency");

        migrationBuilder.AddColumn<int>(
            name: "BlacksGamesCount",
            table: "MafiaStats",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<float>(
            name: "BlacksWinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            defaultValue: 0f);

        migrationBuilder.AddColumn<int>(
            name: "BlacksWinsCount",
            table: "MafiaStats",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BlacksGamesCount",
            table: "MafiaStats");

        migrationBuilder.DropColumn(
            name: "BlacksWinRate",
            table: "MafiaStats");

        migrationBuilder.DropColumn(
            name: "BlacksWinsCount",
            table: "MafiaStats");

        migrationBuilder.RenameColumn(
            name: "PenaltyScores",
            table: "MafiaStats",
            newName: "TotalWinRate");

        migrationBuilder.RenameColumn(
            name: "DonSuccessfullMovesCount",
            table: "MafiaStats",
            newName: "MurderWinsCount");

        migrationBuilder.RenameColumn(
            name: "DonMovesCount",
            table: "MafiaStats",
            newName: "MurderGamesCount");

        migrationBuilder.RenameColumn(
            name: "DonEfficiency",
            table: "MafiaStats",
            newName: "MurderWinRate");

        migrationBuilder.RenameColumn(
            name: "DoctorEfficiency",
            table: "MafiaStats",
            newName: "DoctorWinRate");

        migrationBuilder.RenameColumn(
            name: "CommissionerEfficiency",
            table: "MafiaStats",
            newName: "CommissionerWinRate");
    }
}