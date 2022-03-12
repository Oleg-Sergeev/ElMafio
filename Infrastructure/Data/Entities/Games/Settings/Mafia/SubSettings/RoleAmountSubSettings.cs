namespace Infrastructure.Data.Entities.Games.Settings.Mafia.SubSettings;


public record RoleAmountSubSettings
{
    public int Id { get; set; }

    public int MafiaSettingsTemplateId { get; set; }


    public int DoctorsCount { get; set; }

    public int SheriffsCount { get; set; }

    public int MurdersCount { get; set; }

    public int DonsCount { get; set; }

    public int InnocentsCount { get; set; }

    public int ManiacsCount { get; set; }

    public int HookersCount { get; set; }



    public int RedRolesCount => InnocentsCount + SheriffsCount + DoctorsCount;

    public int BlackRolesCount => MurdersCount + DonsCount;

    public int NeutralRolesCount => ManiacsCount + HookersCount;


    public int MinimumPlayersCount => RedRolesCount + BlackRolesCount + NeutralRolesCount;
}