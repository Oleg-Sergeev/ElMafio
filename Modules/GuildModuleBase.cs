﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using Serilog;
using Services;

namespace Modules;

public abstract class GuildModuleBase : ModuleBase<DbSocketCommandContext>
{
    protected const string LogTemplate = "({Context:l}): {Message}";

    protected const int VotingOptionsMaxCount = 19;


    protected static readonly Random Random = new();


    protected static readonly IEmote ConfirmEmote = new Emoji("✅");
    protected static readonly IEmote DenyEmote = new Emoji("❌");
    protected static readonly IEmote CancelEmote = new Emoji("⏹️");


    private ILogger? _guildLogger;
    protected ILogger GuildLogger => _guildLogger ??= GetGuildLogger(Context.Guild.Id);


    protected InteractiveService Interactive { get; }


    protected GuildModuleBase(InteractiveService interactiveService)
    {
        Interactive = interactiveService;
    }


    protected static ILogger GetGuildLogger(ulong guildId)
        => Log.ForContext("GuildName", guildId);

    protected static IList<IEmote> GetEmotesList(int count)
        => GetEmotesList(count, null, out _);
    protected static IList<IEmote> GetEmotesList(int count, IList<string>? emoteAssociations, out string emoteAssociationsText)
    {
        emoteAssociationsText = "";

        var emotes = new List<IEmote>();

        const int ASymbolASCII = 65;

        for (int i = 0; i < count; i++)
        {
            var symbol = (char)(ASymbolASCII + i);

            emotes.Add(new Emoji(symbol.ConvertToSmile()));

            if (emoteAssociations is not null && emoteAssociations.Count > i)
                emoteAssociationsText += $"{symbol} - {emoteAssociations[i]}\n";
        }

        return emotes;
    }


    public async Task WaitForTimerAsync(int seconds, params IMessageChannel[] channels)
        => await WaitForTimerAsync(seconds, default, channels);
    public async Task WaitForTimerAsync(int seconds, CancellationToken cancellationToken, params IMessageChannel[] channels)
    {
        if (seconds >= 20)
        {
            if (seconds >= 40)
            {
                seconds /= 2;

                await Task.Delay(seconds * 1000, cancellationToken);

                await BroadcastMessagesAsync(channels, $"Осталось {seconds}с!");

                await Task.Delay((seconds - 10) * 1000, cancellationToken);
            }
            else
            {
                await Task.Delay((seconds - 10) * 1000, cancellationToken);
            }

            seconds -= seconds - 10;

            await BroadcastMessagesAsync(channels, $"Осталось {seconds}с!");
        }

        await Task.Delay(seconds * 1000, cancellationToken);
    }



    public async Task<(T?, bool)> WaitForVotingAsync<T>(IMessageChannel channel, int voteTime, IList<T> options, IList<string>? displayOptions = null, CancellationToken token = default) where T : notnull
    {
        if (options.Count == 0)
        {
            await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Error, "Получен пустой список вариантов"));

