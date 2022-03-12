using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddAccessLevel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AccessLevelId",
            table: "ServerUsers",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "AccessLevel",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                Name = table.Column<string>(type: "nvarchar(450)", nullable: false, defaultValue: "Developer"),
                Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 2147483647)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccessLevel", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccessLevel_Servers_ServerId",
                    column: x => x.ServerId,
                    principalTable: "Servers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ServerUsers_AccessLevelId",
            table: "ServerUsers",
            column: "AccessLevelId");

        migrationBuilder.CreateIndex(
            name: "IX_AccessLevel_Name",
            table: "AccessLevel",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AccessLevel_ServerId",
            table: "AccessLevel",
            column: "ServerId");

        migrationBuilder.AddForeignKey(
            name: "FK_ServerUsers_AccessLevel_AccessLevelId",
            table: "ServerUsers",
            column: "AccessLevelId",
            principalTable: "AccessLevel",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ServerUsers_AccessLevel_AccessLevelId",
            table: "ServerUsers");

        migrationBuilder.DropTable(
            name: "AccessLevel");

        migrationBuilder.DropIndex(
            name: "IX_ServerUsers_AccessLevelId",
            table: "ServerUsers");

        migrationBuilder.DropColumn(
            name: "AccessLevelId",
            table: "ServerUsers");
    }
}
