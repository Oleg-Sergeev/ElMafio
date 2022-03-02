using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
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

    public override async Task<VoteGroup> VoteManyAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultchannel = null, bool waitAfterVote = true)
    {
        await context.GuildData.GeneralTextChannel.SendEmbedAsync("Голосование начинается...", EmbedStyle.Waiting);

        await Task.Delay(3000);

        return await base.VoteManyAsync(context, context.GuildData.GeneralTextChannel, context.GuildData.GeneralTextChannel, waitAfterVote);
    }
}
