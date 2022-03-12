namespace Infrastructure.Data.Entities.Games.Settings.Mafia.SubSettings;

public record GameSubSettings
{
    public int Id { get; set; }

    public int MafiaSettingsTemplateId { get; set; }


    public int MafiaCoefficient { get; set; }

    public int LastWordNightCount { get; set; }

    public bool IsAnonymousVoting { get; set; }

    public bool IsRatingGame { get; set; }

    public bool IsCustomGame { get; set; }

    public bool ConditionAliveAtLeast1Innocent { get; set; }

    public bool ConditionContinueGameWithNeutrals { get; set; }

    public bool IsFillWithMurders { get; set; }


    public int VoteTime { get; set; }


    public string? PreGameMessage { get; set; }



    public GameSubSettings()
    {
        MafiaCoefficient = 3;

        LastWordNightCount = 0;

        IsRatingGame = false;

        IsCustomGame = false;

        ConditionAliveAtLeast1Innocent = false;

        ConditionContinueGameWithNeutrals = false;

        IsFillWithMurders = false;

        VoteTime = 40;

        PreGameMessage = null;
    }
}
