using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Net;
using Discord.WebSocket;
using Modules.EnsureCriterias;
using Modules.Extensions;

namespace Modules;

public abstract class GuildModuleBase : InteractiveBase
{
    protected const int VotingOptionsMaxCount = 35;


    public async Task WaitTimerAsync(int seconds, params IMessageChannel[] channels)
        => await WaitTimerAsync(seconds, null, channels);

    public async Task WaitTimerAsync(int seconds, CancellationToken? cancellationToken, params IMessageChannel[] channels)
    {
        if (seconds >= 20)
        {
            if (seconds >= 40)
            {
                seconds /= 2;

                if (cancellationToken is not null)
                    await Task.Delay(seconds * 1000, cancellationToken.Value);
                else
                    await Task.Delay(seconds * 1000);

                await BroadcastMessagesAsync(channels, $"Осталось {seconds}с!");

                if (cancellationToken is not null)
                    await Task.Delay((seconds - 10) * 1000, cancellationToken.Value);
                else
                    await Task.Delay((seconds - 10) * 1000);
            }
            else
            {
                if (cancellationToken is not null)
                    await Task.Delay((seconds - 10) * 1000, cancellationToken.Value);
                else
                    await Task.Delay((seconds - 10) * 1000);
            }

            seconds -= seconds - 10;

            await BroadcastMessagesAsync(channels, $"Осталось {seconds}с!");
        }

        await Task.Delay(seconds * 1000);
    }


    public async Task<(T?, bool)> WaitForVotingAsync<T>(IMessageChannel channel, int voteTime, IList<T> options, IList<string>? displayOptions = null, CancellationToken? token = null) where T : notnull
    {
        if (options.Count == 0)
        {
            await channel.SendMessageAsync("Получен пустой список вариантов");

            return default;
        }

        if (options.Count > VotingOptionsMaxCount)
        {
            await channel.SendMessageAsync($"Превышен лимит вариантов голосования" +
                $"\nПолучено – **{options.Count}** вариантов; лимит – **{VotingOptionsMaxCount}** вариантов" +
                $"\nНа голосовании будут показаны только первые {VotingOptionsMaxCount} вариантов");
        }


        const int ZeroASCII = 48;
        const int ASymbolASCII = 65;

        var text = "";
        var emojis = new Emoji[options.Count + 1];

        var listToDisplay = displayOptions is null ? options.Select(o => o.ToString()).ToList()! : displayOptions;

        var digitVotesCount = Math.Min(options.Count, 9);

        for (int i = 0; i < digitVotesCount; i++)
        {
            var symbol = (char)(1 + i + ZeroASCII);

            text += $"{symbol} - {listToDisplay[i]}\n";

            var emoji = new Emoji(symbol.ConvertToSmile());
            emojis[i] = emoji;
        }

        for (int i = digitVotesCount; i < Math.Min(options.Count, VotingOptionsMaxCount); i++)
        {
            var symbol = (char)(i + ASymbolASCII - digitVotesCount);

            text += $"{symbol} - {listToDisplay[i]}\n";

            var emoji = new Emoji(symbol.ConvertToSmile());
            emojis[i] = emoji;
        }

        text += $"0 - Пропустить голосование\n";
        emojis[options.Count] = new Emoji(0.ConvertToSmile());

        var votingMessage = await channel.SendMessageAsync(text);

        await votingMessage.AddReactionsAsync(emojis);


        await WaitTimerAsync(voteTime, token, channel);


        votingMessage = (IUserMessage)await channel.GetMessageAsync(votingMessage.Id);

        var maxCount1 = 0;
        var maxCount2 = 0;

        var maxIndex = 0;
        var index = 0;
        foreach (var reaction in votingMessage.Reactions.Values)
        {
            var reactionCount = reaction.ReactionCount;

            if (reactionCount > maxCount1)
            {
                maxCount2 = maxCount1;
                maxCount1 = reactionCount;

                maxIndex = index;
            }
            else if (reactionCount > maxCount2)
            {
                maxCount2 = reactionCount;
            }

            index++;
        }

        var selectedUser =
            maxCount1 > maxCount2 && maxIndex < options.Count
            ? options[maxIndex]
            : default;

        return (selectedUser, maxIndex == options.Count);
    }


    public async Task<IEnumerable<IUserMessage>> BroadcastMessagesAsync(
        IEnumerable<IMessageChannel> channels,
        string? text = null,
        bool isTTS = false,
        Embed? embed = null,
        RequestOptions? options = null,
        AllowedMentions? mentions = null,
        MessageReference? reference = null)
    {
        var messages = new List<IUserMessage>();
        foreach (var channel in channels)
        {
            try
            {
                messages.Add(await channel.SendMessageAsync(text, isTTS, embed, options, mentions, reference));
            }
            catch (HttpException e)
            {
                await ReplyAsync($"Не удалось отправить сообщение в канал {channel.Name}. Причина: {e.Reason}");
            }
        }

        return messages;
    }


