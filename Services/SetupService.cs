using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Modules;
using Serilog;

namespace Services
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

            await _commandService.AddModulesAsync(Assembly.GetAssembly(typeof(HelpModule)), _provider);

            if (!_commandService.Modules.Any())
                throw new InvalidOperationException("Modules not loaded");
        }
    }
}
