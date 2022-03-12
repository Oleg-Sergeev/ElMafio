using System;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Modules.Common.Preconditions.Commands;

namespace Modules.Admin;

[Group("Админ")]
[Alias("а")]
[Summary("Набор команд для управления сервером и получения полезной информации")]
[RequireStandartAccessLevel(StandartAccessLevel.Moderator, Group = "perm")]
public class AdminModule : CommandGuildModuleBase
{
    public AdminModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Слоумод")]
    [Alias("смод")]
    [Summary("Установить слоумод в текущем канале")]
    [Remarks("Диапазон интервала: `0-300с`")]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    [RequireUserPermission(GuildPermission.ManageChannels, Group = "perm")]
    public async Task SetSlowMode([Summary("Интервал слоумода")] int secs)
    {
        if (Context.Channel is not ITextChannel textChannel)
            return;


        secs = Math.Clamp(secs, 0, 300);


        await textChannel.ModifyAsync(props => props.SlowModeInterval = secs);

        await ReplyEmbedStampAsync($"Слоумод успешно установлен на {secs}с", EmbedStyle.Successfull);
    }



    [Command("Очистить")]
    [Alias("оч")]
    [Summary("Удалить указанное кол-во сообщений из текущего канала")]
    [Remarks("Максимальное кол-во сообщений, удаляемое за раз: `100`")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public async Task ClearAsync([Summary("Кол-во удаляемых сообщений")] int count)
    {
        if (Context.Channel is not ITextChannel textChannel)
        {
            await ReplyEmbedAndDeleteAsync("Невозможно удалить сообщения. Возможно вы указали не текстовый канал", EmbedStyle.Error);

            return;
        }

        count = Math.Clamp(count, 0, 100);

        var messagesToDelete = (await textChannel
            .GetMessagesAsync(count + 1)
            .FlattenAsync())
            .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14);

        await textChannel.DeleteMessagesAsync(messagesToDelete);


        await ReplyEmbedAndDeleteAsync($"Сообщения успешно удалены ({count} шт)", EmbedStyle.Successfull);
    }



    [Command("ОчиститьДо")]
    [Alias("Очдо")]
    [Summary("Удалить сообщения до указанного (удаляются сообщения, идущие **после** указанного)")]
    [Remarks("Максимальное кол-во сообщений, удаляемое за раз: `100`\n**Также можно ответить на сообщение, до которого нужно удалить все сообщения**")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public async Task ClearToAsync([Summary("ID сообщения, до которого нужно удалить сообщения")] ulong? messageId = null)
    {
        var message = messageId is not null
            ? await Context.Channel.GetMessageAsync(messageId.Value) ?? Context.Message.ReferencedMessage
            : Context.Message.ReferencedMessage;

        if (message is null)
        {
            await ReplyEmbedAndDeleteAsync("Укажите сообщение, до которого вы хотите очистить канал", EmbedStyle.Error);

            return;
        }

        if (Context.Channel is not ITextChannel textChannel)
        {
            await ReplyEmbedAndDeleteAsync("Невозможно удалить сообщения из данного канала", EmbedStyle.Error);

            return;
        }

        if (await textChannel.GetMessageAsync(message.Id) is null)
        {
            await ReplyEmbedAndDeleteAsync("Сообщение не найдено", EmbedStyle.Error);

            return;
        }

        var limit = 500;

        var messages = (await textChannel.GetMessagesAsync(message.Id, Direction.After, limit).FlattenAsync())
            .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14);

        if (!messages.TryGetNonEnumeratedCount(out var count))
            count = messages.Count();

        await textChannel.DeleteMessagesAsync(messages);


        await ReplyEmbedAndDeleteAsync($"Сообщения успешно удалены ({count} шт)", EmbedStyle.Successfull);
    }



