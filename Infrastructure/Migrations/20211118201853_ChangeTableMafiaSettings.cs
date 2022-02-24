using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class ChangeTableMafiaSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AbortGameWhenError",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "CategoryChannelId",
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
            name: "ReplyMessagesOnSetupError",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "SendWelcomeMessage",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "WatcherRoleId",
            table: "MafiaSettings");

        migrationBuilder.RenameColumn(
            name: "RoleSettingsJsonData",
            table: "MafiaSettings",
            newName: "RoleSubSettingsJsonData");

        migrationBuilder.AddColumn<string>(
            name: "GuildSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GuildSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.RenameColumn(
            name: "RoleSubSettingsJsonData",
            table: "MafiaSettings",
            newName: "RoleSettingsJsonData");

        migrationBuilder.AddColumn<bool>(
            name: "AbortGameWhenError",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "CategoryChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

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
            name: "ReplyMessagesOnSetupError",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "SendWelcomeMessage",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<decimal>(
            name: "WatcherRoleId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);
    }
}
