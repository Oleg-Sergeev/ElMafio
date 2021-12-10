using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public abstract class Neutral : GameRole
{
    protected Neutral(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int voteTime) : base(player, options, voteTime)
    {
    }
}
