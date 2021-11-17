using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class AddSettingValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "TotalRating",
            table: "MafiaStats",
            newName: "TotalWinRate");

        migrationBuilder.RenameColumn(
            name: "MurderRating",
            table: "MafiaStats",
            newName: "Scores");

        migrationBuilder.RenameColumn(
            name: "DoctorRating",
            table: "MafiaStats",
            newName: "Rating");

        migrationBuilder.RenameColumn(
            name: "CommissionerRating",
            table: "MafiaStats",
            newName: "MurderWinRate");

        migrationBuilder.AddColumn<float>(
            name: "CommissionerWinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            defaultValue: 0f);

        migrationBuilder.AddColumn<float>(
            name: "DoctorWinRate",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            defaultValue: 0f);

        migrationBuilder.AddColumn<float>(
            name: "ExtraScores",
            table: "MafiaStats",
            type: "real",
            nullable: false,
            defaultValue: 0f);

        migrationBuilder.AddColumn<bool>(
            name: "AbortGameWhenError",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "GeneralTextChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "GeneralVoiceChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsRatingGame",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "MafiaKoefficient",
            table: "MafiaSettings",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "MafiaRoleId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "MurdersTextChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "MurdersVoiceChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "RenameUsers",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "ReplyMessagesOnError",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<decimal>(
            name: "WatcherRoleId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "RoleMuteId",
            table: "GuildSettings",
            type: "decimal(20,0)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CommissionerWinRate",
            table: "MafiaStats");

        migrationBuilder.DropColumn(
            name: "DoctorWinRate",
            table: "MafiaStats");

        migrationBuilder.DropColumn(
            name: "ExtraScores",
            table: "MafiaStats");

        migrationBuilder.DropColumn(
            name: "AbortGameWhenError",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "GeneralTextChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "GeneralVoiceChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "IsRatingGame",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "MafiaKoefficient",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "MafiaRoleId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "MurdersTextChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "MurdersVoiceChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "RenameUsers",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "ReplyMessagesOnError",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "WatcherRoleId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "RoleMuteId",
            table: "GuildSettings");

        migrationBuilder.RenameColumn(
            name: "TotalWinRate",
            table: "MafiaStats",
            newName: "TotalRating");

        migrationBuilder.RenameColumn(
            name: "Scores",
            table: "MafiaStats",
            newName: "MurderRating");

        migrationBuilder.RenameColumn(
            name: "Rating",
            table: "MafiaStats",
            newName: "DoctorRating");

        migrationBuilder.RenameColumn(
            name: "MurderWinRate",
            table: "MafiaStats",
            newName: "CommissionerRating");
    }
}