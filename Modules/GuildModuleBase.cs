using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using Serilog;
using Services;

namespace Modules;

[RequireContext(ContextType.Guild)]
[RequireBotPermission(GuildPermission.SendMessages)]
[RequireBotPermission(GuildPermission.EmbedLinks)]
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


    private IUser? _bot;
    protected IUser Bot => _bot ??= Context.Guild.GetUser(Context.Client.CurrentUser.Id);


    protected InteractiveService Interactive { get; }


    protected GuildModuleBase(InteractiveService interactiveService)
    {
        Interactive = interactiveService;
    }


    protected static ILogger GetGuildLogger(ulong guildId)
        => Log.ForContext("GuildName", guildId);



    public Task<IUserMessage> ReplyAsync(MessageData data)
        => Context.Channel.SendMessageAsync(data);


    public Task<IUserMessage> ReplyEmbedAsync(string description, string title, EmbedBuilder? embedBuilder = null)
        => ReplyEmbedAsync(description, EmbedStyle.Information, title, embedBuilder);

    public Task<IUserMessage> ReplyEmbedAsync(string description, EmbedStyle embedStyle = EmbedStyle.Information, string? title = null, EmbedBuilder? embedBuilder = null)
        => Context.Channel.SendEmbedAsync(description, embedStyle, title, embedBuilder);



    public Task<IUserMessage> ReplyEmbedStampAsync(string description, string title, EmbedBuilder? embedBuilder = null)
        => ReplyEmbedStampAsync(description, EmbedStyle.Information, title, embedBuilder);

    public Task<IUserMessage> ReplyEmbedStampAsync(string description, EmbedStyle embedStyle = EmbedStyle.Information, string? title = null, EmbedBuilder? embedBuilder = null)
        => Context.Channel.SendEmbedStampAsync(description, embedStyle, title, Context.User, Context.Client.CurrentUser, embedBuilder);


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
}