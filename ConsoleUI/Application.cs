using System;
using System.IO;
using System.Linq;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Infrastructure.Data;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Services;
using Modules.Games.Services;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Services;
using CmdRunMode = Discord.Commands.RunMode;
using InrctRunMode = Discord.Interactions.RunMode;

namespace ConsoleUI;

public static class Application
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
        .UseSerilog((context, services, loggerConfig) =>
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            Directory.CreateDirectory(@"Data\Logs\Guilds");

            const string PropertyGuildName = "GuildName";
            const string LogsDirectory = @"Data\Logs";
            const string GuildLogsDirectory = LogsDirectory + @"\Guilds";
            const string GuildLogsDefaultName = "_UnidentifiedGuilds";
            const string OutputConsoleTemplate = "{Timestamp:HH:mm:ss:fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            const string OutputFileTemplate = "{Timestamp:dd.MM.yyyy HH:mm:ss:fff} [{Level:u3}] {Message:j}{NewLine}{Exception}";

            loggerConfig.
                    MinimumLevel.Verbose()
                    .WriteTo.Logger(lc => lc
                            .Filter.ByExcluding(Matching.WithProperty(PropertyGuildName))
                            .WriteTo.Async(wt => wt.Console(outputTemplate: OutputConsoleTemplate)))
                    .WriteTo.Logger(lc => lc
                            .Filter.ByExcluding(Matching.WithProperty(PropertyGuildName))
                            .WriteTo.Async(wt => wt.File(Path.Combine(LogsDirectory, "log.txt"),
                                          restrictedToMinimumLevel: LogEventLevel.Verbose,
                                          outputTemplate: OutputFileTemplate,
                                          shared: true)))
                    .WriteTo.Logger(lc => lc.
                            Filter.ByIncludingOnly(Matching.WithProperty(PropertyGuildName))
                            .WriteTo.Map(PropertyGuildName, GuildLogsDefaultName, (guildName, writeTo)
                                 => writeTo.Async(wt => wt.File(Path.Combine(GuildLogsDirectory, guildName, "log_.txt"),
                                                 restrictedToMinimumLevel: LogEventLevel.Verbose,
                                                 outputTemplate: OutputFileTemplate,
                                                 rollingInterval: RollingInterval.Day,
                                                 shared: true))));
        })
        .ConfigureAppConfiguration(builder =>
        {
            builder
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, Constants.ConfigsRoot))
            .AddJsonFile("AppConfig.json", false, true)
            .AddJsonFile("GamesConfig.json", false, true)
            .AddJsonFile("ContactsConfig.json", false, true)
            .AddJsonFile("LoggerConfig.json", false, true)
            .AddUserSecrets<Program>(false)
            .Build();
        })
        .ConfigureDiscordHost((context, discordConfig) =>
        {
            discordConfig.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                UseSystemClock = true,
                GatewayIntents = GatewayIntents.All
            };

            discordConfig.Token = context.Configuration["Tokens:DiscordBot"];

        })
        .UseCommandService((context, commandServicesConfig) =>
        {
            commandServicesConfig.CaseSensitiveCommands = false;
            commandServicesConfig.DefaultRunMode = CmdRunMode.Async;
            commandServicesConfig.IgnoreExtraArgs = true;
            commandServicesConfig.LogLevel = LogSeverity.Verbose;
            commandServicesConfig.SeparatorChar = '.';
        })
        .ConfigureServices((context, services) =>
        {
            services.AddDbContext<BotContext>(options =>
            {
                options.UseSqlServer(context.Configuration.GetConnectionStringDebugDb());
                options.EnableSensitiveDataLogging();
            })
            .AddHostedService<CommandHandlerService>()
            .AddSingleton<InteractiveService>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig()
            {
                DefaultRunMode = InrctRunMode.Async,
                LogLevel = LogSeverity.Verbose
            }))
            .AddSingleton<LoggingService>()
            .AddTransient<IMafiaSetupService, MafiaSetupService>()
            .AddTransient(typeof(IGameSettingsService<>), typeof(GameSettingsService<>))
            .AddTransient<IGameSettingsService<MafiaSettings>, MafiaSettingsService>();

            var sections = typeof(GameRole).GetAllDerivedTypes().Select(t => t.Name);

            foreach (var section in sections)
                services.Configure<GameRoleData>(section, context.Configuration.GetSection($"{GameRoleData.RootSection}:{section}"));
        });
}
