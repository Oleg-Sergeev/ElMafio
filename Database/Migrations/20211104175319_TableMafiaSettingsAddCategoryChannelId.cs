using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class TableMafiaSettingsAddCategoryChannelId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "CategoryChannelId",
            table: "MafiaSettings",
            type: "decimal(20,0)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CategoryChannelId",
            table: "MafiaSettings");
    }
}