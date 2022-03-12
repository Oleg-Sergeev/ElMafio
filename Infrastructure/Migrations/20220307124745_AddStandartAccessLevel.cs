using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AddStandartAccessLevel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AccessLevel_Servers_ServerId",
            table: "AccessLevel");

        migrationBuilder.DropForeignKey(
            name: "FK_ServerUsers_AccessLevel_AccessLevelId",
            table: "ServerUsers");

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccessLevel",
            table: "AccessLevel");

        migrationBuilder.RenameTable(
            name: "AccessLevel",
            newName: "AccessLevels");

        migrationBuilder.RenameIndex(
            name: "IX_AccessLevel_ServerId_Name",
            table: "AccessLevels",
            newName: "IX_AccessLevels_ServerId_Name");

        migrationBuilder.AddColumn<int>(
            name: "StandartAccessLevel",
            table: "ServerUsers",
            type: "int",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccessLevels",
            table: "AccessLevels",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_AccessLevels_Servers_ServerId",
            table: "AccessLevels",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ServerUsers_AccessLevels_AccessLevelId",
            table: "ServerUsers",
            column: "AccessLevelId",
            principalTable: "AccessLevels",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AccessLevels_Servers_ServerId",
            table: "AccessLevels");

        migrationBuilder.DropForeignKey(
            name: "FK_ServerUsers_AccessLevels_AccessLevelId",
            table: "ServerUsers");

        migrationBuilder.DropPrimaryKey(
            name: "PK_AccessLevels",
            table: "AccessLevels");

        migrationBuilder.DropColumn(
            name: "StandartAccessLevel",
            table: "ServerUsers");

        migrationBuilder.RenameTable(
            name: "AccessLevels",
            newName: "AccessLevel");

        migrationBuilder.RenameIndex(
            name: "IX_AccessLevels_ServerId_Name",
            table: "AccessLevel",
            newName: "IX_AccessLevel_ServerId_Name");

        migrationBuilder.AddPrimaryKey(
            name: "PK_AccessLevel",
            table: "AccessLevel",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_AccessLevel_Servers_ServerId",
            table: "AccessLevel",
            column: "ServerId",
            principalTable: "Servers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ServerUsers_AccessLevel_AccessLevelId",
            table: "ServerUsers",
            column: "AccessLevelId",
            principalTable: "AccessLevel",
            principalColumn: "Id");
    }
}
