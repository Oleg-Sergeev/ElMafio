using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddSendInterval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SendInterval",
            table: "Servers",
            type: "int",
            nullable: false,
            defaultValue: 60);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Servers_SendInterval",
            table: "Servers",
            sql: "[SendInterval] BETWEEN 1 AND 600");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Servers_SendInterval",
            table: "Servers");

        migrationBuilder.DropColumn(
            name: "SendInterval",
            table: "Servers");
    }
}
