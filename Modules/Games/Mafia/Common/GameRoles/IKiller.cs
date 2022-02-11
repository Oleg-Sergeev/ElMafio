using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IKiller
{
    protected const string SuccessfullKillPhrases = "SuccessfullKill";
    protected const string FailedKillPhrases = "FailedKill";


    int MovesCount { get; set; }

    int KillsCount { get; set; }

    bool IsSkip { get; }

    IGuildUser? KilledPlayer { get; }
}
