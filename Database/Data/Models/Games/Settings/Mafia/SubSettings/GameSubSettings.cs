namespace Infrastructure.Data.Models.Games.Settings.Mafia.SubSettings;

public record GameSubSettings
{
    public int MafiaCoefficient { get; init; }

    public int LastWordNightCount { get; init; }

    public bool IsRatingGame { get; init; }

    public bool IsCustomGame { get; init; }

    public bool ConditionAliveAtLeast1Innocent { get; init; }

    public bool ConditionContinueGameWithNeutrals { get; init; }

    public bool IsTurnByTurnVote { get; init; }


    public int VoteTime { get; init; }




    public GameSubSettings()
    {
        MafiaCoefficient = 3;

        LastWordNightCount = 0;

        IsRatingGame = false;

        IsCustomGame = false;

        ConditionAliveAtLeast1Innocent = false;

        ConditionContinueGameWithNeutrals = false;

        IsTurnByTurnVote = false;


        VoteTime = 40;
    }
}
