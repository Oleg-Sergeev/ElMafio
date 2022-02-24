using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class ReplacePreGameMessage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PreGameMessage",
            table: "MafiaSettingsTemplates");

        migrationBuilder.AddColumn<string>(
            name: "PreGameMessage",
            table: "GameSubSettings",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PreGameMessage",
            table: "GameSubSettings");

        migrationBuilder.AddColumn<string>(
            name: "PreGameMessage",
            table: "MafiaSettingsTemplates",
            type: "nvarchar(max)",
            nullable: true);
    }
}
