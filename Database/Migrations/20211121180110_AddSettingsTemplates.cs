using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddSettingsTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CurrentTemplateName",
            table: "MafiaSettings",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "MafiaSettingsTemplates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MafiaSettingsId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                RoleAmountSubSettingsJsonData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                RolesInfoSubSettingsJsonData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                GuildSubSettingsJsonData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                GameSubSettingsJsonData = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MafiaSettingsTemplates", x => x.Id);
                table.ForeignKey(
                    name: "FK_MafiaSettingsTemplates_MafiaSettings_MafiaSettingsId",
                    column: x => x.MafiaSettingsId,
                    principalTable: "MafiaSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MafiaSettingsTemplates_MafiaSettingsId",
            table: "MafiaSettingsTemplates",
            column: "MafiaSettingsId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MafiaSettingsTemplates");

        migrationBuilder.DropColumn(
            name: "CurrentTemplateName",
            table: "MafiaSettings");
    }
}
