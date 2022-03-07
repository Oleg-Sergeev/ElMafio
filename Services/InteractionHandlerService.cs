using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Infrastructure.Data.Models.ServerInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services;

public class InteractionHandlerService : DiscordClientService
{
    private readonly BotContext _db;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;


    public InteractionHandlerService(DiscordSocketClient client, ILogger<DiscordClientService> logger, InteractionService interactionService, BotContext db,
                                     IServiceProvider provider, IConfiguration config, IMemoryCache cache) : base(client, logger)
    {
        _interactionService = interactionService;
        _provider = provider;
        _db = db;
        _config = config;
        _cache = cache;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;

        await _interactionService.AddModulesAsync(Assembly.LoadFrom("Modules.dll"), _provider);

        if (!_interactionService.Modules.Any())
            throw new InvalidOperationException("Interactive modules not loaded");

        Client.InteractionCreated += OnInteractionCreatedAsync;
    }


    private async Task OnReadyAsync()
    {
        var guildsIds = _config.GetSection("Favorites:GuildsIds").Get<ulong[]>();

        if (guildsIds is not null && guildsIds.Length > 0)
            foreach (var guildId in guildsIds)
                try
                {
                    await _interactionService.RegisterCommandsToGuildAsync(guildId, true);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to add commands to guild \'{guildId}\'", guildId);
                }
    }


    private async Task OnInteractionCreatedAsync(SocketInteraction arg)
    {
        var context = new DbInteractionContext(Client, arg, _db);

        if (context.Guild is null || context.Interaction.Type == InteractionType.MessageComponent)
            return;

        Task<int>? saveChangesTask = null;
        if (!_cache.TryGetValue((context.User.Id, context.Guild.Id), out ServerUser? serverUser))
        {
            serverUser = await _db.ServerUsers.FindAsync(context.User.Id, context.Guild.Id);

            if (serverUser is null)
            {
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
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120)
            });
        }

        if (!serverUser!.IsBlocked)
        {
            _ = Task.Run(async () => await _interactionService.ExecuteCommandAsync(context, _provider));

            if (saveChangesTask is not null)
                await saveChangesTask;

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
                        await context.Interaction.RespondAsync(embed: embed);
                        await context.User.SendMessageAsync(embed: embed);
                        break;

                    case BlockBehaviour.SendToDM:
                        await context.User.SendMessageAsync(embed: embed);
                        break;

                    case BlockBehaviour.SendToServer:
                        await context.Interaction.RespondAsync(embed: embed);
                        break;

                    default:
                        await context.Interaction.RespondAsync(embed: embed, ephemeral: true);
                        break;
                }
            else
                await context.Interaction.RespondAsync(embed: embed, ephemeral: true);

        });
    }
}