    public async Task<IUserMessage> ReplyEmbedAsync(
        EmbedType embedType,
        string description,
        bool addSmilesToDescription = true,
        string? title = null,
        bool withDefaultFooter = false,
        bool withDefaultAuthor = false,
        EmbedBuilder? embedBuilder = null)
    {
        embedBuilder ??= new EmbedBuilder();

        embedBuilder.WithDescription(description);

        embedBuilder = embedType switch
        {
            EmbedType.Error => embedBuilder.WithErrorMessage(addSmilesToDescription),
            EmbedType.Warning => embedBuilder.WithWarningMessage(addSmilesToDescription),
            EmbedType.Successfull => embedBuilder.WithSuccessfullyMessage(addSmilesToDescription),
            _ => embedBuilder.WithInformationMessage(addSmilesToDescription)
        };

        if (title is not null)
            embedBuilder.WithTitle(title);

        if (withDefaultFooter)
            embedBuilder
                .WithCurrentTimestamp()
                .WithUserInfoFooter(Context.User);

        if (withDefaultAuthor)
            embedBuilder.WithAuthor(Context.User);


        return await ReplyAsync(embed: embedBuilder.Build());
    }

    public async Task<IUserMessage> ReplyEmbedAndDeleteAsync(
        EmbedType embedType,
        string description,
        bool addSmilesToDescription = true,
        string? title = null,
        bool withDefaultFooter = false,
        bool withDefaultAuthor = false,
        EmbedBuilder? embedBuilder = null,
        TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(embedType, description, addSmilesToDescription, title, withDefaultFooter, withDefaultAuthor, embedBuilder);

        _ = Task.Run(async () =>
        {
            await Task.Delay(timeout ?? TimeSpan.FromSeconds(10));

            await msg.DeleteAsync();
        });

        return msg;
    }


    public async Task<bool?> ConfirmActionAsync(string title, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(EmbedType.Information, "Подтвердите действие", false, title, true);

        var res = await ConfirmActionAsync(msg, timeout);

        return res;
    }

    public async Task<bool?> ConfirmActionAsync(IUserMessage message, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(15);

        var emotes = new IEmote[] { new Emoji("✅"), new Emoji("❌") };

        var criterion = new Criteria<SocketReaction>()
            .AddCriterion(new EnsureReactionFromSourceUserCriterion())
            .AddCriterion(new EnsureReactionFromMessageCriterion(message));

        var eventTrigger = new TaskCompletionSource<SocketReaction>();


        Context.Client.ReactionAdded += Handler;

        await message.AddReactionsAsync(emotes);

        var trigger = eventTrigger.Task;
        var delay = Task.Delay(timeout.Value);
        var task = await Task.WhenAny(trigger, delay);

        Context.Client.ReactionAdded -= Handler;


        await message.DeleteAsync();

        if (task == trigger)
        {
            var reaction = await trigger;

            if (reaction.Emote.Name == emotes[0].Name)
                return true;

            if (reaction.Emote.Name == emotes[1].Name)
                return false;
        }


        return null;



        async Task Handler(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (await criterion.JudgeAsync(Context, reaction))
                eventTrigger.SetResult(reaction);
        }
    }



    public async Task<bool> ConfirmActionWithHandlingAsync(string title, ulong? logChannelId = null, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(EmbedType.Information, "Подтвердите действие", false, title, true);

        return await ConfirmActionWithHandlingAsync(msg, logChannelId, timeout);
    }
    public async Task<bool> ConfirmActionWithHandlingAsync(IUserMessage message, ulong? logChannelId = null, TimeSpan? timeout = null)
    {
        var confirmed = await ConfirmActionAsync(message, timeout);

        if (confirmed is null)
        {
            await ReplyEmbedAndDeleteAsync(EmbedType.Warning, "Вы не подтвердили действие", true, "Сброс рейтинга", true);

            return false;
        }
        else if (confirmed is false)
        {
            await ReplyEmbedAndDeleteAsync(EmbedType.Error, "Вы отклонили действие", true, "Сброс рейтинга", true);

            return false;
        }

        var embed = (Embed)message.Embeds.First();

        message = await ReplyEmbedAndDeleteAsync(EmbedType.Successfull, "Вы подтвердили действие", true, embed.Title, true);

        embed = (Embed)message.Embeds.First();

        if (logChannelId is not null)
        {
            var logChannel = Context.Guild.GetTextChannel(logChannelId.Value);

            if (logChannel is not null)
            {
                await logChannel.SendMessageAsync(
                        $"{Context.Guild.EveryoneRole.Mention} Пользователь **{Context.User.GetFullName()}** выполнил команду **{embed.Title}**", embed: embed);
            }
        }

        return true;
    }
}