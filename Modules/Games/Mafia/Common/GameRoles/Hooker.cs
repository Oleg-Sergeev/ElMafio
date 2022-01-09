using System.Collections.Generic;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Hooker : Neutral, IHooker
{
    public IGuildUser? HealedPlayer { get; protected set; }

    public IGuildUser? BlockedPlayer { get; protected set; }


    public Hooker(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {
    }


    protected override IEnumerable<IGuildUser> GetExceptList()
    {
        if (!IsNight)
            return base.GetExceptList();


        var except = new List<IGuildUser>()
        {
            Player
        };

        if (HealedPlayer is not null)
            except.Add(HealedPlayer);

        return except;
    }


    protected override void HandleChoice(IGuildUser? choice)
    {
        base.HandleChoice(choice);


        if (IsNight)
        {
            HealedPlayer = choice;

            BlockedPlayer = HealedPlayer;
        }
    }

    // ***************
    public override void Block(IBlocker byRole)
    {
        if (byRole is Hooker hooker)
        {
            hooker.HandleChoice(null);
        }
        else
        {
            base.Block(byRole);
        }
    }
}
