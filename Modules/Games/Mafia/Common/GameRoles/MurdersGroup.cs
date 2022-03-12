using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles;

public class MurdersGroup : GroupRole
{
    protected int DiscussionTime { get; init; }


    public MurdersGroup(IReadOnlyList<Murder> roles, IOptionsSnapshot<GameRoleData> options) : base(roles, options)
    {
        DiscussionTime = Roles.Count * 20;

        AllowAnnonymousVoting = false;
    }


    public override async Task<VoteGroup> VoteManyAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultchannel = null)
    {
        await context.ChangeMurdersPermsAsync(MafiaHelper.AllowWrite, MafiaHelper.AllowSpeak);

        await context.GuildData.MurderTextChannel.SendEmbedAsync($"Время обсудить вашу следующую жертву. ({DiscussionTime}с)", EmbedStyle.Waiting);

        await Task.Delay(DiscussionTime * 1000);

        await context.GuildData.MurderTextChannel.SendEmbedAsync("Голосование начинается...", EmbedStyle.Waiting);

        await context.ChangeMurdersPermsAsync(MafiaHelper.DenyWrite, MafiaHelper.DenyView);

        return await base.VoteManyAsync(context, context.GuildData.MurderTextChannel, context.GuildData.MurderTextChannel);
    }
}