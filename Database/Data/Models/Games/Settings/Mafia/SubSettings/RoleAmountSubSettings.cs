namespace Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;


public record class RoleAmountSubSettings
{
    public int? DoctorsCount { get; init; }

    public int? SheriffsCount { get; init; }

    public int? MurdersCount { get; init; }

    public int? DonsCount { get; init; }

    public int? InnocentCount { get; init; }


    public int? RedRolesCount => InnocentCount is null && DoctorsCount is null && SheriffsCount is null ? null : (InnocentCount ?? 0) + (DoctorsCount ?? 0) + (SheriffsCount ?? 0);

    public int? BlackRolesCount => MurdersCount is null && DonsCount is null ? null : (MurdersCount ?? 0) + (DonsCount ?? 0);

    public int MinimumPlayersCount => (RedRolesCount ?? 0) + (BlackRolesCount ?? 0);
}