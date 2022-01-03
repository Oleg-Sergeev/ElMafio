using Discord;

namespace Modules.Games.Mafia.Common.Interfaces;

public interface IKiller
{
    protected const string SuccessfullKillPhrases = "SuccessfullKill";
    protected const string FailedKillPhrases = "FailedKill";

    IGuildUser? KilledPlayer { get; }
}
