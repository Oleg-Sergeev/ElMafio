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


public abstract class RolesGroup : GameRole
{
    protected IReadOnlyList<GameRole> Roles { get; }


    public RolesGroup(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles[0].Player, options)
    {
        Roles = roles;
    }



    public virtual async Task<VoteGroup> VoteManyAsync(MafiaContext context, CancellationToken token, IUserMessage? voteMessage = null)
    {
        var votes = new Dictionary<IGuildUser, Vote>();

        foreach (var role in Roles)
        {
            var vote = await role.VoteAsync(context, token, voteMessage);

            votes[vote.VotedRole.Player] = vote;
        }

        var votingResult = new VoteGroup(this, votes);

        return votingResult;
    }


    public async override Task<Vote> VoteAsync(MafiaContext context, CancellationToken token, IUserMessage? message = null)
        => (await VoteManyAsync(context, token, message)).Choice;

    protected override void HandleChoice(IGuildUser? choice)
    {
        foreach (var role in Roles)
            HandleChoiceInternal(role, choice);
    }


    public override async Task SendVotingResults()
    {
        var seq = GetMoveResultPhasesSequence();

        foreach (var phrase in seq)
        {
            foreach (var role in Roles)
            {
                await role.Player.SendMessageAsync(embed: EmbedHelper.CreateEmbed(phrase.Item2, phrase.Item1));
            }
        }
    }
}
