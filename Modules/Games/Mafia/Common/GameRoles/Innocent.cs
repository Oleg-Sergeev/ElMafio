using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class Innocent : GameRole
{
    public Innocent(IGuildUser player, IOptionsSnapshot<GameRoleData> options) : base(player, options)
    {

    }


    protected override IEnumerable<IGuildUser> GetExceptList()
    {
        if (!IsNight)
            return base.GetExceptList();

        var except = new List<IGuildUser>();

        if (LastMove is not null)
            except.Add(LastMove);

        return except;
    }



    public override async Task<Vote> VoteAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null, bool waitAfterVote = true)
    {
        if (GetType() != typeof(Innocent) || context.SettingsTemplate.RolesExtraInfoSubSettings.CanInnocentsKillAtNight)
            return await base.VoteAsync(context, voteChannel, voteResultChannel, waitAfterVote);

        await Task.Delay(context.VoteTime * 1000, context.MafiaData.TokenSource.Token);

        return new Vote(this, null, true);
    }
}
