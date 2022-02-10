using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IHealer
{
    IGuildUser? HealedPlayer { get; }
}
