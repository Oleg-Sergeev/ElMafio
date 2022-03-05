using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddBlockUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BlockUsers",
            columns: table => new
            {
                UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BlockUsers", x => new { x.UserId, x.ServerId });
                table.ForeignKey(
                    name: "FK_BlockUsers_Servers_ServerId",
                    column: x => x.ServerId,
                    principalTable: "Servers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BlockUsers_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BlockUsers_ServerId",
            table: "BlockUsers",
            column: "ServerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BlockUsers");
    }
}
