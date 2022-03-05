    using System;
using System.IO;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models.Guild;
using Serilog;
using Serilog.Events;

namespace Services;

public class LoggingService
{
    public const string PropertyGuildName = "GuildName";

    private const string GuildLogsDefaultName = "_UnidentifiedGuilds";
    private const string OutputConsoleTemplate = "{Timestamp:HH:mm:ss:fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    private const string OutputFileTemplate = "{Timestamp:dd.MM.yyyy HH:mm:ss:fff} [{Level:u3}] {Message:j}{NewLine}{Exception}";

    private const string LogTemplate = "({0:l}): Executed {1} for {2} in {3}. Raw message: {4}";


    private readonly BotContext _db;
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commandService;
    private readonly Discord.Interactions.InteractionService _interactionService;


    public LoggingService(DiscordSocketClient client, CommandService commandService, Discord.Interactions.InteractionService interactionService, BotContext db)
    {
        _client = client;
        _commandService = commandService;
        _interactionService = interactionService;


        _commandService.CommandExecuted += OnCommandExecutedAsync;
        _db = db;
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

        var msg = "Произошла непредвиденная ошибка";

        switch (result.Error)
        {
            case CommandError.ParseFailed or CommandError.BadArgCount or CommandError.ObjectNotFound:
                var parseResult = (ParseResult)result;

                var cmd = context.Message.Content.Split(' ')[0].Remove(0, 1);

                msg = $"Неверные параметры команды. Введите команду **помощь {cmd}** для информации по данной команде";

                break;

            case CommandError.UnknownCommand:
                guildLog.Verbose(result.ToString());

                msg = $"Неизвестная команда. Введите команду **помощь** для получения списка команд";

                break;

            case CommandError.UnmetPrecondition:
                guildLog.Verbose(result.ToString());

                msg = $"Ошибка полномочий: {result.ErrorReason}";

                break;

            default:
                var exeResult = result as ExecuteResult?;
                if (exeResult is not null)
                    guildLog.Warning(exeResult.Value.Exception, LogTemplate,
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

                var server = await _db.Servers.FindAsync(context.Guild.Id);

                if (server is not null)
                {
                    var errorInfo = server.DebugMode switch
                    {
                        DebugMode.ErrorMessages => $"\n{result.ErrorReason}",
                        DebugMode.StackTrace => $"\n{exeResult?.Exception.ToString() ?? result.ErrorReason}",
                        _ => null
                    };

                    msg += errorInfo;
                }
                break;
        }

        try
        {
            await context.Channel.SendEmbedAsync(msg, EmbedStyle.Error);
        } 
        catch (HttpException e1) when (e1.DiscordCode == DiscordErrorCode.InsufficientPermissions)
        {
            try
            {
                await context.Channel.SendMessageAsync($"**Ошибка:**\n{msg}\nПожалуйста, предоставьте доступ к вставлению ссылок");
            }
            catch (HttpException e2) when (e2.DiscordCode == DiscordErrorCode.InsufficientPermissions)
            {
                try
                {
                    await context.User.SendMessageAsync($"**Ошибка:**\n{msg}\nПожалуйста, предоставьте доступ к отправке текстовых сообщений и вставление ссылок");
                } 
                catch (Exception e)
                {
                    guildLog.Warning(e, $"{LogTemplate} **Warning**",
                                        nameof(OnCommandExecutedAsync),
                                        commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                                        context.User.Username,
                                        $"{context.Guild.Name}/{context.Channel.Name}",
                                        context.Message.Content);

                    var appInfo = await context.Client.GetApplicationInfoAsync();
                    await appInfo.Owner.SendMessageAsync($"**Warning**\n{e}\n{context.Guild.Name}/{context.Channel.Name}/{context.User.Username}");
                }
            }
            catch (Exception e)
            {
                guildLog.Warning(e, LogTemplate,
                                    nameof(OnCommandExecutedAsync),
                                    commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                                    context.User.Username,
                                    $"{context.Guild.Name}/{context.Channel.Name}",
                                    context.Message.Content);
            }
        }
        catch (Exception e)
        {
            guildLog.Warning(e, LogTemplate,
                                nameof(OnCommandExecutedAsync),
                                commandInfo.IsSpecified ? commandInfo.Value.Name : "NULL",
                                context.User.Username,
                                $"{context.Guild.Name}/{context.Channel.Name}",
                                context.Message.Content);
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