using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Infrastructure.Data.ViewModels;
using Infrastructure.TypeReaders;
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
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;


    public CommandHandlerService(DiscordSocketClient client, CommandService commandService, BotContext db, IServiceProvider provider, IConfiguration config, ILogger<DiscordClientService> logger) : base(client, logger)
    {
        _db = db;
        _config = config;
        _provider = provider;
        _commandService = commandService;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Ready += OnReadyAsync;


        _commandService.AddTypeReader<bool>(new BooleanTypeReader(), true);
        _commandService.AddTypeReader<Emoji>(new EmojiTypeReader());
        _commandService.AddTypeReader<Emote>(new EmoteTypeReader());
        _commandService.AddTypeReader<MafiaSettingsViewModel>(new MafiaSettingsTypeReader());

        await _commandService.AddModulesAsync(Assembly.LoadFrom("Modules"), _provider);

        if (!_commandService.Modules.Any())
            throw new InvalidOperationException("Modules not loaded");
    }

    private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage.Author.IsBot)
            return;

        if (socketMessage is not SocketUserMessage userMessage)
            return;

        var context = new DbSocketCommandContext(Client, userMessage, _db);

        int argPos = 0;
        if (userMessage.HasStringPrefix(_prefixes[context.Guild.Id], ref argPos) || userMessage.HasMentionPrefix(Client.CurrentUser, ref argPos))
            await _commandService.ExecuteAsync(context, argPos, _provider, MultiMatchHandling.Best);
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

    private async Task OnUserLeft(SocketGuildUser user)
    {
        var guildSettings = await _db.GuildSettings.FindAsync(user.Guild.Id);

        var logChannelId = guildSettings.LogChannelId;

        if (logChannelId is not null)
        {
            var guild = user.Guild;

            var logChannel = guild.GetTextChannel(logChannelId.Value);

            if (logChannel is not null)
            {
                await logChannel.SendMessageAsync(
                        $"{guild.EveryoneRole.Mention} Пользователь **{user.Nickname ?? user.Username}** покинул сервер");
            }
        }
    }

    private async Task OnJoinedGuildAsync(SocketGuild guild)
    {
        if (_db.GuildSettings.Any(g => g.Id == guild.Id))
            return;


        var guildSettings = new GuildSettings()
        {
            Id = guild.Id,
            Prefix = _config[PrefixSectionPath]
        };

        await _db.GuildSettings.AddAsync(guildSettings);

        await _db.MafiaSettings.AddAsync(new()
        {
            GuildSettingsId = guildSettings.Id
        });

        await _db.RussianRouletteSettings.AddAsync(new()
        {
            GuildSettingsId = guildSettings.Id
        });



        await _db.SaveChangesAsync();
    }



    private void OnDbUpdated(object? sender, SavedChangesEventArgs args)
    {
        if (sender is null || sender is not BotContext db || args.EntitiesSavedCount != 1)
            return;


        var lastGuildSettingsEntry = db.ChangeTracker.Entries<GuildSettings>().ToList().LastOrDefault();

        if (lastGuildSettingsEntry is not null)
        {
            var settings = lastGuildSettingsEntry.Entity;

            if (_prefixes[settings.Id] != settings.Prefix)
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

    private async Task AddNewGuildsAsync(IEnumerable<ulong> newGuildsId)
    {
        var newGuildsSettings = newGuildsId
            .Select(id => new GuildSettings
            {
                Id = id,
                Prefix = _config[PrefixSectionPath]
            })
            .ToList();

        await _db.GuildSettings.AddRangeAsync(newGuildsSettings);

        await _db.SaveChangesAsync();


        foreach (var guildSettings in newGuildsSettings)
            _prefixes.Add(guildSettings.Id, guildSettings.Prefix);
    }

}