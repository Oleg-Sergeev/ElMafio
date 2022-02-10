using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;

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




    public IReadOnlyList<Vote> Votes => _votes;
    public readonly List<Vote> _votes; // public!

    public int Priority { get; }


    public GameRole(IGuildUser player, IOptionsSnapshot<GameRoleData> options)
    {
        var name = GetType().Name;
        Data = options.Get(name);

        Name = Data.Name;

        Priority = Data.Priority;


        Player = player;

        IsAlive = true;

        _votes = new List<Vote>();
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

        if (isNight)
            UnblockAll();
    }


    protected static string ParsePattern(string str, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
            str = str.Replace($"{{{i}}}", $"**{values[i]}**");

        return str;
    }



    protected static void HandleChoiceInternal(GameRole role, IGuildUser? choice)
    {
        role.LastMove = choice;
    }

    // public ???
    public virtual void HandleChoice(IGuildUser? choice)
        => HandleChoiceInternal(this, choice);

    public virtual async Task SendVotingResultsAsync(IMessageChannel channel)
    {
        var seq = GetMoveResultPhasesSequence();

        foreach (var phrase in seq)
            await channel.SendEmbedAsync(phrase.Item2, phrase.Item1);
    }


    protected static async Task<Vote> VoteInternalAsync(GameRole role, MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null, bool waitAfterVote = true)
    {
        var token = context.MafiaData.TokenSource.Token;

        if (!role.IsAlive)
        {
            await Task.Delay(context.VoteTime * 1000, token);

            return new Vote(role, null, false);
        }


        if (voteChannel is null)
        {
            voteChannel = await role.Player.CreateDMChannelAsync();
            voteResultChannel = voteChannel;
        }

        if (role.BlockedByHooker)
        {
            await role.Player.SendMessageAsync(embed: EmbedHelper.CreateEmbed("Вас охмурила путана, развлекайтесь с ней", EmbedStyle.Warning));

            await Task.Delay(context.VoteTime * 1000, token);

            return new Vote(role, null, true);
        }


        var playersToVote = context.RolesData.AliveRoles.Keys.Except(role.GetExceptList());

        var cts = new CancellationTokenSource();

        var timeout = TimeSpan.FromSeconds(context.VoteTime);

        IGuildUser? selectedPlayer = null;

        IUserMessage? timeoutMessage = null;

        IUserMessage? message = null;

        Task? timeoutTask = null;

        Vote? vote = null;

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
                : $"Вы выбрали - {selectedPlayer.GetFullName()}";

            var embed = new EmbedBuilder()
               .WithTitle($"Голосование ({role.Player.GetFullName()})")
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
                var embedTimeout = EmbedHelper.CreateEmbed($"Осталось времени: {timeout.Minutes}м {timeout.Seconds}с", EmbedStyle.Waiting);

                timeoutMessage = await message.Channel.SendMessageAsync(embed: embedTimeout);

                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);

                timeoutTask = Task.Run(async () =>
                {
                    while (vote is null && timeout.TotalSeconds > 0)
                    {
                        await Task.Delay(5000, linkedCts.Token);

                        await timeoutMessage.ModifyAsync(msg =>
                        {
                            msg.Embed = EmbedHelper.CreateEmbed($"Осталось времени: {timeout.Minutes}м {timeout.Seconds}с", EmbedStyle.Waiting);
                        });

                        timeout -= TimeSpan.FromSeconds(5);
                    }
                }, linkedCts.Token);
            }


            var result = await context.Interactive.NextMessageComponentAsync(
                x => x.Message.Id == message.Id && (x.User.Id == role.Player.Id || x.User.Id == context.CommandContext.Guild.OwnerId),
                timeout: timeout, cancellationToken: token);



            if (result.IsSuccess)
            {
                var data = result.Value.Data;

                await result.Value.DeferAsync();

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
        while (timeout.TotalSeconds > 0);


        if (timeoutTask is not null)
        {
            cts.Cancel();

            try
            {
                await timeoutTask;
            }
            catch (OperationCanceledException) { }
        }

        await timeoutMessage.DeleteAsync();



        vote ??= new Vote(role, null, false);

        role.HandleChoice(vote.Option);


        if (voteResultChannel is not null)
            await role.SendVotingResultsAsync(voteResultChannel);


        role._votes.Add(vote);

        if (waitAfterVote && timeout.TotalSeconds > 0)
            await Task.Delay(timeout, token);


        return vote;
    }

    public virtual Task<Vote> VoteAsync(MafiaContext context, IMessageChannel? voteChannel = null, IMessageChannel? voteResultChannel = null, bool waitAfterVote = true)
        => VoteInternalAsync(this, context, voteChannel, voteResultChannel, waitAfterVote);





    public override string ToString() => Name;
}