    [Command("РольЦвет")]
    [Alias("РЦ")]
    [Summary("Изменить цвет роли на новый")]
    [Remarks("Форматы ввода цвета:" +
        "\n**0-255:** `(r, g, b)` **Пример:** `(150, 255, 0)`" +
        "\n**0-1:** `(r, g, b)` **Пример:** `(0.4, 0, 1)`" +
        "\n**HEX:** `#RRGGBB` **Пример:** `#280af0`")]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RequireUserPermission(GuildPermission.ManageRoles, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public async Task UpdateRoleColorAsync([Summary("Роль, для которой нужно изменить цвет")] IRole role, [Summary("Новый цвет")] Color color)
    {
        var guildUser = (IGuildUser)Context.User;

        if (guildUser.Hierarchy < role.Position)
        {
            var botOwner = (await Context.Client.GetApplicationInfoAsync()).Owner;

            if (botOwner.Id != guildUser.Id)
            {
                await ReplyEmbedAsync("Невозможно управлять ролью: недостаточно полномочий", EmbedStyle.Error);

                return;
            }
        }

        if (role.Color == color)
        {
            await ReplyEmbedAsync("Роль уже имеет такой цвет", EmbedStyle.Warning);

            return;
        }

        var oldColor = role.Color;

        await role.ModifyAsync(r => r.Color = color);

        await ReplyEmbedStampAsync($"Старый цвет: {oldColor} {oldColor.ToRgbString()}",
            EmbedStyle.Successfull,
            "Цвет роли успешно изменен");
    }





    [Group("Смайлы")]
    [Alias("Смайл", "С")]
    [Summary("Добавление и удаление пользовательских смайлов")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public class SmileModule : CommandGuildModuleBase
    {
        public SmileModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Priority(-1)]
        [Command("Добавить")]
        [Alias("+")]
        [Summary("Добавить смайл со стандартным именем")]
        [Remarks("Чтобы загрузить смайл на сервер, прикрепите картинку к сообщению, или ответьте на сообщение, содержащее картинку" +
            "\n**Картинка должна весить не более `256Кб`**")]
        public Task AddEmoteAsync()
            => AddEmoteAsync($"emoji_{Context.Guild.Emotes.Count + 1}");


        [Command("Добавить")]
        [Alias("+")]
        [Summary("Добавить смайл с указанным именем")]
        [Remarks("Чтобы загрузить смайл на сервер, прикрепите картинку к сообщению, или ответьте на сообщение, содержащее картинку" +
            "\n**Картинка должна весить не более `256Кб`**")]
        public async Task AddEmoteAsync([Summary("Имя смайла")] string name)
        {
            var attachment = Context.Message.Attachments.FirstOrDefault() ?? Context.Message.ReferencedMessage?.Attachments.FirstOrDefault();

            var httpClient = new HttpClient();

            if (attachment is null)
            {
                HttpResponseMessage? resp = null;

                var arr = Context.Message.Content.Split();
                var uri = arr.Length > 1 ? arr[1] : string.Empty;

                if (uri.StartsWith("http"))
                {
                    name = $"emoji_{Context.Guild.Emotes.Count + 1}";
                    resp = await httpClient.GetAsync(uri);
                }

                if ((resp is null || !resp.IsSuccessStatusCode) && (Context.Message.ReferencedMessage?.Content.StartsWith("http") ?? false))
                    resp = await httpClient.GetAsync(Context.Message.ReferencedMessage.Content);

                if (resp is null || !resp.IsSuccessStatusCode)
                {
                    await ReplyEmbedAsync("Прикрепите к своему сообщению картинку, или ответьте на сообщение, содержащее картинку", EmbedStyle.Warning);

                    return;
                }

                var resptream = await resp.Content.ReadAsStreamAsync();

                string? smileExtension = null;
                var smileExtensions = resp.Content.Headers.ContentType?.MediaType?.Split('/');

                if (smileExtensions?.Length > 1)
                    smileExtension = smileExtensions[1];

                if (smileExtension is null)
                {
                    await ReplyEmbedAsync("Прикрепите к своему сообщению картинку, или ответьте на сообщение, содержащее картинку", EmbedStyle.Error);

                    return;
                }

                var embed = EmbedHelper.CreateEmbed("Ваша картинка");

                var file = await Context.Channel.SendFileAsync(resptream, $"smile.{smileExtension}", embed: embed, messageReference: new(Context.Message.Id));

                attachment = file.Attachments.First();
            }

            if (attachment.Size > 256 * 1024)
            {
                await ReplyEmbedAsync($"Изображение имеет слишком большой размер. Допустимый размер: 256Кб; Размер изображения: {attachment.Size / 1024}Кб",
                    EmbedStyle.Error);

                return;
            }


            var stream = await httpClient.GetStreamAsync(attachment.Url);

            if (stream is null)
            {
                await ReplyEmbedAsync("Не удалось загрузить картинку\nКартинка не найдена", EmbedStyle.Error);

                return;
            }

            var image = new Image(stream);

            var emote = await Context.Guild.CreateEmoteAsync(name, image);


            var msg = await ReplyEmbedAsync("Смайл успешно добавлен", EmbedStyle.Successfull);

            await msg.AddReactionAsync(emote);
        }



        [Command("Удалить")]
        [Alias("-")]
        [Summary("Удалить указанный смайл с сервера")]
        public async Task DeleteEmoteAsync([Summary("Смайл, который необходимо удалить")] Emote emote)
        {
            if (!Context.Guild.Emotes.Contains(emote))
            {
                await ReplyEmbedAsync("Смайл не найден", EmbedStyle.Error);

                return;
            }

            var guildEmote = await Context.Guild.GetEmoteAsync(emote.Id);

            await Context.Guild.DeleteEmoteAsync(guildEmote);

            await ReplyEmbedAsync("Смайл успешно удален", EmbedStyle.Successfull);
        }
    }




    [Name("Черный список")]
    [Group("ЧерныйСписок")]
    [Alias("ЧС", "Блок")]
    [Summary("Управление черным списком")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public class BlockList : CommandGuildModuleBase
    {
        private readonly IMemoryCache _cache;

        public BlockList(InteractiveService interactiveService, IMemoryCache cache) : base(interactiveService)
        {
            _cache = cache;
        }

        [Name("Черный список")]
        [Command("Список")]
        [Alias("Лист")]
        public async Task GetBlockListAsync(int usersPerPage = 10)
        {
            var blockList = await Context.Db.ServerUsers
                .AsNoTracking()
                .Where(su => su.IsBlocked && su.ServerId == Context.Guild.Id)
                .ToListAsync();

            if (blockList.Count == 0)
            {
                await ReplyEmbedAsync("Черный список пуст", EmbedStyle.Warning);

                return;
            }

            var pagesCount = (blockList.Count - 1) / usersPerPage;

            var lazyPaginator = new LazyPaginatorBuilder()
                .AddUser(Context.User)
                .WithActionOnCancellation(ActionOnStop.DeleteMessage)
                .WithActionOnTimeout(ActionOnStop.DeleteMessage)
                .WithMaxPageIndex(pagesCount)
                .WithCacheLoadedPages(true)
                .WithPageFactory(GenerateBlockList)
                .Build();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            _ = Interactive.SendPaginatorAsync(lazyPaginator, Context.Channel, cancellationToken: cts.Token);


            IPageBuilder GenerateBlockList(int page)
                => new PageBuilder()
                {
                    Title = "Черный список",
                    Color = Utils.GetRandomColor(),
                    Description = string.Join('\n', blockList
                            .Skip(page * usersPerPage)
                            .Take(usersPerPage)
                            .Select(x => $"**{Context.Guild.GetMentionFromId(x.UserId)}**"))
                };
        }


        [Command("Добавить")]
        [Alias("+")]
        [RequireConfirmAction(false)]
        public async Task AddToBlockListAsync(IGuildUser guildUser)
        {
            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.GetFullMention()} не найден", EmbedStyle.Error);

                return;
            }

            if (serverUser.IsBlocked)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.GetFullMention()} уже добавлен в черный список", EmbedStyle.Warning);

                return;
            }

