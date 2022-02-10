using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IKiller
{
    protected const string SuccessfullKillPhrases = "SuccessfullKill";
    protected const string FailedKillPhrases = "FailedKill";

    IGuildUser? KilledPlayer { get; }
}
