using Discord;

namespace Modules.Games.Mafia.Common.Interfaces;

public interface IBlocker
{
    IGuildUser? BlockedPlayer { get; }
}
