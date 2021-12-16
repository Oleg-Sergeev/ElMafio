namespace Core.ViewModels;

public class GameSubSettingsViewModel
{
    public int? MafiaCoefficient { get; init; }

    public int? LastWordNightCount { get; init; }

    public bool? IsRatingGame { get; init; }

    public bool? IsCustomGame { get; init; }

    public bool? ConditionAliveAtLeast1Innocent { get; init; }

    public bool? IsTurnByTurnVote { get; init; }
}
