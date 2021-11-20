using System.Collections.Generic;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Sheriff : Innocent, IKiller
{
    public override bool CanDoMove => true;


    public IGuildUser? KilledPlayer { get; set; }

    protected int ShotsCount { get; set; }

    protected bool WasShot { get; set; }

    protected bool ShotSelected { get; set; }


    public Sheriff(IGuildUser player, IOptionsMonitor<GameRoleData> options, int voteTime, int maxShotsCount) : base(player, options, voteTime, true)
    {
        ShotsCount = maxShotsCount;
    }


    public override IEnumerable<IGuildUser> GetExceptList()
        => new List<IGuildUser>()
        {
            Player
        };


    public void ConfigureMove(bool shotSelected) => ShotSelected = shotSelected;

    public override void ProcessMove(IGuildUser? selectedPlayer, bool isSkip)
    {
        base.ProcessMove(selectedPlayer, isSkip);

        if (!isSkip && ShotSelected && ShotsCount > 0)
        {
            KilledPlayer = selectedPlayer;

            ShotSelected = false;
            ShotsCount--;
        }
    }
}
