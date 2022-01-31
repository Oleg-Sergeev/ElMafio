using System.ComponentModel;

namespace Core.ViewModels;

public class GameSubSettingsViewModel
{
    [DisplayName("Коэффициент мафии")]
    public int? MafiaCoefficient { get; init; }

    [DisplayName("Число ночей с последним словом")]
    public int? LastWordNightCount { get; init; }

    public bool? IsRatingGame { get; init; }

    public bool? IsCustomGame { get; init; }

    public bool? ConditionAliveAtLeast1Innocent { get; init; }

    public bool? ConditionContinueGameWithNeutrals { get; init; }

    [DisplayName("Заполнять стол мафией")]
    public bool? IsFillWithMurders { get; init; }

    public int? VoteTime { get; init; }
}
