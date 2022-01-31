using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Newtonsoft.Json.Linq;

namespace Modules.Games.Mafia.Common.GameRoles.RolesGroups;


public abstract class GroupRole : GameRole
{
    protected IReadOnlyList<GameRole> Roles { get; }

    protected IEnumerable<GameRole> AliveRoles => Roles.Where(r => r.IsAlive);


    public GroupRole(IReadOnlyList<GameRole> roles, IOptionsSnapshot<GameRoleData> options) : base(roles[0].Player, options)
    {
        Roles = roles;
    }



    public virtual async Task<VoteGroup> VoteManyAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null, bool waitAfterVote = true)
    {
        var votes = new Dictionary<IGuildUser, Vote>();

        if (voteChannel is null)
        {
            voteChannel = await Player.CreateDMChannelAsync();
            voteResultChannel = voteChannel;
        }

        var voters = GetVoters();
        var playersVotes = new Dictionary<IGuildUser, Vote?>();
        //var votesCount = new Dictionary<IGuildUser, int>();


        foreach (var voter in voters)
            playersVotes.Add(voter.Player, null);

        var playersNames = voters.Select(p => p.Player.GetFullName());


        var voteProgressEmbed = GetVotingInfoEmbed();

        var voteProgressMsg = await voteChannel.SendMessageAsync(embed: voteProgressEmbed);


        var entryVoteComponent = new ComponentBuilder()
            .WithButton("Голосовать", "vote")
            .Build();

        var entryVoteMsg = await voteChannel.SendMessageAsync(embed: EmbedHelper.CreateEmbed("Для участия в голосовании нажмите на кнопку ниже"),
            components: entryVoteComponent);


        var roles = voters.ToDictionary(p => p.Player.Id);

        var timeout = TimeSpan.FromSeconds(context.VoteTime);

        var token = context.MafiaData.TokenSource.Token;

        var tasks = new List<Task>
        {
            Task.Run(async () =>
            {
                var embedTimeout = EmbedHelper.CreateEmbed($"Осталось времени: {timeout.Minutes}м {timeout.Seconds}с", EmbedStyle.Waiting);

                var timeoutMessage = await voteChannel.SendMessageAsync(embed: embedTimeout);

                try
                {
                    while (timeout.TotalSeconds > 0)
                    {
                        await Task.Delay(5000, token);

                        timeout -= TimeSpan.FromSeconds(5);

                        await timeoutMessage.ModifyAsync(msg =>
                        {
                            msg.Embed = EmbedHelper.CreateEmbed($"Осталось времени: {timeout.Minutes}м {timeout.Seconds}с", EmbedStyle.Waiting);
                        });
                    }
                }
                finally
                {
                    await timeoutMessage.DeleteAsync();
                }
            }, token)
        };



        while (roles.Count > 0 && timeout.TotalSeconds > 0 && !token.IsCancellationRequested)
        {
            var res = await context.Interactive.NextMessageComponentAsync(m => m.Message.Id == entryVoteMsg.Id && roles.ContainsKey(m.User.Id),
                timeout: timeout,
                cancellationToken: token);


            if (res.IsSuccess)
            {
                var interaction = res.Value;

                var role = roles[res.Value.User.Id];

                roles.Remove(interaction.User.Id);


                tasks.Add(Task.Run(async () =>
                {
                    var vote = await HandleVotingAsync(interaction, role);

                    playersVotes[vote.VotedRole.Player] = vote;

                    votes[vote.VotedRole.Player] = vote;


                    var voteProgressEmbed = GetVotingInfoEmbed();

                    await voteProgressMsg.ModifyAsync(x =>
                    {
                        x.Embed = voteProgressEmbed;
                    });

                    if (vote.IsSkip)
                        await interaction.FollowupAsync(embed: EmbedHelper.CreateEmbed("Вы пропустили голосование", EmbedStyle.Successfull),
                            ephemeral: true);
                    else if (vote.Option is not null)
                        await interaction.FollowupAsync(embed: EmbedHelper.CreateEmbed($"Вы проголосовали за {vote.Option.GetFullMention()}", EmbedStyle.Successfull),
                            ephemeral: true);
                    else
                        await interaction.FollowupAsync(embed: EmbedHelper.CreateEmbed("Вы не смогли принять решение", EmbedStyle.Warning),
                            ephemeral: true);
                }));
            }
            else if (res.IsTimeout)
                break;
        }


        try
        {
            await Task.WhenAll(tasks);
        }
        catch (AggregateException ae)
        {
            var str = string.Join('\n', ae.InnerExceptions.Select(e => $"{e.Message} -> {e.InnerException?.Message ?? "No inner"}"));
            await context.CommandContext.Channel.SendEmbedAsync(str, EmbedStyle.Error);

            throw;
        }

        foreach (var role in roles.Values)
        {
            role.HandleChoice(null);

            var vote = new Vote(role, null, false);

            votes.Add(role.Player, vote);

            role._votes.Add(vote);
        }

        var voteGroup = new VoteGroup(this, votes);


        base.HandleChoice(voteGroup.Choice.Option);

        if (voteResultChannel is not null)
            await SendVotingResultsAsync(voteResultChannel);


        return voteGroup;



        async Task<Vote> HandleVotingAsync(SocketMessageComponent interaction, GameRole role)
        {
            await interaction.DeferAsync(true);


            IGuildUser? selectedPlayer = null;

            IUserMessage? msg = null;

            Vote? vote = null;

            var embed = new EmbedBuilder()
                .WithDescription("Выберите игрока из списка")
                .WithColor(Color.Gold)
                .Build();

            while (timeout.TotalSeconds > 0)
            {
                var playersToVote = context.RolesData.AliveRoles.Keys.Except(GetExceptList());

                var options = playersToVote
                    .Select(p => new SelectMenuOptionBuilder(p.GetFullName(), p.Id.ToString(), isDefault: selectedPlayer?.Id == p.Id))
                    .ToList();

                var select = new SelectMenuBuilder()
                    .WithCustomId("select")
                    .WithPlaceholder("Выберите игрока")
                    .WithOptions(options);

                var component = new ComponentBuilder()
                       .WithSelectMenu(select)
                       .WithButton("Проголосовать", "vote")
                       .WithButton("Пропустить", "skip", ButtonStyle.Danger)
                       .Build();

                if (msg is null)
                    msg = await interaction.FollowupAsync(embed: embed, components: component, ephemeral: true);
                else
                    await msg.ModifyAsync(x =>
                    {
                        x.Embed = embed;
                        x.Components = component;
                        x.Embeds = Array.Empty<Embed>();
                    });

                var result = await context.Interactive.NextMessageComponentAsync(
                    x => x.Message.Id == msg.Id && (x.User.Id == role.Player.Id || x.User.Id == context.CommandContext.Guild.OwnerId),
                    timeout: timeout, cancellationToken: token);

                if (result.IsSuccess)
                {
                    var data = result.Value.Data;

                    await result.Value.DeferAsync(true);

                    if (data.Type == ComponentType.Button)
                    {
                        if (data.CustomId == "skip")
                        {
                            vote = new Vote(role, null, true);

                            break;
                        }
                        else if (data.CustomId == "vote")
                        {
                            vote = new Vote(role, selectedPlayer, false);

                            break;
                        }
                        else
                        {
                            throw new Exception("so bad");
                        }
                    }
                    else if (data.Values.Count > 0)
                    {
                        var value = data.Values.First();

                        if (!ulong.TryParse(value, out var id))
                            throw new FormatException($"Failed to parse player id. Data has value: {value}");

                        selectedPlayer = playersToVote.First(p => p.Id == id);
                    }
                    else
                    {
                        throw new Exception("bad");
                    }
                }
                else
                {
                    vote = new Vote(role, null, false);

                    break;
                }
            }

            vote ??= new Vote(role, null, false);

            role.HandleChoice(vote.Option);


            role._votes.Add(vote);

            return vote;
        }


        Embed GetVotingInfoEmbed()
            => new EmbedBuilder()
            .WithTitle($"Дневное голосование #")
            .AddField("Игрок", string.Join('\n', playersNames), true)
            .AddField("Голос", string.Join('\n', playersVotes.Values.Select(v => v is null ? "None" : (v.IsSkip ? "Skip" : v.Option?.GetFullName() ?? "None"))), true)
            .Build();
    }


    protected virtual IEnumerable<GameRole> GetVoters() => AliveRoles.Where(r => !r.BlockedByHooker);


    public override async Task<Vote> VoteAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null, bool waitAfterVote = true)
        => (await VoteManyAsync(context, voteChannel, voteResultChannel, waitAfterVote)).Choice;

    public override void HandleChoice(IGuildUser? choice)
    {
        foreach (var role in AliveRoles)
            HandleChoiceInternal(role, choice);
    }
}
