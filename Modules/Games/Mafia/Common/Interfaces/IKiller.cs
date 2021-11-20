using Discord;

namespace Modules.Games.Mafia.Common.Interfaces;

public interface IKiller
{
    IGuildUser? KilledPlayer { get; set; }
}
