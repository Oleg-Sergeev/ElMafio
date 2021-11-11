﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Services;

namespace Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var host = CreateHostBuilder(args)
                    .Build();

        host.Services.GetRequiredService<LoggingService>();

        await host.RunAsync();
    }

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
            .SetBasePath(Path.Combine(AppContext.BaseDirectory, @"Data\Configs"))
            .AddJsonFile("AppConfig.json", false, true)
            .AddJsonFile("GamesConfig.json", false, true)
            .AddJsonFile("ContactsConfig.json", false, true)
            .AddJsonFile("LoggerConfig.json", false, true)
            .AddUserSecrets<Program>(false)
            .Build();
        })
        .ConfigureDiscordHost((context, discrodConfig) =>
        {
            discrodConfig.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000,
                ExclusiveBulkDelete = true,
                GatewayIntents =
              GatewayIntents.Guilds
            | GatewayIntents.GuildBans
            | GatewayIntents.GuildEmojis
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
            | GatewayIntents.GuildPresences
            | GatewayIntents.GuildVoiceStates
            | GatewayIntents.GuildMessageReactions
            | GatewayIntents.DirectMessageReactions
            };

            discrodConfig.Token = context.Configuration["Tokens:DiscordBot"];

        })
        .UseCommandService((context, commandServicesConfig) =>
        {
            commandServicesConfig.CaseSensitiveCommands = false;
            commandServicesConfig.DefaultRunMode = RunMode.Async;
            commandServicesConfig.IgnoreExtraArgs = true;
            commandServicesConfig.LogLevel = LogSeverity.Verbose;
            commandServicesConfig.SeparatorChar = '.';
        })
        .ConfigureServices((context, services) =>
        {
            services.AddDbContext<BotContext>(options =>
                options.UseSqlServer(context.Configuration.GetConnectionString("DefaultSQLServer")))
            .AddHostedService<CommandHandler>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<Random>();
        });

}