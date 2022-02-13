﻿using System;
using System.IO;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

namespace Services;

public class LoggingService
{
    public const string PropertyGuildName = "GuildName";

    private const string LogsDirectory = @"Data\Logs";
    private const string GuildLogsDirectory = LogsDirectory + @"\Guilds";
    private const string GuildLogsDefaultName = "_UnidentifiedGuilds";
    private const string OutputConsoleTemplate = "{Timestamp:HH:mm:ss:fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    private const string OutputFileTemplate = "{Timestamp:dd.MM.yyyy HH:mm:ss:fff} [{Level:u3}] {Message:j}{NewLine}{Exception}";

    private const string LogTemplate = "({0:l}): Executed {1} for {2} in {3}. Raw message: {4}";


    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    private readonly Discord.Interactions.InteractionService _interactionService;


    public LoggingService(DiscordSocketClient client, CommandService commandService, Discord.Interactions.InteractionService interactionService)
    {
        _client = client;
        _commandService = commandService;
        _interactionService = interactionService;


        _commandService.CommandExecuted += OnCommandExecutedAsync;

        _interactionService.Log += OnLogAsync;
    }


    public static FileStream? GetLogFile(string path)
    {
        if (!File.Exists(path))
            return null;

        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public static FileStream? GetGuildLogFileToday(ulong guildId)
    {
        var path = Path.Combine(GuildLogsDirectory, guildId.ToString(), $"log_{DateTime.Now:yyyyMMdd}.txt");

        return GetLogFile(path);
    }


    private async Task OnCommandExecutedAsync(Optional<CommandInfo> commandInfo, ICommandContext context, IResult result)
    {
        var guildLog = Log.ForContext(PropertyGuildName, context.Guild.Id);

        guildLog.Verbose(LogTemplate,
            nameof(OnCommandExecutedAsync),
            commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
            context.User.Username,
            $"{context.Guild.Name}/{context.Channel.Name}",
            context.Message.Content);

        if (result.IsSuccess)
            return;

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

                var cmd = context.Message.Content.Split(' ')[0].Remove(0, 1);

                await context.Channel.SendEmbedAsync($"Неверные параметры команды. Введите команду **помощь {cmd}** для информации по данной команде", EmbedStyle.Error);

                break;

            case CommandError.UnknownCommand:
                guildLog.Verbose(result.ToString());

                await context.Channel.SendEmbedAsync("Неизвестная команда. Введите команду **помощь** для получения списка команд", EmbedStyle.Error);

                break;

            case CommandError.UnmetPrecondition:
                guildLog.Verbose(result.ToString());

                await context.Channel.SendEmbedAsync(result.ErrorReason, EmbedStyle.Error);

                break;

            default:
                if (result is ExecuteResult exeResult)
                    guildLog.Error(exeResult.Exception, LogTemplate,
                                   nameof(OnCommandExecutedAsync),
                                   commandInfo.Value,
                                   context.User.Username,
                                   $"{context.Guild.Name}/{context.Channel.Name}",
                                   context.Message.Content);
                else
                    guildLog.Warning($"{LogTemplate}. Warning: {{5}}",
                                     nameof(OnCommandExecutedAsync),
                                     commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                                     context.User.Username,
                                     $"{context.Guild.Name}/{context.Channel.Name}",
                                     context.Message.Content,
                                     result.ToString());


                await context.Channel.SendEmbedAsync($"Произошла непредвиденная ошибка: {result.ErrorReason}", EmbedStyle.Error);

                break;
        }
    }

    private Task OnLogAsync(LogMessage message)
    {
        Log.Write(ConvertSeverity(message.Severity), message.Exception, message.ToString());

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