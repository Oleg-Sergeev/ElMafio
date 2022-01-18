﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;

public class CitizenGroup : GroupRole
{
    public CitizenGroup(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles, options)
    {
    }

    public override async Task<VoteGroup> VoteManyAsync(MafiaContext context, CancellationToken token, IMessageChannel? voteChannel = null, IMessageChannel? voteResultchannel = null)
    {
        await context.GuildData.GeneralTextChannel.SendEmbedAsync("Голосование начинается...", EmbedStyle.Waiting);

        await Task.Delay(3000);

        return await base.VoteManyAsync(context, token, context.GuildData.GeneralTextChannel, context.GuildData.GeneralTextChannel);
    }
}
