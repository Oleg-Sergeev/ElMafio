using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Modules.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Services
{
    public class LoggingService
    {
        public const string PropertyGuildName = "GuildName";

        private const string LogsDirectory = @"Data\Logs";
        private const string GuildLogsDirectory = LogsDirectory + @"\Guilds";
        private const string GuildLogsDefaultName = "_UnidentifiedGuilds";
        private const string OutputConsoleTemplate = "{Timestamp:HH:mm:ss:fff} [{Level:u3}] {Message:j}{NewLine}{Exception}";
        private const string OutputFileTemplate = "{Timestamp:dd.MM.yyyy HH:mm:ss:fff} [{Level:u3}] {Message:j}{NewLine}{Exception}";


        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;


        public LoggingService(DiscordSocketClient client, CommandService commandService)
        {
            _client = client;
            _commandService = commandService;


            _client.Log += OnLog;
            _commandService.Log += OnLog;
            _commandService.CommandExecuted += OnCommandExecutedAsync;

        }


        public void Configure()
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            Directory.CreateDirectory(GuildLogsDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Logger(lc => lc
                        .Filter.ByExcluding(Matching.WithProperty(PropertyGuildName))
                        .WriteTo.Async(wt => wt.Console(outputTemplate: OutputConsoleTemplate)))
                .WriteTo.Logger(lc => lc
                        .Filter.ByExcluding(Matching.WithProperty(PropertyGuildName))
                        .WriteTo.Async(wt => wt.File(Path.Combine(LogsDirectory, "log.txt"),
                                      restrictedToMinimumLevel: LogEventLevel.Debug,
                                      outputTemplate: OutputFileTemplate,
                                      shared: true)))
                .WriteTo.Logger(lc => lc.
                        Filter.ByIncludingOnly(Matching.WithProperty(PropertyGuildName))
                        .WriteTo.Map(PropertyGuildName, GuildLogsDefaultName, (guildName, writeTo)
                             => writeTo.Async(wt => wt.File(Path.Combine(GuildLogsDirectory, guildName, "log_.txt"),
                                             restrictedToMinimumLevel: LogEventLevel.Verbose,
                                             outputTemplate: OutputFileTemplate,
                                             rollingInterval: RollingInterval.Day,
                                             shared: true))))
                .CreateLogger();
        }


        private async Task OnCommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            var guildLog = Log.ForContext(PropertyGuildName, context.Guild.Id);

            guildLog.Verbose("({0:l}): Executed {1} for {2} in {3}. Raw message: {4}",
                nameof(OnCommandExecutedAsync),
                commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                context.User.GetFullName(),
                $"{context.Guild.Name}/{context.Channel.Name}",
                context.Message.Content);

            if (result.IsSuccess) return;


            switch (result.Error)
            {
                case CommandError.ParseFailed or CommandError.BadArgCount or CommandError.ObjectNotFound:
                    var parseResult = (ParseResult)result;

                    guildLog.Verbose("{0:l}. Error parameter: {1}; Arg values: {2}; Param values: {3}",
                        parseResult.ToString(),
                        parseResult.ErrorParameter?.ToString(),
                        parseResult.ArgValues,
                        parseResult.ParamValues);

                    await context.Channel.SendMessageAsync($"Неверные параметры команды. Введите команду **помощь {commandInfo.Value.Name}** для информации по данной команде");

                    break;

                case CommandError.UnknownCommand:
                    guildLog.Verbose(result.ToString());

                    await context.Channel.SendMessageAsync("Неизвестная команда. Введите команду **помощь** для получения списка команд");

                    break;

                case CommandError.UnmetPrecondition:
                    guildLog.Verbose(result.ToString());

                    await context.Channel.SendMessageAsync(result.ErrorReason);

                    break;

                default:
                    if (result is ExecuteResult exeResult)
                        guildLog.Error(exeResult.Exception, result.ToString());
                    else
                        guildLog.Warning(result.ToString());


                    await context.Channel.SendMessageAsync("Произошла непредвиденная ошибка");

                    break;
            }
        }

        private Task OnLog(LogMessage log)
        {
            var logText = $"({log.Source}): {log.Message}";

            var logLevel = ConvertSeverity(log.Severity);

            Log.Write(logLevel, log.Exception, logText);

            return Task.CompletedTask;
        }


        private static LogEventLevel ConvertSeverity(LogSeverity logSeverity)
            => logSeverity switch
            {
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Critical => LogEventLevel.Fatal,
                _ => throw new ArgumentException("Invalid value of log severity", nameof(logSeverity))
            };
    }
}