            if (serverUser.UserId == BotOwner.Id)
            {
                await ReplyEmbedAsync($"Невозможно добавить в черный список пользователя {guildUser.GetFullMention()}", EmbedStyle.Error);

                return;
            }

            serverUser.IsBlocked = true;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                _cache.Set((guildUser.Id, Context.Guild.Id), serverUser, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(10),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                });

                await ReplyEmbedStampAsync($"Пользователь {guildUser.GetFullMention()} добавлен в черный список", EmbedStyle.Successfull);
            }
            else
            {
                await ReplyEmbedStampAsync($"Не удалось добавить пользователя {guildUser.GetFullMention()} в черный список", EmbedStyle.Error);
            } 
        }


        [Command("Удалить")]
        [Alias("Убрать", "-")]
        [RequireConfirmAction(false)]
        public async Task RemoveFromBlockListAsync(IGuildUser guildUser)
        {
            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.GetFullMention()} не найден", EmbedStyle.Error);

                return;
            }

            if (!serverUser.IsBlocked)
            {
                await ReplyEmbedAsync($"Пользователя {guildUser.GetFullMention()} нет в черном списке", EmbedStyle.Warning);

                return;
            }

            serverUser.IsBlocked = false;

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
            {
                _cache.Set((guildUser.Id, Context.Guild.Id), serverUser, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(10),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                });

                await ReplyEmbedStampAsync($"Пользователь {guildUser.GetFullMention()} удален из черного списка", EmbedStyle.Successfull);
            }
            else
            {
                await ReplyEmbedStampAsync($"Не удалось удалить пользователя {guildUser.GetFullMention()} из черного списка", EmbedStyle.Error);
            }
        }
    }


    [Name("Уровни доступа")]
    [Group("УровниДоступа")]
    [Alias("УД", "Роли")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireStandartAccessLevel(StandartAccessLevel.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public class AccessLevelsModule : CommandGuildModuleBase
    {
        public AccessLevelsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Command("Текущий")]
        public async Task ShowAccessLevel(IGuildUser guildUser)
        {
            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.Mention} не найден", EmbedStyle.Error);

                return;
            }

            await ReplyEmbedAsync($"Текущий уровень доступа у пользователя {guildUser.Mention}: **`{serverUser.StandartAccessLevel?.ToString() ?? "уровень доступа отсутствует"}`**");
        }


        [Command("Назначить")]
        [RequireConfirmAction]
        public async Task SetAccessLevel(IGuildUser guildUser, StandartAccessLevel accessLevel)
        {
            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.Mention} не найден", EmbedStyle.Error);

                return;
            }

            if (serverUser.StandartAccessLevel == accessLevel)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.Mention} уже имеет данный уровень доступа", EmbedStyle.Error);

                return;
            }

            serverUser.StandartAccessLevel = accessLevel;


            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                await ReplyEmbedAsync($"Уровень доступа у пользователя {guildUser.Mention} успешно изменен", EmbedStyle.Successfull);
            else
                await ReplyEmbedAsync($"Не удалось иземнить уровень доступа у пользователя {guildUser.Mention}", EmbedStyle.Error);
        }


        [Command("Сбросить")]
        [RequireConfirmAction]
        public async Task ResetAccessLevel(IGuildUser guildUser)
        {
            var serverUser = await Context.Db.ServerUsers.FindAsync(guildUser.Id, Context.Guild.Id);

            if (serverUser is null)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.Mention} не найден", EmbedStyle.Error);

                return;
            }

            if (serverUser.StandartAccessLevel is null)
            {
                await ReplyEmbedAsync($"Пользователь {guildUser.Mention} отсутствует уровень доступа", EmbedStyle.Error);

                return;
            }

            serverUser.StandartAccessLevel = null;


            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                await ReplyEmbedAsync($"Уровень доступа у пользователя {guildUser.Mention} успешно изменен", EmbedStyle.Successfull);
            else
                await ReplyEmbedAsync($"Не удалось иземнить уровень доступа у пользователя {guildUser.Mention}", EmbedStyle.Error);
        }



        [Group("Расширенные")]
        [Alias("Экстра", "Доп")]
        public class ExtendedAccessLevelsModule : CommandGuildModuleBase
        {
            public ExtendedAccessLevelsModule(InteractiveService interactiveService) : base(interactiveService)
            {
            }


            [Command("Список")]
            public async Task ShowListAsync()
            {
                var accessLevels = await Context.Db.AccessLevels
                    .AsNoTracking()
                    .Where(al => al.ServerId == Context.Guild.Id)
                    .OrderByDescending(al => al.Priority)
                    .ToListAsync();


                if (accessLevels.Count == 0)
                {
                    await ReplyEmbedAsync("Уровни доступа отсутствуют", EmbedStyle.Error);

                    return;
                }


                var msg = string.Join('\n', accessLevels.Select(al => $"`{al.Name} ({al.Priority})`"));

                await ReplyEmbedAsync(msg);
            }


            [Command("Добавить")]
            [Alias("Доб", "+")]
            [RequireConfirmAction(false)]
            public async Task AddAccessLevelAsync(string name, int priority)
            {
                if (priority == int.MaxValue && Context.User.Id != BotOwner.Id)
                {
                    await ReplyEmbedAsync("Наивысший приоритет не может быть задан вручную", EmbedStyle.Error);

                    return;
                }

                var accessLevel = await Context.Db.AccessLevels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(al => al.ServerId == Context.Guild.Id && al.Name == name);

                if (accessLevel is not null)
                {
                    await ReplyEmbedAsync("Уровень доступа с таким именем уже существует", EmbedStyle.Error);

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
                    await ReplyEmbedAsync($"Уровень доступа **`{name} ({priority})`** успешно создан", EmbedStyle.Successfull);
                else
                    await ReplyEmbedAsync($"Не удалось создать уровень доступа **`{name} ({priority})`**", EmbedStyle.Error);
            }


            [Command("Удалить")]
            [Alias("Уд", "-")]
            [RequireConfirmAction(false)]
            public async Task RemoveAccessLevelAsync(string name)
            {
                var accessLevel = await Context.Db.AccessLevels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(al => al.ServerId == Context.Guild.Id && al.Name == name);

                if (accessLevel is null)
                {
                    await ReplyEmbedAsync("Уровень доступа с таким именем не существует", EmbedStyle.Error);

                    return;
                }

                Context.Db.AccessLevels.Remove(accessLevel);

                var n = await Context.Db.SaveChangesAsync();

                if (n > 0)
                    await ReplyEmbedAsync($"Уровень доступа **`{accessLevel.Name} ({accessLevel.Priority})`** успешно удален", EmbedStyle.Successfull);
                else
                    await ReplyEmbedAsync($"Не удалось удалить уровень доступа **`{accessLevel.Name} ({accessLevel.Priority})`**", EmbedStyle.Error);
            }
        }
    }
}