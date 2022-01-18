using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;


public abstract class GroupRole : GameRole
{
    protected IReadOnlyList<GameRole> Roles { get; }

    protected IEnumerable<GameRole> AliveRoles => Roles.Where(r => r.IsAlive);


    public GroupRole(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles[0].Player, options)
    {
        Roles = roles;
    }



    public virtual async Task<VoteGroup> VoteManyAsync(MafiaContext context, CancellationToken token, IMessageChannel? voteChannel = null, IMessageChannel? voteResultchannel = null)
    {
        var votes = new Dictionary<IGuildUser, Vote>();

        foreach (var role in AliveRoles)
        {
            var vote = await role.VoteAsync(context, token, voteChannel);

            votes[vote.VotedRole.Player] = vote;
        }

        var voteGroup = new VoteGroup(this, votes);


        base.HandleChoice(voteGroup.Choice.Option);

        if (voteResultchannel is not null)
            await SendVotingResultsAsync(voteResultchannel);


        return voteGroup;
    }


    public override async Task<Vote> VoteAsync(MafiaContext context, CancellationToken token, IMessageChannel? voteChannel = null, IMessageChannel? voteResultchannel = null)
        => (await VoteManyAsync(context, token, voteChannel, voteResultchannel)).Choice;

    protected override void HandleChoice(IGuildUser? choice)
    {
        foreach (var role in AliveRoles)
            HandleChoiceInternal(role, choice);
    }
}
