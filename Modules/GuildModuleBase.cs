using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Net;
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


    public async Task<(T?, bool)> WaitForVotingAsync<T>(IMessageChannel channel, int voteTime, IList<T> options, IList<string>? displayOptions = null, CancellationToken? token = null)
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

        var listToDisplay = displayOptions is null ? options.Select(o => o?.ToString() ?? "NULL").ToList() : displayOptions;

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


    public async Task BroadcastMessagesAsync(
        IEnumerable<IMessageChannel> channels,
        string? text = null,
        bool isTTS = false,
        Embed? embed = null,
        RequestOptions? options = null,
        AllowedMentions? mentions = null,
        MessageReference? reference = null)
    {
        foreach (var channel in channels)
        {
            try
            {
                await channel.SendMessageAsync(text, isTTS, embed, options, mentions, reference);
            }
            catch (HttpException e)
            {
                await ReplyAsync($"Не удалось отправить сообщение в канал {channel.Name}. Причина: {e.Reason}");
            }
        }
    }


    public async Task ReplyEmbedAsync(
        EmbedType embedType,
        string description,
        bool addSmilesToDescription = true,
        string? title = null,
        EmbedFooterBuilder? embedFooter = null,
        EmbedAuthorBuilder? embedAuthor = null,
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
            embedBuilder = embedBuilder.WithTitle(title);

        if (embedFooter is not null)
            embedBuilder.WithFooter(embedFooter);

        if (embedAuthor is not null)
            embedBuilder.WithAuthor(embedAuthor);


        await ReplyAsync(embed: embedBuilder.Build());
    }
}
