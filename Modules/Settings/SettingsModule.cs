using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Common;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore;
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
            await RespondEmbedAsync("Не удалось изменить префикс", EmbedStyle.Error, ephemeral: true);
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
                await RespondEmbedAsync("Данное поведение уже установлено", EmbedStyle.Warning, ephemeral: true);

                return;
            }

            server.BlockBehaviour = blockBehaviour;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                await RespondEmbedAsync($"Поведение успешно изменено с `{oldBlockBehaviour}` на `{blockBehaviour}`", EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить поведение", EmbedStyle.Error, ephemeral: true);
        }


        [SlashCommand("сообщение", "Изменить сообщение о блокировке")]
        public async Task SetBlockMessageAsync(string blockMessage)
        {
            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            ArgumentNullException.ThrowIfNull(server);


            var oldBlockMessage = server.BlockMessage;

            if (blockMessage == oldBlockMessage)
            {
                await RespondEmbedAsync("Данное сообщение уже установлено", EmbedStyle.Warning, ephemeral: true);

                return;
            }

            server.BlockMessage = blockMessage;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                await RespondEmbedAsync($"Сообщение успешно изменено с `{oldBlockMessage}` на `{blockMessage}`", EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить сообщение", EmbedStyle.Error, ephemeral: true);
        }



        [SlashCommand("интервал", "Задать интервал отправки (в секундах) сообщения о блокировке")]
        public async Task SetSendMessageIntervalAsync([Summary("интервал", "диапазон значений: `1-600`")] int interval)
        {
            if (interval < 1 || interval > 600)
            {
                await RespondEmbedAsync("Интервал должен быть в диапазоне `1-600`", EmbedStyle.Error, ephemeral: true);

                return;
            }

            var server = await Context.Db.Servers.FindAsync(Context.Guild.Id);

            ArgumentNullException.ThrowIfNull(server);


            var oldInterval = server.SendInterval;

            if (interval == oldInterval)
            {
                await RespondEmbedAsync("Данный интервал уже установлен", EmbedStyle.Warning, ephemeral: true);

                return;
            }

            server.SendInterval = interval;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                await RespondEmbedAsync($"Интервал успешно изменен с `{oldInterval}с` на `{interval}с`", EmbedStyle.Successfull);
            }
            else
                await RespondEmbedAsync("Не удалось изменить интервал", EmbedStyle.Error, ephemeral: true);
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
                await RespondEmbedAsync("Не удалось изменить канал для логов", EmbedStyle.Error, ephemeral: true);
        }
    }



    [Group("уровни-доступа", "Раздел для управления уровнями доступа к боту")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireStandartAccessLevel(StandartAccessLevel.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public class AccessLevelsModule : InteractionGuildModuleBase
    {
        public AccessLevelsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [SlashCommand("текущий", "Показать текущий уровень доступа к боту")]
        public async Task ShowAccessLevel(IGuildUser guildUser)
        {
            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await RespondEmbedAsync($"Пользователь {guildUser.Mention} не найден", EmbedStyle.Error, ephemeral: true);

                return;
            }

            await RespondEmbedAsync($"Текущий уровень доступа у пользователя {guildUser.Mention}: **`{serverUser.StandartAccessLevel?.ToString() ?? "уровень доступа отсутствует"}`**");
        }


        [SlashCommand("установить", "Установить новый уровен доступа к боту для указанного пользователя")]
        public async Task SetAccessLevel(IGuildUser guildUser, StandartAccessLevel accessLevel)
        {
            var contextServerUser = await Context.Db.ServerUsers.FindAsync(Context.User.Id, Context.Guild.Id);

            if (contextServerUser is null)
                return;

            if (contextServerUser.StandartAccessLevel < accessLevel && contextServerUser.UserId != 184316176007036928)
            {
                await RespondEmbedAsync($"Невозможно изменить уровень доступа пользователью {guildUser.Mention}: " +
                    $"ваш уровень доступа ({contextServerUser.StandartAccessLevel}) ниже, чем уровень доступа пользователя {guildUser.Mention} ({accessLevel})",
                    EmbedStyle.Error, ephemeral: true);

                return;
            }

            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await RespondEmbedAsync($"Пользователь {guildUser.Mention} не найден", EmbedStyle.Error, ephemeral: true);

                return;
            }

            if (serverUser.StandartAccessLevel == contextServerUser.StandartAccessLevel && contextServerUser.UserId != 184316176007036928)
            {
                await RespondEmbedAsync($"Невозможно изменить уровень доступа пользователью {guildUser.Mention}: " +
                    $"вы имеете одинаковый уровень доступа",
                    EmbedStyle.Error, ephemeral: true);

                return;
            }

            if (serverUser.StandartAccessLevel == accessLevel)
            {
                await RespondEmbedAsync($"Пользователь {guildUser.Mention} уже имеет данный уровень доступа", EmbedStyle.Error, ephemeral: true);

                return;
            }

            var oldAccessLevel = serverUser.StandartAccessLevel;

            serverUser.StandartAccessLevel = accessLevel;


            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                await RespondEmbedAsync($"Уровень доступа у пользователя {guildUser.Mention} успешно изменен: " +
                    $"`{oldAccessLevel?.ToString() ?? "Нет статуса"}` -> `{accessLevel}`", EmbedStyle.Successfull);
            else
                await RespondEmbedAsync($"Не удалось иземнить уровень доступа у пользователя {guildUser.Mention}", EmbedStyle.Error, ephemeral: true);
        }


        [SlashCommand("сбросить", "Сбросить уроень доступа для указанного пользователя")]
        public async Task ResetAccessLevel(IGuildUser guildUser)
        {
            var contextServerUser = await Context.Db.ServerUsers.FindAsync(Context.User.Id, Context.Guild.Id);

            if (contextServerUser is null)
                return;


            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await RespondEmbedAsync($"Пользователь {guildUser.Mention} не найден", EmbedStyle.Error, ephemeral: true);

                return;
            }

            if (serverUser.StandartAccessLevel is null)
            {
                await RespondEmbedAsync($"У пользователя {guildUser.Mention} отсутствует уровень доступа", EmbedStyle.Error, ephemeral: true);

                return;
            }

            if (contextServerUser.StandartAccessLevel < serverUser.StandartAccessLevel)
            {
                await RespondEmbedAsync($"Невозможно сбросить уровень доступа пользователю {guildUser.Mention}: " +
                    $"ваш уровень доступа ({contextServerUser.StandartAccessLevel}) ниже, чем уровень доступа пользователя {guildUser.Mention} ({serverUser.StandartAccessLevel})",
                    EmbedStyle.Error, ephemeral: true);

                return;
            }

            serverUser.StandartAccessLevel = null;


            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                await RespondEmbedAsync($"Уровень доступа у пользователя {guildUser.Mention} успешно сброшен", EmbedStyle.Successfull);
            else
                await RespondEmbedAsync($"Не удалось сбросить уровень доступа у пользователя {guildUser.Mention}", EmbedStyle.Error, ephemeral: true);
        }
    }



    [Group("уровни-доступа-расширенные", "Раздел для расширенных уровней доступа (В разработке)")]
    [RequireStandartAccessLevel(StandartAccessLevel.Developer, Group = "ePerm")]
    [RequireOwner(Group = "ePerm")]
    public class ExtendedAccessLevelsModule : InteractionGuildModuleBase
    {
        public ExtendedAccessLevelsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [SlashCommand("список", "Вывести список расширенных уровней доступа")]
        public async Task ShowListAsync()
        {
            var accessLevels = await Context.Db.AccessLevels
                .AsNoTracking()
                .Where(al => al.ServerId == Context.Guild.Id)
                .OrderByDescending(al => al.Priority)
                .ToListAsync();


            if (accessLevels.Count == 0)
            {
                await RespondEmbedAsync("Уровни доступа отсутствуют", EmbedStyle.Error, ephemeral: true);

                return;
            }


            var msg = string.Join('\n', accessLevels.Select(al => $"`{al.Name} ({al.Priority})`"));

            await RespondEmbedAsync(msg);
        }


        [SlashCommand("добавить", "Добавить новый уровень доступа к боту")]
        public async Task AddAccessLevelAsync(
            [Summary("имя", "имя уровня доступа")] string name,
            [Summary("приоритет", "приоритет доступа")] int priority)
        {
            if (priority == int.MaxValue && Context.User.Id != 184316176007036928)
            {
                await RespondEmbedAsync("Наивысший приоритет не может быть задан вручную", EmbedStyle.Error, ephemeral: true);

                return;
            }

            var accessLevel = await Context.Db.AccessLevels
                .AsNoTracking()
                .FirstOrDefaultAsync(al => al.ServerId == Context.Guild.Id && al.Name == name);

            if (accessLevel is not null)
            {
                await RespondEmbedAsync("Уровень доступа с таким именем уже существует", EmbedStyle.Error, ephemeral: true);

                return;
            }


            accessLevel = new(name)
            {
                ServerId = Context.Guild.Id,
                Priority = priority
            };

            Context.Db.AccessLevels.Add(accessLevel);

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                await RespondEmbedAsync($"Уровень доступа **`{name} ({priority})`** успешно создан", EmbedStyle.Successfull);
            else
                await RespondEmbedAsync($"Не удалось создать уровень доступа **`{name} ({priority})`**", EmbedStyle.Error, ephemeral: true);
        }


        [SlashCommand("удалить", "Удалить уровень доступа. **Пользователи с данным уровнем доступа автоматически потеряют его**")]
        public async Task RemoveAccessLevelAsync(string name)
        {
            var accessLevel = await Context.Db.AccessLevels
                .AsNoTracking()
                .FirstOrDefaultAsync(al => al.ServerId == Context.Guild.Id && al.Name == name);

            if (accessLevel is null)
            {
                await RespondEmbedAsync("Уровень доступа с таким именем не существует", EmbedStyle.Error, ephemeral: true);

                return;
            }

            Context.Db.AccessLevels.Remove(accessLevel);

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                await RespondEmbedAsync($"Уровень доступа **`{accessLevel.Name} ({accessLevel.Priority})`** успешно удален", EmbedStyle.Successfull);
            else
                await RespondEmbedAsync($"Не удалось удалить уровень доступа **`{accessLevel.Name} ({accessLevel.Priority})`**", EmbedStyle.Error, ephemeral: true);
        }
    }
}