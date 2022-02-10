﻿using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;


public class Murder : GameRole, IKiller
{
    public IGuildUser? KilledPlayer { get; protected set; }


    public Murder(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {
    }


    public override void HandleChoice(IGuildUser? choice)
    {
        base.HandleChoice(choice);

        if (IsNight)
            KilledPlayer = choice;
    }
}
