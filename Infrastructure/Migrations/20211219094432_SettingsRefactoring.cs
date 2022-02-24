using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class SettingsRefactoring : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.AddColumn<decimal>(
            name: "WatcherRoleId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "WatchersTextChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "WatchersVoiceChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
            name: "MafiaRoleId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "MurdersTextChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "MurdersVoiceChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "WatcherRoleId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "WatchersTextChannelId",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "WatchersVoiceChannelId",
            table: "MafiaSettings");
    }
}
