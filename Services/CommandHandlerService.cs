using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Core.Extensions;
using Core.TypeReaders;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services;

public class CommandHandlerService : DiscordClientService
{
    private const string PrefixSectionPath = "DefaultSettings:Guild:Prefix";

    private static readonly Dictionary<ulong, string> _prefixes = new();


    private readonly BotContext _db;
    private readonly CommandService _commandService;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;


    public CommandHandlerService(DiscordSocketClient client, CommandService commandService, BotContext db,
        IServiceProvider provider, IConfiguration config, ILogger<DiscordClientService> logger, InteractionService interactionService) : base(client, logger)
    {
        _db = db;
        _config = config;
        _provider = provider;
        _commandService = commandService;
        _interactionService = interactionService;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;

        _commandService.AddTypeReader<bool>(new BooleanTypeReader(), true);
        _commandService.AddTypeReader<Emoji>(new EmojiTypeReader());
        _commandService.AddTypeReader<Emote>(new EmoteTypeReader());
        _commandService.AddTypeReader<Color>(new ColorTypeReader());

        await _commandService.AddModulesAsync(Assembly.LoadFrom("Modules.dll"), _provider);

        if (!_commandService.Modules.Any())
            throw new InvalidOperationException("Modules not loaded");
    }


    private Task OnMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage.Author.IsBot)
            return Task.CompletedTask;

        if (socketMessage is not SocketUserMessage userMessage)
            return Task.CompletedTask;

        if (userMessage.Channel is not IGuildChannel)
            return Task.CompletedTask;

        var context = new DbSocketCommandContext(Client, userMessage, _db);

        int argPos = 0;
        if (userMessage.HasStringPrefix(_prefixes[context.Guild.Id], ref argPos) || userMessage.HasMentionPrefix(Client.CurrentUser, ref argPos))
            _ = Task.Run(async () => await _commandService.ExecuteAsync(context, argPos, _provider));

        return Task.CompletedTask;
    }



    private async Task OnReadyAsync()
    {
        var loadGuildSettingsTask = LoadGuildSettingsAsync();

        await loadGuildSettingsTask;

        Client.MessageReceived += OnMessageReceivedAsync;
        Client.JoinedGuild += OnJoinedGuildAsync;
        Client.UserLeft += OnUserLeft;

        _db.SavedChanges += OnDbUpdated;
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        var guildSettings = await _db.GuildSettings.FindAsync(guild.Id);

        if (guildSettings is null)
            throw new InvalidOperationException();

        var logChannelId = guildSettings.LogChannelId;

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
        if (_db.GuildSettings.Any(g => g.Id == guild.Id))
            return;


        await AddNewGuildAsync(guild.Id);
    }


    private void OnDbUpdated(object? sender, SavedChangesEventArgs args)
    {
        if (sender is not BotContext db || args.EntitiesSavedCount != 1)
            return;


        var lastGuildSettingsEntry = db.ChangeTracker.Entries<GuildSettings>().ToList().LastOrDefault();

        if (lastGuildSettingsEntry is not null)
        {
            var settings = lastGuildSettingsEntry.Entity;

            if (_prefixes.TryGetValue(settings.Id, out var prefix) && prefix != settings.Prefix)
            {
                _prefixes[settings.Id] = settings.Prefix;

                lastGuildSettingsEntry.State = EntityState.Detached;
            }
        }
    }



    private async Task LoadGuildSettingsAsync()
    {
        var guildsCount = await _db.GuildSettings
            .AsNoTracking()
            .CountAsync();

        var existingGuildsSettings = await _db.GuildSettings
            .AsNoTracking()
            .Select(g => new { g.Id, g.Prefix })
            .ToListAsync();


        foreach (var guildSettings in existingGuildsSettings)
            _prefixes.Add(guildSettings.Id, guildSettings.Prefix);


        if (guildsCount != Client.Guilds.Count)
        {
            var allGuildsId = Client.Guilds.Select(g => g.Id);

            var newGuildsId = allGuildsId.Except(existingGuildsSettings.Select(gs => gs.Id));


            await AddNewGuildsAsync(newGuildsId);
        }
    }


    private async Task AddNewGuildAsync(ulong guildId)
    {
        var guildSettings = new GuildSettings()
        {
            Id = guildId,
            Prefix = _config[PrefixSectionPath]
        };

        _db.GuildSettings.Add(guildSettings);

        await _db.SaveChangesAsync();


        _prefixes.Add(guildSettings.Id, guildSettings.Prefix);
    }

    private async Task AddNewGuildsAsync(IEnumerable<ulong> newGuildsId)
    {
        foreach (var guildId in newGuildsId)
            await AddNewGuildAsync(guildId);
    }
}