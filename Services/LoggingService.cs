using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Infrastructure
{
    public class LoggingService
    {
        public const string PropertyGuildName = "GuildName";

        private const string LogsDirectory = @"Data\Logs";
        private const string GuildLogsDirectory = LogsDirectory + @"\Guilds";
        private const string GuildLogsDefaultName = "_UnidentifiedGuilds";
        private const string OutputConsoleTemplate = "{Timestamp:HH:mm:ss:fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
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

        ~LoggingService()
        {
            Log.Information("Shutting down the application");
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

            Log.Information(new string('-', 64));
            Log.Information("Application started");
        }


        public static FileStream? GetLogFile(string path)
        {
            if (!File.Exists(path)) return null;

            return File.OpenRead(path);
        }

        public static FileStream? GetGuildLogFileToday(ulong guildId)
        {
            var path = Path.Combine(GuildLogsDirectory, guildId.ToString(), $"log_{DateTime.Now:yyyyMMdd}.txt");

            return GetLogFile(path);
        }


        private async Task OnCommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
        {
            var guildLog = Log.ForContext(PropertyGuildName, context.Guild.Id);

            const string infoPattern = "({0:l}): Executed {1} for {2} in {3}. Raw message: {4}";

            guildLog.Verbose(infoPattern,
                nameof(OnCommandExecutedAsync),
                commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                context.User.Username,
                $"{context.Guild.Name}/{context.Channel.Name}",
                context.Message.Content);

            if (result.IsSuccess) return;

            switch (result.Error)
            {
                case CommandError.ParseFailed or CommandError.BadArgCount or CommandError.ObjectNotFound:
                    var parseResult = (ParseResult)result;

                    guildLog.Verbose("({0:l}): {1:l}. Error parameter: {2}; Arg values: {3}; Param values: {4}",
                        nameof(OnCommandExecutedAsync),
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
                        guildLog.Error(exeResult.Exception, infoPattern,
                                       nameof(OnCommandExecutedAsync),
                                       commandInfo.Value,
                                       context.User.Username,
                                       $"{context.Guild.Name}/{context.Channel.Name}",
                                       context.Message.Content);
                    else
                        guildLog.Warning($"{infoPattern}. Warning: {{5}}",
                                         nameof(OnCommandExecutedAsync),
                                         commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                                         context.User.Username,
                                         $"{context.Guild.Name}/{context.Channel.Name}",
                                         context.Message.Content,
                                         result.ToString());


                    await context.Channel.SendMessageAsync("Произошла непредвиденная ошибка");

                    break;
            }
        }

        private Task OnLog(LogMessage log)
        {
            var logLevel = ConvertSeverity(log.Severity);

            Log.Write(logLevel, log.Exception, $"({{0:l}}): {log.Message}", log.Source);

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
