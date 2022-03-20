using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class CitizenGroup : GroupRole
{
    public CitizenGroup(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles, options)
    {
        EarlyVotingTermination = true;
    }


    protected override IEnumerable<GameRole> GetVoters() => AliveRoles;

    public override async Task<VoteGroup> VoteManyAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultchannel = null)
    {
        await context.GuildData.GeneralTextChannel.SendMessageAsync($"{context.GuildData.MafiaRole.Mention} Голосование начинается...");

        await Task.Delay(3000);

        return await base.VoteManyAsync(context, context.GuildData.GeneralTextChannel, context.GuildData.GeneralTextChannel);
    }
}
