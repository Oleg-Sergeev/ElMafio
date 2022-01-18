using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Fergun.Interactive.Pagination;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Interfaces;

namespace Modules.Games.Mafia.Common.GameRoles;



public abstract class GameRole
{
    protected const string YourMovePhrases = "YourMove";
    protected const string SuccessPhrases = "Success";
    protected const string FailurePhrases = "Failure";

    protected static readonly Random _random = new();


    public virtual IGuildUser Player { get; }

    public virtual IGuildUser? LastMove { get; protected set; }


    public virtual string Name { get; }

    public virtual bool IsAlive { get; protected set; }

    public virtual bool BlockedByHooker { get; protected set; }

    public virtual bool IsNight { get; protected set; }


    public bool IsSkip { get; protected set; }

    protected GameRoleData Data { get; }




    public List<Vote> Votes { get; }

    public int Priority { get; }


    public GameRole(IGuildUser player, IOptionsSnapshot<GameRoleData> options)
    {
        var name = GetType().Name;
        Data = options.Get(name);

        Name = Data.Name;

        Priority = Data.Priority;


        Player = player;

        IsAlive = true;

        Votes = new();
    }



    protected virtual IEnumerable<IGuildUser> GetExceptList()
        => Enumerable.Empty<IGuildUser>();



    public virtual ICollection<(EmbedStyle, string)> GetMoveResultPhasesSequence()
    {
        var sequence = new List<(EmbedStyle, string)>();

        if (LastMove is not null)
        {
            sequence.Add((EmbedStyle.Successfull, "Выбор сделан"));
            sequence.Add((EmbedStyle.Successfull, ParsePattern(GetRandomPhrase(Data.Phrases[SuccessPhrases]), $"**{LastMove?.GetFullName()}**")));
        }
        else
        {
            if (IsSkip)
                sequence.Add((EmbedStyle.Error, "Вы пропустили голосование"));
            else
            {
                sequence.Add((EmbedStyle.Error, "Вы не смогли сделать выбор"));
                sequence.Add((EmbedStyle.Error, ParsePattern(GetRandomPhrase(Data.Phrases[FailurePhrases]))));
            }
        }

        return sequence;
    }

    public string GetRandomYourMovePhrase() => GetRandomPhrase(Data.Phrases[YourMovePhrases]);

    protected static string GetRandomPhrase(string[] phrases) => phrases[_random.Next(phrases.Length)];

    public virtual void GameOver()
    {
        IsAlive = false;
    }


    public virtual void Block(IBlocker byRole)
    {
        switch (byRole)
        {
            case Hooker:
                BlockedByHooker = true;
                break;
        }
    }

    public virtual void UnblockAll()
    {
        BlockedByHooker = false;
    }

    public virtual void SetPhase(bool isNight)
    {
        IsNight = isNight;
    }


    protected static string ParsePattern(string str, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
            str = str.Replace($"{{{i}}}", $"**{values[i]}**");

        return str;
    }



    protected static void HandleChoiceInternal(GameRole role, IGuildUser? choice)
        => role.HandleChoice(choice);

    protected virtual void HandleChoice(IGuildUser? choice)
    {
        LastMove = choice;
    }

    protected virtual async Task SendVotingResultsAsync(IMessageChannel channel)
    {
        var seq = GetMoveResultPhasesSequence();

        foreach (var phrase in seq)
            await channel.SendEmbedAsync(phrase.Item2, phrase.Item1);
    }


    public virtual async Task<Vote> VoteAsync(MafiaContext context, CancellationToken token, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null)
    {
        // Preconditions

        var except = GetExceptList();

        var playersToVote = context.RolesData.AliveRoles.Keys.Except(except);

        var timeout = TimeSpan.FromSeconds(context.VoteTime);


        IGuildUser? selectedPlayer = null;

        IUserMessage? timeoutMessage = null;

        IUserMessage? message = null;

        Task? timeoutTask = null;

        Vote? vote = null;

        if (voteChannel is null)
        {
            voteChannel = await Player.CreateDMChannelAsync();
            voteResultChannel = voteChannel;
        }

        do
        {
            var options = playersToVote
                .Select(p => new SelectMenuOptionBuilder(p.GetFullName(), p.Id.ToString(), isDefault: selectedPlayer?.Id == p.Id))
                .ToList();

            var select = new SelectMenuBuilder()
                .WithCustomId("select")
                .WithPlaceholder("Выберите игрока")
                .WithOptions(options);

            var component = new ComponentBuilder()
                   .WithButton("Проголосовать", "vote", ButtonStyle.Primary, disabled: selectedPlayer is null)
                   .WithButton("Пропустить", "skip", ButtonStyle.Danger)
                   .WithSelectMenu(select, 1)
                   .Build();

            var description = selectedPlayer is null
                ? "Выберите человека из списка"
                : $"Вы выбрали - {selectedPlayer.GetFullMention()}";

            var embed = new EmbedBuilder()
               .WithTitle($"Голосование: {Player.GetFullName()}")
               .WithColor(Color.Gold)
               .WithDescription(description)
               .AddField("Игрок", string.Join('\n', playersToVote.Select(p => p.GetFullName())), true)
               .Build();

            if (message is null)
            {
                message = await voteChannel.SendMessageAsync(embed: embed, components: component);
            }
            else
            {
                await message.ModifyAsync(x =>
                {
                    x.Embed = embed;
                    x.Components = component;
                    x.Embeds = Array.Empty<Embed>();
                });
            }

            if (timeoutMessage is null)
            {
                var ts = timeout;

                var embedTimeout = EmbedHelper.CreateEmbed($"Осталось времени: {ts.Minutes}м {ts.Seconds}с", EmbedStyle.Waiting);

                timeoutMessage = await message.Channel.SendMessageAsync(embed: embedTimeout);


                timeoutTask = Task.Run(async () =>
                {
                    while (vote is null && timeout.TotalSeconds > 0)
                    {
                        await timeoutMessage.ModifyAsync(msg =>
                        {
                            msg.Embed = EmbedHelper.CreateEmbed($"Осталось времени: {ts.Minutes}м {ts.Seconds}с", EmbedStyle.Waiting);
                        });

                        await Task.Delay(5000);

                        ts -= TimeSpan.FromSeconds(5);
                    }
                });
            }


            var result = await context.Interactive.NextMessageComponentAsync(
                x => x.Message.Id == message.Id && (x.User.Id == Player.Id || x.User.Id == context.CommandContext.Guild.OwnerId),
                timeout: timeout, cancellationToken: token);



            if (result.IsSuccess)
            {
                var data = result.Value.Data;

                await result.Value.DeferAsync(true);

                if (data.Type == ComponentType.Button)
                {
                    if (data.CustomId == "skip")
                    {
                        vote = new Vote(this, null, true);

                        break;
                    }
                    else if (data.CustomId == "vote")
                    {
                        vote = new Vote(this, selectedPlayer, false);

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
                vote = new Vote(this, null, false);

                break;
            }
        }
        while (timeout.TotalSeconds > 0);


        if (timeoutTask is not null)
            await timeoutTask;

        await timeoutMessage.DeleteAsync();

        vote ??= new Vote(this, null, false);

        HandleChoice(vote.Option);


        if (voteResultChannel is not null)
            await SendVotingResultsAsync(voteResultChannel);


        Votes.Add(vote);

        return vote;
    }
}
