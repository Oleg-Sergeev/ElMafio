using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddNavProp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_MafiaSettings_CurrentTemplateId",
            table: "MafiaSettings",
            column: "CurrentTemplateId");

        migrationBuilder.AddForeignKey(
            name: "FK_MafiaSettings_MafiaSettingsTemplates_CurrentTemplateId",
            table: "MafiaSettings",
            column: "CurrentTemplateId",
            principalTable: "MafiaSettingsTemplates",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MafiaSettings_MafiaSettingsTemplates_CurrentTemplateId",
            table: "MafiaSettings");

        migrationBuilder.DropIndex(
            name: "IX_MafiaSettings_CurrentTemplateId",
            table: "MafiaSettings");
    }
}
