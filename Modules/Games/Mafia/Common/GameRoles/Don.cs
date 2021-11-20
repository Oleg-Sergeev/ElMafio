using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Don : Murder
{
    public Don(IGuildUser player, IOptionsMonitor<GameRoleData> options, int voteTime) : base(player, options, voteTime)
    {
    }
}
