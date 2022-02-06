using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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


    public Task<IUserMessage> ReplyAsync(MessageData data)
        => Context.Channel.SendMessageAsync(data);


    public Task<IUserMessage> ReplyEmbedAsync(string description, string title, EmbedBuilder? embedBuilder = null)
        => ReplyEmbedAsync(description, EmbedStyle.Information, title, embedBuilder);

    public Task<IUserMessage> ReplyEmbedAsync(string description, EmbedStyle embedStyle = EmbedStyle.Information, string? title = null, EmbedBuilder? embedBuilder = null)
        => Context.Channel.SendEmbedAsync(description, embedStyle, title, embedBuilder);



    public Task<IUserMessage> ReplyEmbedStampAsync(string description, string title, EmbedBuilder? embedBuilder = null)
        => ReplyEmbedStampAsync(description, EmbedStyle.Information, title, embedBuilder);

    public async Task<IUserMessage> ReplyEmbedStampAsync(string description, EmbedStyle embedStyle = EmbedStyle.Information, string? title = null, EmbedBuilder? embedBuilder = null)
    {
        var embed = CreateEmbedStamp(embedStyle, description, title, embedBuilder);

        return await ReplyAsync(embed: embed);
    }


    public async Task<IUserMessage> ReplyEmbedAndDeleteAsync(string description, EmbedStyle embedType = EmbedStyle.Information, string? title = null,
        EmbedBuilder? embedBuilder = null, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync(description, embedType, title, embedBuilder);

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


    public async Task<InteractiveResult<SocketMessage?>> NextMessageAsync(MessageData data, bool fromSourceUser = true, bool fromSourceChannel = true, bool deleteNextMessage = false,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var messageChannel = Context.Channel;

        var msg = await messageChannel.SendMessageAsync(data);

        var result = await Interactive.NextMessageAsync(Filter, null, timeout, cancellationToken);

        if (deleteNextMessage)
            await msg.DeleteAsync();

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

    public Task<InteractiveResult<SocketMessage?>> NextMessageAsync(string? message = null, bool isTTS = false, Embed? embed = null,
        bool fromSourceUser = true, bool fromSourceChannel = true, bool deleteNextMessage = false, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => NextMessageAsync(new MessageData() { Message = message, IsTTS = isTTS, Embed = embed },
            fromSourceUser, fromSourceChannel, deleteNextMessage, timeout, cancellationToken);



    public async Task<bool?> ConfirmActionAsync(string title, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync("Подтвердите действие", EmbedStyle.Information, title);

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



    protected Embed CreateEmbedStamp(EmbedStyle embedStyle, string description, string? title = null, EmbedBuilder? innerEmbedBuilder = null)
    {
        innerEmbedBuilder ??= new EmbedBuilder()
            .WithUserAuthor(Context.User)
            .WithUserFooter(Context.Client.CurrentUser)
            .WithCurrentTimestamp();

        var embed = EmbedHelper.CreateEmbed(description, embedStyle, title, innerEmbedBuilder);

        return embed;
    }












    public async Task<bool> ConfirmActionWithHandlingAsync(string title, ulong? logChannelId = null, TimeSpan? timeout = null)
    {
        var msg = await ReplyEmbedAsync("Подтвердите действие", EmbedStyle.Information, title);

        return await ConfirmActionWithHandlingAsync(msg, logChannelId, timeout);
    }
    public async Task<bool> ConfirmActionWithHandlingAsync(IUserMessage message, ulong? logChannelId = null, TimeSpan? timeout = null)
    {
        var confirmed = await ConfirmActionAsync(message, timeout);

        if (confirmed is null)
        {
            await ReplyEmbedAndDeleteAsync("Вы не подтвердили действие", EmbedStyle.Warning, "Сброс рейтинга");

            return false;
        }
        else if (confirmed is false)
        {
            await ReplyEmbedAndDeleteAsync("Вы отклонили действие", EmbedStyle.Error, "Сброс рейтинга");

            return false;
        }

        var embed = (Embed)message.Embeds.First();

        message = await ReplyEmbedAndDeleteAsync("Вы подтвердили действие", EmbedStyle.Successfull, embed.Title);

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