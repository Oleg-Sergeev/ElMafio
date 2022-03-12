using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Modules.Common.Preconditions.Interactions;
using Serilog;

namespace Modules.Developer;

[DefaultPermission(false)]
[RequireContext(ContextType.Guild)]
[RequireOwner(Group = "perm")]
[RequireStandartAccessLevel(StandartAccessLevel.Administrator, Group = "perm")]
public class DeveloperModule : InteractionGuildModuleBase
{
    public DeveloperModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [SlashCommand("рестарт", "Перезапустить бота")]
    public async Task RestartAsync()
    {
        await RespondEmbedAsync("Перезапуск...", EmbedStyle.Debug);

        Log.Debug("({0:l}): Restart request received from server {1} by user {2}",
                  nameof(RestartAsync),
                  Context.Guild.Name,
                  Context.User.GetFullName());

        var asm = Assembly.GetEntryAssembly();

        if (asm is null)
        {
            await RespondEmbedAsync("Не удалось найти входную сборку", EmbedStyle.Error);

            return;
        }

        System.Diagnostics.Process.Start(asm.Location.Replace("dll", "exe"));

        Environment.Exit(0);
    }


    public class DebugModule : InteractionGuildModuleBase
    {
        public DebugModule(InteractiveService interactive) : base(interactive)
        {
        }



        [SlashCommand("режим", "сменить режим отладки")]
        public async Task SwitchDebugMode(DebugMode mode)
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            if (server is null)
            {
                await RespondEmbedAsync("Сервер не найден", EmbedStyle.Error);

                return;
            }

            if (server.DebugMode == mode)
            {
                await RespondEmbedAsync("Указанный режим уже установлен", EmbedStyle.Warning);

                return;
            }

            var oldMode = server.DebugMode;

            server.DebugMode = mode;

            await Context.Db.SaveChangesAsync();


            await RespondEmbedAsync($"Режим отладки успешно изменен: `{oldMode} -> {mode}`", EmbedStyle.Successfull);
        }
    }


    public class CacheModule : InteractionGuildModuleBase
    {
        private readonly IMemoryCache _cache;


        public CacheModule(InteractiveService interactiveService, IMemoryCache cache) : base(interactiveService)
        {
            _cache = cache;
        }



        //[Group("бд", "Кэш базы данных")]
        //public class DatabaseCacheModule : InteractionGuildModuleBase
        //{
        //    public DatabaseCacheModule(InteractiveService interactive) : base(interactive)
        //    {
        //    }
        //}


        //[Group("память", "Кэш памяти")]
        //public class InMemoryCacheModule : InteractionGuildModuleBase
        //{
        //    private readonly IMemoryCache _cache;


        //    public InMemoryCacheModule(InteractiveService interactive, IMemoryCache cache) : base(interactive)
        //    {
        //        _cache = cache;
        //    }


        [SlashCommand("показать-память", "Просмотреть текущий кэш памяти")]
        public async Task ShowInMemoryCacheAsync()
        {
            var cacheDictionary = _cache.GetCacheDictionary();

            var pairs = new List<string>();

            foreach (DictionaryEntry entry in cacheDictionary)
                pairs.Add(entry.Value is ICacheEntry ce
                    ? $"`{ce.Key}` – `{ce.Value} ({ce.AbsoluteExpiration?.LocalDateTime:dd.MM HH:mm:ss} / {ce.SlidingExpiration?.TotalMinutes ?? -1}мин / {ce.AbsoluteExpirationRelativeToNow?.TotalMinutes ?? -1}мин)`"
                    : $"`{entry.Key}` – `{entry.Value?.ToString() ?? "[NULL]"}`");

            var msg = string.Join('\n', pairs);

            await RespondEmbedAsync(msg.Truncate(EmbedBuilder.MaxDescriptionLength));
        }


        [SlashCommand("очистить-память", "Очистить кэш памяти")]
        public async Task ClearInMemoryCacheAsync()
        {
            _cache.Clear();

            await RespondEmbedAsync("Кэш памяти успешно очищен", EmbedStyle.Successfull);
        }
        //}
    }
}
