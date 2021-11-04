using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure;

namespace Core
{
    public class Startup
    {
        private const string ConfigPath = @"Data\Configs\DefaultConfig.json";

        public IConfiguration Configuration { get; }


        public Startup(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(ConfigPath, false, true);

            Configuration = builder.Build();
        }


        public async Task RunAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<LoggingService>().Configure();
            provider.GetRequiredService<CommandHandler>();

            await provider.GetRequiredService<SetupService>().ConfigureAsync();

            await Task.Delay(Timeout.Infinite);
        }


        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<BotContext>(options =>
               options.UseSqlServer(Configuration.GetConnectionString("DefaultSQLServer")));

            services
            .AddSingleton<LoggingService>()
            .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
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
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true,
                SeparatorChar = '.'
            }))
            .AddSingleton<CommandHandler>()
            .AddSingleton<SetupService>()
            .AddSingleton<Random>()
            .AddSingleton(Configuration);
        }
    }
}
