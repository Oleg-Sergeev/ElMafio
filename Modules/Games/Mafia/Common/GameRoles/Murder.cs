﻿using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Murder : GameRole, IKiller
{
    public IGuildUser? KilledPlayer { get; set; }


    public Murder(IGuildUser player, IOptionsMonitor<GameRoleData> options, int voteTime) : base(player, options, voteTime)
    {
    }

    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        KilledPlayer = !isSkip ? selectedPlayer : null;
    }
}
