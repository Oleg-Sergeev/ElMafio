using System;
using System.Threading.Tasks;
using Core.Common;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.Extensions.Caching.Memory;
using Modules.Common.Preconditions.Interactions;

namespace Modules.Settings;

[Group("настройки", "Редактирование настроек сервера")]
[RequireStandartAccessLevel(StandartAccessLevel.Administrator, Group = "perm")]
[RequireOwner(Group = "perm")]
public class SettingsModule : InteractionGuildModuleBase
{
    private readonly IMemoryCache _cache;

    public SettingsModule(InteractiveService interactiveService, IMemoryCache cache) : base(interactiveService)
    {
        _cache = cache;
    }


    [SlashCommand("префикс", "Изменить префикс бота")]
    public async Task SetPrefixAsync(string newPrefix)
    {
        var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

        ArgumentNullException.ThrowIfNull(server);

        var oldPrefix = server.Prefix;
        server.Prefix = newPrefix;

        var n = await Context.Db.SaveChangesAsync();

        if (n > 0)
        {
            _cache.Set((server.Id, "prefix"), newPrefix, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
            });

            await RespondEmbedAsync($"Префикс успешно изменен с `{oldPrefix}` на `{newPrefix}`", EmbedStyle.Successfull);
        }
        else
            await RespondEmbedAsync("Не удалось изменить префикс", EmbedStyle.Error);
    }


    [Group("черный-список", "Настройки черного списка")]
    public class BlockModule : InteractionGuildModuleBase
    {
        public BlockModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [SlashCommand("поведение", "Задает режим, в котором будет отправляться сообщение")]
        public async Task SetBehaviourAsync(BlockBehaviour blockBehaviour)
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            ArgumentNullException.ThrowIfNull(server);

            var oldBlockBehaviour = server.BlockBehaviour;

            if (blockBehaviour == oldBlockBehaviour)
            {
                await RespondEmbedAsync("Данное поведение уже установлено", EmbedStyle.Warning);

                return;
            }

            server.BlockBehaviour = blockBehaviour;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                await RespondEmbedAsync($"Поведение успешно изменено с `{oldBlockBehaviour}` на `{blockBehaviour}`", EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить поведение", EmbedStyle.Error);
        }


        [SlashCommand("сообщение", "Изменить сообщение о блокировке")]
        public async Task SetBlockMessageAsync(string blockMessage)
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            ArgumentNullException.ThrowIfNull(server);


            var oldBlockMessage = server.BlockMessage;

            if (blockMessage == oldBlockMessage)
            {
                await RespondEmbedAsync("Данное сообщение уже установлено", EmbedStyle.Warning);

                return;
            }

            server.BlockMessage = blockMessage;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                await RespondEmbedAsync($"Сообщение успешно изменено с `{oldBlockMessage}` на `{blockMessage}`", EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить сообщение", EmbedStyle.Error);
        }



        [SlashCommand("интервал", "Задать интервал отправки (в секундах) сообщения о блокировке")]
        public async Task SetSendMessageIntervalAsync([Summary("интервал", "диапазон значений: `1-600`")]int interval)
        {
            if (interval < 1 || interval > 600)
            {
                await RespondEmbedAsync("Интервал должен быть в диапазоне `1-600`", EmbedStyle.Error);

                return;
            }

            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            ArgumentNullException.ThrowIfNull(server);


            var oldInterval = server.SendInterval;

            if (interval == oldInterval)
            {
                await RespondEmbedAsync("Данный интервал уже установлен", EmbedStyle.Warning);

                return;
            }

            server.SendInterval = interval;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                await RespondEmbedAsync($"Интервал успешно изменен с `{oldInterval}с` на `{interval}с`", EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить интервал", EmbedStyle.Error);
        }
    }



    [Group("логгирование", "Раздел управления логами")]
    public class LogsModule : InteractionGuildModuleBase
    {
        public LogsModule(InteractiveService interactive) : base(interactive)
        {
        }



        [SlashCommand("канал", "Задать/сбросить канал логов")]
        public async Task SetLogChannelAsync([Summary("Канал_для_логов")] ITextChannel? logChannel = null)
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            ArgumentNullException.ThrowIfNull(server);


            server.LogChannelId = logChannel?.Id;

            var n = await Context.Db.SaveChangesAsync();


            if (n > 0)
            {
                var msg = logChannel is not null
                    ? $"Канал для логов [{logChannel.Mention}] успешно установлен"
                    : "Канал для логов успешно сброшен";

                await RespondEmbedAsync(msg, EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить канал для логов", EmbedStyle.Error);
        }
    }
}