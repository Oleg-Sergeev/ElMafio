namespace Infrastructure.Data.Entities.Games.Settings.Mafia.SubSettings;

public record RolesExtraInfoSubSettings
{
    public int Id { get; set; }

    public int MafiaSettingsTemplateId { get; set; }


    public int DoctorSelfHealsCount { get; set; }

    public int SheriffShotsCount { get; set; }

    public bool MurdersKnowEachOther { get; set; }

    public bool MurdersVoteTogether { get; set; }

    public bool MurdersMustVoteForOnePlayer { get; set; }

    public bool CanInnocentsKillAtNight { get; set; }

    public bool InnocentsMustVoteForOnePlayer { get; set; }


    public RolesExtraInfoSubSettings()
    {
        DoctorSelfHealsCount = 1;

        SheriffShotsCount = 0;

        MurdersKnowEachOther = true;

        MurdersVoteTogether = true;

        MurdersMustVoteForOnePlayer = false;

        CanInnocentsKillAtNight = false;

        InnocentsMustVoteForOnePlayer = false;
    }
}
