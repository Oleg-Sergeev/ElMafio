using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GameSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "GuildSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "RoleAmountSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "RolesInfoSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.AddColumn<string>(
            name: "PreGameMessage",
            table: "MafiaSettingsTemplates",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PreGameMessage",
            table: "MafiaSettingsTemplates");

        migrationBuilder.AddColumn<string>(
            name: "GameSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "GuildSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RoleAmountSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RolesInfoSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);
    }
}
