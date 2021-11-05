using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Data.ViewModels;
using Infrastructure.TypeReaders;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    public class SetupService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _provider;


        public SetupService(DiscordSocketClient discord, CommandService commandService, IConfiguration config, IServiceProvider provider)
        {
            _client = discord;
            _commandService = commandService;
            _config = config;
            _provider = provider;
        }


        public async Task ConfigureAsync()
        {
            var discordToken = _config["Tokens:DiscordBot"];

            if (string.IsNullOrWhiteSpace(discordToken))
                throw new ArgumentNullException(nameof(discordToken), "Bot's token not found, check the `_configuration.json` file");


            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();

            _commandService.AddTypeReader<bool>(new BooleanTypeReader(), true);
            _commandService.AddTypeReader<Emoji>(new EmojiTypeReader());
            _commandService.AddTypeReader<Emote>(new EmoteTypeReader());
            _commandService.AddTypeReader<MafiaSettingsViewModel>(new MafiaSettingsTypeReader());

            await _commandService.AddModulesAsync(Assembly.LoadFrom("Modules"), _provider);

            if (!_commandService.Modules.Any())
                throw new InvalidOperationException("Modules not loaded");
        }
    }
}
