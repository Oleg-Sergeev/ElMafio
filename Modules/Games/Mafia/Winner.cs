using System.Collections.Generic;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia;

public class Winner
{
    public static readonly Winner None = new(null);


    public GameRole? Role { get; }

    public ICollection<ulong>? PlayersIds { get; }


    public Winner(GameRole? role, ICollection<ulong>? playersIds = null)
    {
        Role = role;
        PlayersIds = playersIds;
    }
}