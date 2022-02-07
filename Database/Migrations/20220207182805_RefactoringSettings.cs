using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class RefactoringSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameSubSettingsJsonData",
                table: "MafiaSettingsTemplates");

            migrationBuilder.DropColumn(
                name: "GuildSubSettingsJsonData",
                table: "MafiaSettingsTemplates");

            migrationBuilder.DropColumn(
                name: "RoleAmountSubSettingsJsonData",
                table: "MafiaSettingsTemplates");

            migrationBuilder.DropColumn(
                name: "RolesInfoSubSettingsJsonData",
                table: "MafiaSettingsTemplates");

            migrationBuilder.DropColumn(
                name: "CurrentTemplateName",
                table: "MafiaSettings");

            migrationBuilder.AddColumn<int>(
                name: "CurrentTemplateId",
                table: "MafiaSettings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameSubSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MafiaSettingsTemplateId = table.Column<int>(type: "int", nullable: false),
                    MafiaCoefficient = table.Column<int>(type: "int", nullable: false),
                    LastWordNightCount = table.Column<int>(type: "int", nullable: false),
                    IsRatingGame = table.Column<bool>(type: "bit", nullable: false),
                    IsCustomGame = table.Column<bool>(type: "bit", nullable: false),
                    ConditionAliveAtLeast1Innocent = table.Column<bool>(type: "bit", nullable: false),
                    ConditionContinueGameWithNeutrals = table.Column<bool>(type: "bit", nullable: false),
                    IsFillWithMurders = table.Column<bool>(type: "bit", nullable: false),
                    VoteTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSubSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSubSettings_MafiaSettingsTemplates_MafiaSettingsTemplateId",
                        column: x => x.MafiaSettingsTemplateId,
                        principalTable: "MafiaSettingsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleAmountSubSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MafiaSettingsTemplateId = table.Column<int>(type: "int", nullable: false),
                    DoctorsCount = table.Column<int>(type: "int", nullable: false),
                    SheriffsCount = table.Column<int>(type: "int", nullable: false),
                    MurdersCount = table.Column<int>(type: "int", nullable: false),
                    DonsCount = table.Column<int>(type: "int", nullable: false),
                    InnocentsCount = table.Column<int>(type: "int", nullable: false),
                    ManiacsCount = table.Column<int>(type: "int", nullable: false),
                    HookersCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAmountSubSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAmountSubSettings_MafiaSettingsTemplates_MafiaSettingsTemplateId",
                        column: x => x.MafiaSettingsTemplateId,
                        principalTable: "MafiaSettingsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolesExtraInfoSubSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MafiaSettingsTemplateId = table.Column<int>(type: "int", nullable: false),
                    DoctorSelfHealsCount = table.Column<int>(type: "int", nullable: true),
                    SheriffShotsCount = table.Column<int>(type: "int", nullable: true),
                    MurdersKnowEachOther = table.Column<bool>(type: "bit", nullable: false),
                    MurdersVoteTogether = table.Column<bool>(type: "bit", nullable: false),
                    MurdersMustVoteForOnePlayer = table.Column<bool>(type: "bit", nullable: false),
                    CanInnocentsKillAtNight = table.Column<bool>(type: "bit", nullable: false),
                    InnocentsMustVoteForOnePlayer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesExtraInfoSubSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolesExtraInfoSubSettings_MafiaSettingsTemplates_MafiaSettingsTemplateId",
                        column: x => x.MafiaSettingsTemplateId,
                        principalTable: "MafiaSettingsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerSubSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MafiaSettingsTemplateId = table.Column<int>(type: "int", nullable: false),
                    RemoveRolesFromUsers = table.Column<bool>(type: "bit", nullable: false),
                    RenameUsers = table.Column<bool>(type: "bit", nullable: false),
                    ReplyMessagesOnSetupError = table.Column<bool>(type: "bit", nullable: false),
                    AbortGameWhenError = table.Column<bool>(type: "bit", nullable: false),
                    SendWelcomeMessage = table.Column<bool>(type: "bit", nullable: false),
                    MentionPlayersOnGameStart = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSubSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerSubSettings_MafiaSettingsTemplates_MafiaSettingsTemplateId",
                        column: x => x.MafiaSettingsTemplateId,
                        principalTable: "MafiaSettingsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSubSettings_MafiaSettingsTemplateId",
                table: "GameSubSettings",
                column: "MafiaSettingsTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAmountSubSettings_MafiaSettingsTemplateId",
                table: "RoleAmountSubSettings",
                column: "MafiaSettingsTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolesExtraInfoSubSettings_MafiaSettingsTemplateId",
                table: "RolesExtraInfoSubSettings",
                column: "MafiaSettingsTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerSubSettings_MafiaSettingsTemplateId",
                table: "ServerSubSettings",
                column: "MafiaSettingsTemplateId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSubSettings");

            migrationBuilder.DropTable(
                name: "RoleAmountSubSettings");

            migrationBuilder.DropTable(
                name: "RolesExtraInfoSubSettings");

            migrationBuilder.DropTable(
                name: "ServerSubSettings");

            migrationBuilder.DropColumn(
                name: "CurrentTemplateId",
                table: "MafiaSettings");

            migrationBuilder.AddColumn<string>(
                name: "GameSubSettingsJsonData",
                table: "MafiaSettingsTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuildSubSettingsJsonData",
                table: "MafiaSettingsTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleAmountSubSettingsJsonData",
                table: "MafiaSettingsTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolesInfoSubSettingsJsonData",
                table: "MafiaSettingsTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentTemplateName",
                table: "MafiaSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