            return default;
        }

        if (options.Count > VotingOptionsMaxCount)
        {
            var msg = $"Превышен лимит вариантов голосования" +
                $"\nПолучено – **{options.Count}** вариантов; лимит – **{VotingOptionsMaxCount}** вариантов" +
                $"\nНа голосовании будут показаны только первые {VotingOptionsMaxCount} вариантов";

            await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Warning, msg));
        }


        var listToDisplay = displayOptions is null ? options.Select(o => o.ToString()).ToList()! : displayOptions;

        var emojis = GetEmotesList(Math.Min(options.Count, VotingOptionsMaxCount), listToDisplay, out var text);

        text += $"0 - Пропустить голосование\n";
        emojis.Add(new Emoji(0.ConvertToSmile()));


        var votingMessage = await channel.SendMessageAsync(text);

        await votingMessage.AddReactionsAsync(emojis.ToArray());


        await WaitForTimerAsync(voteTime, token, channel);


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


    public async Task<IEnumerable<IUserMessage>> BroadcastMessagesAsync(IEnumerable<IMessageChannel> channels, string? text = null, bool isTTS = false,
        Embed? embed = null, RequestOptions? options = null, AllowedMentions? mentions = null, MessageReference? reference = null)
    {
        var tasks = new List<Task<IUserMessage>>();

        foreach (var channel in channels)
            tasks.Add(channel.SendMessageAsync(text, isTTS, embed, options, mentions, reference));

        var messages = new List<IUserMessage>();

        while (tasks.Count > 0)
        {
            var messageTask = await Task.WhenAny(tasks);

            tasks.Remove(messageTask);

            try
            {
                var message = await messageTask;

                messages.Add(message);
            }
            catch (HttpException e)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, $"Не удалось отправить сообщение в канал. Причина: {e.Reason}");
            }
        }


        return messages;
    }


    public async Task<IUserMessage> ReplyEmbedAsync(EmbedStyle embedStyle, string description, string? title = null, EmbedBuilder? embedBuilder = null)
    {
        var embed = CreateEmbed(embedStyle, description, title, embedBuilder);

        return await ReplyAsync(embed: embed);
    }

    public async Task<IUserMessage> ReplyEmbedStampAsync(EmbedStyle embedStyle, string description, string? title = null, EmbedBuilder? embedBuilder = null)
    {
        var embed = CreateEmbedStamp(embedStyle, description, title, embedBuilder);

        return await ReplyAsync(embed: embed);
    }

    public async Task<IUserMessage> ReplyEmbedAndDeleteAsync(EmbedStyle embedType, string description, string? title = null,
        EmbedBuilder? embedBuilder = null, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(embedType, description, title, embedBuilder);

        _ = Task.Run(async () =>
        {
            await Task.Delay(timeout ?? TimeSpan.FromSeconds(10));

            await msg.DeleteAsync();
        });

        return msg;
    }


    public async Task<InteractiveResult<SocketReaction?>> NextReactionAsync(IUserMessage message, TimeSpan? timeout = null, IList<IEmote>? emotes = null,
        bool hasCancellationSmile = false, RequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        emotes ??= message.Reactions.Keys.ToList();

        if (emotes.Count == 0)
        {
            emotes.Add(ConfirmEmote);
            emotes.Add(DenyEmote);
        }

        if (hasCancellationSmile)
            emotes.Add(CancelEmote);

        var cts = new CancellationTokenSource();

        options ??= new RequestOptions()
        {
            CancelToken = cts.Token
        };

        if (options.CancelToken == default)
            options.CancelToken = cts.Token;

        timeout ??= TimeSpan.FromSeconds(15);

        var addReactionsTask = message.AddReactionsAsync(emotes.ToArray(), options);

        var result = await Interactive.NextReactionAsync(x => x.UserId == Context.User.Id && x.MessageId == message.Id, null, timeout, cancellationToken);

        cts.Cancel();

        try
        {
            await addReactionsTask;
        }
        catch (OperationCanceledException) { }


        return result;
    }

    public async Task<InteractiveResult<SocketMessage?>> NextMessageAsync(string? message = null, bool isTTS = false, Embed? embed = null,
        bool fromSourceUser = true, bool fromSourceChannel = true, TimeSpan? timeout = null, IMessageChannel? messageChannel = null, CancellationToken cancellationToken = default)
    {
        messageChannel ??= Context.Channel;

        await messageChannel.SendMessageAsync(message, isTTS, embed);

        var result = await Interactive.NextMessageAsync(Filter, null, timeout, cancellationToken);

        return result;


        bool Filter(SocketMessage msg)
        {
            if (fromSourceUser && msg.Author != Context.User)
                return false;

            if (fromSourceChannel && msg.Channel != messageChannel)
                return false;

            return true;
        }
    }



    public async Task<bool?> ConfirmActionAsync(string title, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(EmbedStyle.Information, "Подтвердите действие", title);

        var res = await ConfirmActionAsync(msg, timeout);

        return res;
    }

    public async Task<bool?> ConfirmActionAsync(IUserMessage message, TimeSpan? timeout = null)
    {
        var result = await NextReactionAsync(message, timeout);

        if (!result.IsSuccess)
            return null;

        var selectedEmote = result.Value!.Emote;

        if (selectedEmote.Name == ConfirmEmote.Name)
            return true;

        if (selectedEmote.Name == DenyEmote.Name)
            return false;


        return null;
    }



    protected static Embed CreateEmbed(EmbedStyle embedStyle, string description, string? title = null, EmbedBuilder? innerEmbedBuilder = null)
    {
        innerEmbedBuilder ??= new EmbedBuilder();

        innerEmbedBuilder.WithDescription(description);

        innerEmbedBuilder = embedStyle switch
        {
            EmbedStyle.Error => innerEmbedBuilder.WithErrorMessage(),
            EmbedStyle.Warning => innerEmbedBuilder.WithWarningMessage(),
            EmbedStyle.Successfull => innerEmbedBuilder.WithSuccessfullyMessage(),
            _ => innerEmbedBuilder.WithInformationMessage()
        };

        if (title is not null)
            innerEmbedBuilder.WithTitle(title);

        return innerEmbedBuilder.Build();
    }

    protected Embed CreateEmbedStamp(EmbedStyle embedStyle, string description, string? title = null, EmbedBuilder? innerEmbedBuilder = null)
    {
        innerEmbedBuilder ??= new EmbedBuilder()
            .WithUserAuthor(Context.User)
            .WithUserFooter(Context.Client.CurrentUser)
            .WithCurrentTimestamp();

        var embed = CreateEmbed(embedStyle, description, title, innerEmbedBuilder);

        return embed;
    }












    public async Task<bool> ConfirmActionWithHandlingAsync(string title, ulong? logChannelId = null, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(EmbedStyle.Information, "Подтвердите действие", title);

        return await ConfirmActionWithHandlingAsync(msg, logChannelId, timeout);
    }
    public async Task<bool> ConfirmActionWithHandlingAsync(IUserMessage message, ulong? logChannelId = null, TimeSpan? timeout = null)
    {
        var confirmed = await ConfirmActionAsync(message, timeout);

        if (confirmed is null)
        {
            await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, "Вы не подтвердили действие", "Сброс рейтинга");

            return false;
        }
        else if (confirmed is false)
        {
            await ReplyEmbedAndDeleteAsync(EmbedStyle.Error, "Вы отклонили действие", "Сброс рейтинга");

            return false;
        }

        var embed = (Embed)message.Embeds.First();

        message = await ReplyEmbedAndDeleteAsync(EmbedStyle.Successfull, "Вы подтвердили действие", embed.Title);

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