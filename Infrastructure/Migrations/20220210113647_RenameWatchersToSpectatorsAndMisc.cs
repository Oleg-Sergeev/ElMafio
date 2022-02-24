using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

public partial class RenameWatchersToSpectatorsAndMisc : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "WatchersVoiceChannelId",
            table: "MafiaSettings",
            newName: "SpectatorsVoiceChannelId");

        migrationBuilder.RenameColumn(
            name: "WatchersTextChannelId",
            table: "MafiaSettings",
            newName: "SpectatorsTextChannelId");

        migrationBuilder.AlterColumn<int>(
            name: "SheriffShotsCount",
            table: "RolesExtraInfoSubSettings",
            type: "int",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "DoctorSelfHealsCount",
            table: "RolesExtraInfoSubSettings",
            type: "int",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "SpectatorsVoiceChannelId",
            table: "MafiaSettings",
            newName: "WatchersVoiceChannelId");

        migrationBuilder.RenameColumn(
            name: "SpectatorsTextChannelId",
            table: "MafiaSettings",
            newName: "WatchersTextChannelId");

        migrationBuilder.AlterColumn<int>(
            name: "SheriffShotsCount",
            table: "RolesExtraInfoSubSettings",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AlterColumn<int>(
            name: "DoctorSelfHealsCount",
            table: "RolesExtraInfoSubSettings",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");
    }
}
