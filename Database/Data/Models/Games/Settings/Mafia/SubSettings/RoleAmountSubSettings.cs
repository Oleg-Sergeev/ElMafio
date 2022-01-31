namespace Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;


public record class RoleAmountSubSettings
{
    public int DoctorsCount { get; init; }

    public int SheriffsCount { get; init; }

    public int MurdersCount { get; init; }

    public int DonsCount { get; init; }

    public int InnocentsCount { get; init; }

    public int ManiacsCount { get; init; }

    public int HookersCount { get; init; }



    public int RedRolesCount => InnocentsCount + SheriffsCount + DoctorsCount;
    
    public int BlackRolesCount => MurdersCount + DonsCount;

    public int NeutralRolesCount => ManiacsCount + HookersCount;


    public int MinimumPlayersCount => RedRolesCount + BlackRolesCount + NeutralRolesCount;
}