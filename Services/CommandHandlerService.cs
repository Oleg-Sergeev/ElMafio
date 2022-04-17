using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Core.TypeReaders;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Entities;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services;

public class CommandHandlerService : DiscordClientService
{
    private const string PrefixSectionPath = "DefaultSettings:Guild:Prefix";

    private readonly BotContext _db;
    private readonly CommandService _commandService;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;


    public CommandHandlerService(DiscordSocketClient client, CommandService commandService, BotContext db,
        IServiceProvider provider, IConfiguration config, ILogger<DiscordClientService> logger, IMemoryCache cache) : base(client, logger)
    {
        _db = db;
        _config = config;
        _provider = provider;
        _commandService = commandService;
        _cache = cache;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;

        Client.MessageReceived += OnMessageReceivedAsync;
        Client.UserLeft += OnUserLeft;

        _commandService.AddTypeReader<bool>(new BooleanTypeReader(), true);
        _commandService.AddTypeReader<Emoji>(new EmojiTypeReader());
        _commandService.AddTypeReader<Emote>(new EmoteTypeReader());
        _commandService.AddTypeReader<Color>(new ColorTypeReader());

        await _commandService.AddModulesAsync(Assembly.LoadFrom("Modules.dll"), _provider);

        if (!_commandService.Modules.Any())
            throw new InvalidOperationException("Modules not loaded");
    }


    private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage.Author.IsBot)
            return;

        if (socketMessage is not SocketUserMessage userMessage)
            return;

        if (userMessage.Channel is not IGuildChannel)
            return;

        var context = new DbSocketCommandContext(Client, userMessage, _db);


        int argPos = 0;
        if (!userMessage.HasMentionPrefix(Client.CurrentUser, ref argPos))
        {
            if (!_cache.TryGetValue((context.Guild.Id, "prefix"), out string prefix))
            {
                var server = await _db.Servers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == context.Guild.Id);

                ArgumentNullException.ThrowIfNull(server);

                prefix = server.Prefix;

                _cache.Set((server.Id, "prefix"), server.Prefix, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(4),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
                });
            }

            if (!userMessage.HasStringPrefix(prefix, ref argPos))
                return;
        }

        Task<int>? saveChangesTask = null;
        if (!_cache.TryGetValue((context.User.Id, context.Guild.Id), out ServerUser? serverUser))
        {
            serverUser = await _db.ServerUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == context.User.Id && x.ServerId == context.Guild.Id);

            if (serverUser is null)
            {
                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == context.User.Id);

                if (user is null)
                {
                    user = new()
                    {
                        Id = context.User.Id,
                        JoinedAt = context.User.CreatedAt.DateTime
                    };

                    _db.Users.Add(user);

                    await _db.SaveChangesAsync();
                }

                serverUser = new()
                {
                    UserId = context.User.Id,
                    ServerId = context.Guild.Id
                };

                _db.ServerUsers.Add(serverUser);

                saveChangesTask = _db.SaveChangesAsync();
            }

            _cache.Set((context.User.Id, context.Guild.Id), serverUser, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
            });
        }

        if (!serverUser!.IsBlocked)
        {
            if (saveChangesTask is not null)
                await saveChangesTask;

            _ = Task.Run(async () => await _commandService.ExecuteAsync(context, argPos, _provider));

            return;
        }

        await Task.Run(async () =>
        {
            var serverSettings = await _db.Servers.FindAsync(context.Guild.Id);

            if (serverSettings is null)
                throw new InvalidOperationException("Server settings cannot be null");

            var embed = EmbedHelper.CreateEmbed($"{context.User.Mention}\n{serverSettings.BlockMessage}", EmbedStyle.Warning, "Черный список");


            var key = (serverUser.UserId, serverUser.ServerId, "canSendMessage");

            if (!_cache.TryGetValue(key, out bool canSend))
            {
                canSend = true;

                _cache.Set(key, false, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(serverSettings.SendInterval)
                });
            }

            if (canSend)
                switch (serverSettings.BlockBehaviour)
                {
                    case BlockBehaviour.SendEverywhere:
                        await context.Channel.SendMessageAsync(embed: embed);
                        await context.User.SendMessageAsync(embed: embed);
                        break;

                    case BlockBehaviour.SendToDM:
                        await context.User.SendMessageAsync(embed: embed);
                        break;

                    case BlockBehaviour.SendToServer:
                        await context.Channel.SendMessageAsync(embed: embed);
                        break;

                    default:
                        break;
                }
        });
    }



    private async Task OnReadyAsync()
    {
        var loadServerTask = LoadServerAsync();

        await loadServerTask;


        Client.JoinedGuild -= OnJoinedGuildAsync;
        Client.JoinedGuild += OnJoinedGuildAsync;
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        var Server = await _db.Servers.FindAsync(guild.Id);

        if (Server is null)
            throw new InvalidOperationException();

        var logChannelId = Server.LogChannelId;

        if (logChannelId is not null)
        {
            var logChannel = guild.GetTextChannel(logChannelId.Value);

            if (logChannel is not null)
            {
                await logChannel.SendMessageAsync(
                        $"{guild.EveryoneRole.Mention} Пользователь **{user.GetFullName()}** покинул сервер");
            }
        }
    }

    private async Task OnJoinedGuildAsync(SocketGuild guild)
    {
        if (_db.Servers.Any(g => g.Id == guild.Id))
            return;


        await AddNewGuildAsync(guild.Id);
    }


    private async Task LoadServerAsync()
    {
        var serversCount = await _db.Servers
            .AsNoTracking()
            .CountAsync();

        var existingServersSettings = await _db.Servers
            .AsNoTracking()
            .Select(s => new { s.Id, s.Prefix })
            .ToListAsync();


        foreach (var server in existingServersSettings)
            _cache.Set((server.Id, "prefix"), server.Prefix, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(4),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
            });


        if (serversCount != Client.Guilds.Count)
        {
            var allGuildsId = Client.Guilds.Select(g => g.Id);

            var newGuildsId = allGuildsId
                .Except(existingServersSettings
                .Select(s => s.Id));


            await AddNewGuildsAsync(newGuildsId);
        }
    }


    private async Task AddNewGuildAsync(ulong guildId)
    {
        var server = new Server()
        {
            Id = guildId,
            Prefix = _config[PrefixSectionPath]
        };

        _db.Servers.Add(server);

        await _db.SaveChangesAsync();


        _cache.Set((server.Id, "prefix"), server.Prefix, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(4),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
        });
    }

    private async Task AddNewGuildsAsync(IEnumerable<ulong> newGuildsId)
    {
        foreach (var guildId in newGuildsId)
            await AddNewGuildAsync(guildId);
    }
}