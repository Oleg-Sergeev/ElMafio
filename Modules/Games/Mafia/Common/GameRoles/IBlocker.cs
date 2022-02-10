using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IBlocker
{
    IGuildUser? BlockedPlayer { get; }
}
