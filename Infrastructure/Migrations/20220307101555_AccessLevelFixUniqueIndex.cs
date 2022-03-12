using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class AccessLevelFixUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AccessLevel_Name",
            table: "AccessLevel");

        migrationBuilder.DropIndex(
            name: "IX_AccessLevel_ServerId",
            table: "AccessLevel");

        migrationBuilder.CreateIndex(
            name: "IX_AccessLevel_ServerId_Name",
            table: "AccessLevel",
            columns: new[] { "ServerId", "Name" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AccessLevel_ServerId_Name",
            table: "AccessLevel");

        migrationBuilder.CreateIndex(
            name: "IX_AccessLevel_Name",
            table: "AccessLevel",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AccessLevel_ServerId",
            table: "AccessLevel",
            column: "ServerId");
    }
}
