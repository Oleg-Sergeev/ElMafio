using System;
using Discord;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public abstract class Neutral : GameRole
{
    protected Neutral(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {
    }


    public override sealed void UpdateStats(MafiaStats stats, Winner winner)
        => throw new InvalidOperationException("Neutral roles cannot participate in game statistics because");
}
