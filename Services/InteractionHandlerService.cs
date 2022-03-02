using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.TypeReaders;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Services;

public class InteractionHandlerService : DiscordClientService
{
    private readonly BotContext _db;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;


    public InteractionHandlerService(DiscordSocketClient client, ILogger<DiscordClientService> logger, InteractionService interactionService, BotContext db, IServiceProvider provider) : base(client, logger)
    {
        _interactionService = interactionService;
        _provider = provider;
        _db = db;
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
        await _interactionService.RegisterCommandsToGuildAsync(776013268908113930, true);
    }


    private Task OnInteractionCreatedAsync(SocketInteraction arg)
    {
        var ctx = new DbInteractionContext(Client, arg, _db);

        _ = _interactionService.ExecuteCommandAsync(ctx, _provider);

        return Task.CompletedTask;
    }

}
