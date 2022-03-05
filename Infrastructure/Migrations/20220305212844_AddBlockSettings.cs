using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddBlockSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BlockBehaviour",
            table: "Servers",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "BlockMessage",
            table: "Servers",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "Вам заблокирован доступ к командам. Пожалуйста, обратитесь к администраторам сервера для разблокировки");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BlockBehaviour",
            table: "Servers");

        migrationBuilder.DropColumn(
            name: "BlockMessage",
            table: "Servers");
    }
}
