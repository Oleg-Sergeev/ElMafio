using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class UpdateMafiaStats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "DonSuccessfullMovesCount",
            table: "MafiaStats",
            newName: "SheriffRevealsCount");

        migrationBuilder.RenameColumn(
            name: "DoctorSuccessfullMovesCount",
            table: "MafiaStats",
            newName: "SheriffMovesCount");

        migrationBuilder.RenameColumn(
            name: "CommissionerSuccessfullShotsCount",
            table: "MafiaStats",
            newName: "SheriffKillsCount");

        migrationBuilder.RenameColumn(
            name: "CommissionerSuccessfullFoundsCount",
            table: "MafiaStats",
            newName: "DonRevealsCount");

        migrationBuilder.RenameColumn(
            name: "CommissionerMovesCount",
            table: "MafiaStats",
            newName: "DoctorHealsCount");

        migrationBuilder.RenameColumn(
            name: "CommissionerEfficiency",
            table: "MafiaStats",
            newName: "SheriffEfficiency");

        migrationBuilder.AlterColumn<DateTime>(
            name: "JoinedAt",
            table: "Users",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "SheriffRevealsCount",
            table: "MafiaStats",
            newName: "DonSuccessfullMovesCount");

        migrationBuilder.RenameColumn(
            name: "SheriffMovesCount",
            table: "MafiaStats",
            newName: "DoctorSuccessfullMovesCount");

        migrationBuilder.RenameColumn(
            name: "SheriffKillsCount",
            table: "MafiaStats",
            newName: "CommissionerSuccessfullShotsCount");

        migrationBuilder.RenameColumn(
            name: "SheriffEfficiency",
            table: "MafiaStats",
            newName: "CommissionerEfficiency");

        migrationBuilder.RenameColumn(
            name: "DonRevealsCount",
            table: "MafiaStats",
            newName: "CommissionerSuccessfullFoundsCount");

        migrationBuilder.RenameColumn(
            name: "DoctorHealsCount",
            table: "MafiaStats",
            newName: "CommissionerMovesCount");

        migrationBuilder.AlterColumn<DateTime>(
            name: "JoinedAt",
            table: "Users",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldNullable: true);
    }
}
