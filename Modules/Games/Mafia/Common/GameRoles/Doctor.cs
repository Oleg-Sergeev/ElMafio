using System.Collections.Generic;
using Discord;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Doctor : Innocent, IHealer
{
    public int HealsCount { get; set; }

    public int MovesCount { get; set; }

    public IGuildUser? HealedPlayer { get; protected set; }

    protected int SelfHealsCount { get; set; }


    public Doctor(IGuildUser player, IOptionsSnapshot<GameRoleData> options, int selfHealsCount) : base(player, options)
    {
        SelfHealsCount = selfHealsCount;
    }


    protected override IEnumerable<IGuildUser> GetExceptList()
    {
        if (!IsNight)
            return base.GetExceptList();

        var except = new List<IGuildUser>();

        if (HealedPlayer is not null)
            except.Add(HealedPlayer);

        if (SelfHealsCount == 0)
            except.Add(Player);

        return except;
    }


    public override void HandleChoice(IGuildUser? choice)
    {
        base.HandleChoice(choice);

        if (IsNight)
        {
            HealedPlayer = choice;

            if (HealedPlayer == Player)
                SelfHealsCount--;
        }
    }


    public override void UpdateStats(MafiaStats stats, Winner winner)
    {
        base.UpdateStats(stats, winner);

        stats.DoctorMovesCount += MovesCount;
        stats.DoctorHealsCount += HealsCount;
    }
}
