using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddMoreSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "RoleSubSettingsJsonData",
            table: "MafiaSettings",
            newName: "RolesInfoSubSettingsJsonData");

        migrationBuilder.AddColumn<string>(
            name: "GameSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RoleAmountSubSettingsJsonData",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GameSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.DropColumn(
            name: "RoleAmountSubSettingsJsonData",
            table: "MafiaSettings");

        migrationBuilder.RenameColumn(
            name: "RolesInfoSubSettingsJsonData",
            table: "MafiaSettings",
            newName: "RoleSubSettingsJsonData");
    }
}
