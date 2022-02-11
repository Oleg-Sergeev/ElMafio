using Discord;

namespace Modules.Games.Mafia.Common.GameRoles;

public interface IHealer
{
    int HealsCount { get; set; }

    int MovesCount { get; set; }

    bool IsSkip { get; }
    IGuildUser? HealedPlayer { get; }
}
