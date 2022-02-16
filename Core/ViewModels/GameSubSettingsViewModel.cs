using System.ComponentModel;

namespace Core.ViewModels;

public class GameSubSettingsViewModel
{
    [DisplayName("Коэффициент мафии")]
    public int? MafiaCoefficient { get; init; }


    [DisplayName("Число ночей с последним словом")]
    public int? LastWordNightCount { get; init; }


    [DisplayName("Рейтинговая игра")]
    public bool? IsRatingGame { get; init; }


    [DisplayName("Пользовательская игра")]
    public bool? IsCustomGame { get; init; }


    [DisplayName("Играть до последнего мирного")]
    public bool? ConditionAliveAtLeast1Innocent { get; init; }


    [DisplayName("Продолжать игру с нейтралами")]
    public bool? ConditionContinueGameWithNeutrals { get; init; }


    [DisplayName("Заполнять стол мафией")]
    public bool? IsFillWithMurders { get; init; }


    [DisplayName("Время голосования")]
    public int? VoteTime { get; init; }


    [DisplayName("Преамбула")]
    public string? PreGameMessage { get; init; }
}
