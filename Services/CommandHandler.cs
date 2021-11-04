using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data.Models;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Infrastructure.Data;

namespace Infrastructure
{
    public class CommandHandler
    {
        private const string PrefixSectionPath = "DefaultSettings:Guild:Prefix";

        private static readonly Dictionary<ulong, string> _prefixes = new();

        private readonly BotContext _db;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _provider;
        private readonly IConfiguration _config;


        public CommandHandler(DiscordSocketClient discord, CommandService commandService, BotContext db, IServiceProvider provider, IConfiguration config)
        {
            _client = discord;
            _commandService = commandService;
            _db = db;
            _config = config;
            _provider = provider;

            _client.Ready += OnReadyAsync;

            _client.MessageReceived += OnMessageReceivedAsync;
            _client.JoinedGuild += OnJoinedGuildAsync;

            _db.SavedChanges += OnDbUpdated;
        }


        private async Task OnReadyAsync()
        {
            var loadGuildSettingsTask = LoadGuildSettingsAsync();

            await loadGuildSettingsTask;
        }

        private async Task OnJoinedGuildAsync(SocketGuild guild)
        {
            if (_db.GuildSettings.Any(g => g.Id == guild.Id)) return;


            await _db.GuildSettings.AddAsync(new GuildSettings
            {
                Id = guild.Id,
                Prefix = _config[PrefixSectionPath]
            });


            await _db.SaveChangesAsync();
        }


        private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage userMessage) return;

            if (userMessage.Author.IsBot) return;

            var context = new SocketCommandContext(_client, userMessage);

            int argPos = 0;
            if (userMessage.HasStringPrefix(_prefixes[context.Guild.Id], ref argPos) || userMessage.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await _commandService.ExecuteAsync(context, argPos, _provider);
            }
        }


        private void OnDbUpdated(object? sender, SavedChangesEventArgs args)
        {
            if (sender is null || sender is not BotContext db || args.EntitiesSavedCount != 1) return;


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


            if (guildsCount != _client.Guilds.Count)
            {
                var allGuildsId = _client.Guilds.Select(g => g.Id);

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
}
