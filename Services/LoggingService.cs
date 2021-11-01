using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Services
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;


        public LoggingService(DiscordSocketClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;


            _client.Log += OnLog;
            _commandService.Log += OnLog;
            _commandService.CommandExecuted += OnCommandExecutedAsync;

            _client.LoggedIn += OnLoggedIn;
            _client.LoggedOut += OnLoggedOut;
            _client.Ready += OnReady;
            _client.Connected += OnConnected;
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        private Task OnLog(LogMessage log)
        {
            var logText = $"{DateTime.UtcNow:hh:mm:ss:fff} [{log.Severity}] {log.Source}: {log.Exception?.ToString() ?? log.Message}";

            return Console.Out.WriteLineAsync(logText);
        }

        private Task OnReady()
        {
            return Console.Out.WriteLineAsync($"{_client.CurrentUser} is ready");
        }

        private Task OnConnected()
        {
            return Console.Out.WriteLineAsync($"{_client.CurrentUser} is connected");
        }

        private Task OnLoggedIn()
        {
            return Console.Out.WriteLineAsync($"Logged in");
        }

        private Task OnLoggedOut()
        {
            return Console.Out.WriteLineAsync($"{_client.CurrentUser} is logged out");
        }
    }
}
