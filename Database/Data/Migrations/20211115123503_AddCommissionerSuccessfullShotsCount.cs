using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Migrations;

public partial class AddCommissionerSuccessfullShotsCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "CommissionerSuccessfullMovesCount",
            table: "MafiaStats",
            newName: "CommissionerSuccessfullShotsCount");

        migrationBuilder.AddColumn<int>(
            name: "CommissionerSuccessfullFoundsCount",
            table: "MafiaStats",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CommissionerSuccessfullFoundsCount",
            table: "MafiaStats");

        migrationBuilder.RenameColumn(
            name: "CommissionerSuccessfullShotsCount",
            table: "MafiaStats",
            newName: "CommissionerSuccessfullMovesCount");
    }
}
