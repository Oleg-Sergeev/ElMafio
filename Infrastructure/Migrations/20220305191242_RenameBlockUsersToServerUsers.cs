using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class RenameBlockUsersToServerUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BlockUsers");

        migrationBuilder.AddColumn<bool>(
            name: "DisbandPartyAfterGameEnd",
            table: "MafiaSettings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "ServerUsers",
            columns: table => new
            {
                UserId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                IsBlocked = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ServerUsers", x => new { x.UserId, x.ServerId });
                table.ForeignKey(
                    name: "FK_ServerUsers_Servers_ServerId",
                    column: x => x.ServerId,
                    principalTable: "Servers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ServerUsers_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ServerUsers_ServerId",
            table: "ServerUsers",
            column: "ServerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ServerUsers");

        migrationBuilder.DropColumn(
            name: "DisbandPartyAfterGameEnd",
            table: "MafiaSettings");

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
}
