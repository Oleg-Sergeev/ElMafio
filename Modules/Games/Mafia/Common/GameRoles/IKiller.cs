using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IKiller : IActiveRole
{
    protected const string SuccessfullKillPhrases = "SuccessfullKill";
    protected const string FailedKillPhrases = "FailedKill";


    int KillsCount { get; set; }

    IGuildUser? KilledPlayer { get; }
}
