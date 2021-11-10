using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class AddTablesGamesSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Prefix",
            table: "GuildSettings",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "/",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true,
            oldDefaultValue: "/");

        migrationBuilder.CreateTable(
            name: "MafiaSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                GuildSettingsId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MafiaSettings", x => x.Id);
                table.ForeignKey(
                    name: "FK_MafiaSettings_GuildSettings_GuildSettingsId",
                    column: x => x.GuildSettingsId,
                    principalTable: "GuildSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RussianRouletteSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UnicodeSmileKilled = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "💀"),
                UnicodeSmileSurvived = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "😎"),
                CustomSmileKilled = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CustomSmileSurvived = table.Column<string>(type: "nvarchar(max)", nullable: true),
                GuildSettingsId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RussianRouletteSettings", x => x.Id);
                table.ForeignKey(
                    name: "FK_RussianRouletteSettings_GuildSettings_GuildSettingsId",
                    column: x => x.GuildSettingsId,
                    principalTable: "GuildSettings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MafiaSettings_GuildSettingsId",
            table: "MafiaSettings",
            column: "GuildSettingsId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RussianRouletteSettings_GuildSettingsId",
            table: "RussianRouletteSettings",
            column: "GuildSettingsId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MafiaSettings");

        migrationBuilder.DropTable(
            name: "RussianRouletteSettings");

        migrationBuilder.AlterColumn<string>(
            name: "Prefix",
            table: "GuildSettings",
            type: "nvarchar(max)",
            nullable: true,
            defaultValue: "/",
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldDefaultValue: "/");
    }
}
