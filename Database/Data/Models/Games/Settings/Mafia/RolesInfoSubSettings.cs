namespace Infrastructure.Data.Models.Games.Settings.Mafia;

public record RolesInfoSubSettings
{
    public int? DoctorSelfHealsCount { get; init; }

    public int? SheriffShotsCount { get; init; }

    public bool MurdersKnowEachOther { get; init; }

    public bool MurdersVoteTogether { get; init; }

    public bool MurdersMustVoteForOnePlayer { get; init; }

    public bool CanInnocentsKillAtNight { get; init; }

    public bool InnocentsMustVoteForOnePlayer { get; init; }


    public RolesInfoSubSettings()
    {
        MurdersKnowEachOther = true;

        MurdersVoteTogether = true;

        MurdersMustVoteForOnePlayer = false;

        CanInnocentsKillAtNight = false;

        InnocentsMustVoteForOnePlayer = false;
    }
}
