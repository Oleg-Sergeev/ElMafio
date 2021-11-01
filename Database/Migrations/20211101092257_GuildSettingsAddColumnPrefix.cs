using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class GuildSettingsAddColumnPrefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "GuildSettings",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "/");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "GuildSettings");
        }
    }
}
