using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;

public class MurdersGroup : RolesGroup
{
    public MurdersGroup(IReadOnlyList<Murder> roles, IOptionsSnapshot<GameRoleData> options) : base(roles, options)
    {

    }


    public async override Task<VoteGroup> VoteManyAsync(MafiaContext context, CancellationToken token, IUserMessage? message = null)
    {
        message ??= await context.GuildData.MurderTextChannel.SendEmbedAsync("Голосование начинается...", EmbedStyle.Waiting);

        await Task.Delay(3000);

        return await base.VoteManyAsync(context, token, message);
    }
}