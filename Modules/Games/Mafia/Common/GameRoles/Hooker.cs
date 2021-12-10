using System.Collections.Generic;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Hooker : Neutral, IHealer
{
    public IGuildUser? HealedPlayer { get; set; }

    public Hooker(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int voteTime) : base(player, options, voteTime)
    {
    }

    public override IEnumerable<IGuildUser> GetExceptList()
    {
        var except = new List<IGuildUser>()
        {
            Player
        };

        if (HealedPlayer is not null)
            except.Add(HealedPlayer);

        return except;
    }

    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        if (IsNight)
        {
            HealedPlayer = !isSkip ? selectedPlayer : null;

            IsNight = false;
        }
    }
}
