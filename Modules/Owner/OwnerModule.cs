using System;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord.Commands;
using Fergun.Interactive;
using Infrastructure.Data.Models.Guild;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Modules.Owner;

[RequireContext(ContextType.Guild)]
[RequireOwner]
[Group]
public class OwnerModule : CommandGuildModuleBase
{
    public OwnerModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Рестарт")]
    public async Task RestartAsync()
    {
        await ReplyEmbedAsync("Перезапуск...", EmbedStyle.Debug);

        Log.Debug("({0:l}): Restart request received from server {1} by user {2}",
                  nameof(RestartAsync),
                  Context.Guild.Name,
                  Context.User.GetFullName());

        var asm = Assembly.GetEntryAssembly();

        if (asm is null)
        {
            await ReplyEmbedAsync("Не удалось найти входную сборку", EmbedStyle.Error);

            return;
        }

        System.Diagnostics.Process.Start(asm.Location.Replace("dll", "exe"));

        Environment.Exit(0);
    }


    [Group("Дебаг")]
    [Alias("Д")]
    public class DebugModule : CommandGuildModuleBase
    {
        public DebugModule(InteractiveService interactive) : base(interactive)
        {
        }



        [Command("Отладка")]
        [Alias("о")]
        public async Task SwitchDebugMode(DebugMode mode)
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            if (server is null)
            {
                await ReplyEmbedAsync("Сервер не найден", EmbedStyle.Error);

                return;
            }

            if (server.DebugMode == mode)
            {
                await ReplyEmbedAsync("Указанный режим уже установлен", EmbedStyle.Warning);

                return;
            }

            var oldMode = server.DebugMode;

            server.DebugMode = mode;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Режим отладки успешно изменен: `{oldMode} -> {mode}`", EmbedStyle.Successfull);
        }


        [Command("Текущий")]
        [Alias("Тек", "Т")]
        public async Task ShowDebugMode()
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            if (server is null)
            {
                await ReplyEmbedAsync("Сервер не найден", EmbedStyle.Error);

                return;
            }

            await ReplyEmbedAsync($"Текущий режим отладки: `{server.DebugMode}`");
        }
    }


    [Group("Кэш")]
    [Alias("К")]
    public class CacheModule : CommandGuildModuleBase
    {
        private readonly IMemoryCache _cache;


        public CacheModule(InteractiveService interactiveService, IMemoryCache cache) : base(interactiveService)
        {
            _cache = cache;
        }



        [Command("Бд")]
        public async Task ClearDbLocalCacheAsync()
        {
            Context.Db.ChangeTracker.Clear();

            await ReplyEmbedAsync("Кэш бд успешно очищен", EmbedStyle.Successfull);
        }


        [Command("Память")]
        [Alias("П")]
        public async Task ClearInMemoryCacheAsync()
        {
            _cache.Clear();

            await ReplyEmbedAsync("Кэш памяти успешно очищен", EmbedStyle.Successfull);
        }
    }
}
