using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IHealer : IActiveRole
{
    int HealsCount { get; set; }

    IGuildUser? HealedPlayer { get; }
}
