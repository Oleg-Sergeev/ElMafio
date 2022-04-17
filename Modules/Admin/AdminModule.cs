using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Infrastructure.Data.Entities;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Modules.Common.Preconditions.Commands;

namespace Modules.Admin;

[Group("Админ")]
[Alias("а")]
[Summary("Набор команд для управления сервером и получения полезной информации")]
[RequireOwner(Group = "user")]
[RequireUserPermission(ChannelPermission.ManageChannels, Group = "user")]
[RequireUserPermission(ChannelPermission.ManageMessages, Group = "user")]
[RequireUserPermission(GuildPermission.ManageRoles, Group = "user")]
[RequireStandartAccessLevel(StandartAccessLevel.Moderator, Group = "user")]
public class AdminModule : CommandGuildModuleBase
{
    private readonly AdminService _adminService;


    public AdminModule(InteractiveService interactiveService, AdminService adminService) : base(interactiveService)
    {
        _adminService = adminService;
    }


    [Command("Слоумод")]
    [Alias("смод")]
    [Summary("Установить слоумод в текущем канале")]
    [Remarks("Диапазон интервала: `0-300с`")]
    [RequireBotPermission(ChannelPermission.ManageChannels)]
    [RequireUserPermission(ChannelPermission.ManageChannels)]
    public async Task SetSlowMode([Summary("Интервал слоумода")] int secs)
    {
        if (Context.Channel is not ITextChannel textChannel)
            return;

        secs = Math.Clamp(secs, 0, 300);

        await _adminService.SetSlowModeAsync(textChannel, secs);

        await ReplyEmbedStampAsync($"Слоумод успешно установлен на {secs}с", EmbedStyle.Successfull);
    }



    [Command("Очистить")]
    [Alias("Удалить", "Оч")]
    [Summary("Удалить указанное кол-во сообщений из текущего канала")]
    [Remarks("Максимальное кол-во сообщений, удаляемое за раз: `100`")]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    public async Task ClearAsync([Summary("Кол-во удаляемых сообщений")] int count)
    {
        if (Context.Channel is not ITextChannel textChannel)
        {
            await ReplyEmbedAndDeleteAsync("Невозможно удалить сообщения. Возможно вы указали не текстовый канал", EmbedStyle.Error);

            return;
        }

        count = Math.Clamp(count, 0, 100);

        await _adminService.ClearAsync(textChannel, count + 1);

        await ReplyEmbedAndDeleteAsync($"Сообщения успешно удалены ({count} шт)", EmbedStyle.Successfull);
    }


    [Command("ОчиститьДо")]
    [Alias("УдалитьДо", "ОчДо")]
    [Summary("Удалить сообщения до указанного (удаляются сообщения, идущие **`после`** указанного, **`включая`** указанное)")]
    [Remarks("Максимальное кол-во сообщений, удаляемое за раз: `100`\n**Также можно ответить на сообщение, до которого нужно удалить все сообщения**")]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    public async Task ClearAsync([Summary("ID сообщения, до которого нужно удалить сообщения")] ulong? messageId = null)
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


        var count = await _adminService.ClearAsync(textChannel, message);


        await ReplyEmbedAndDeleteAsync($"Сообщения успешно удалены ({count} шт)", EmbedStyle.Successfull);
    }


    [Command("ОчиститьДиапазон")]
    [Alias("УдалитьДпз", "ОчДпз")]
    [Summary("Удалить сообщения в указанном диапазоне (удаляются сообщения, находящиеся **`между`** указанными сообщениями, **`включая`** сами границы)")]
    [Remarks("Максимальное кол-во сообщений, удаляемое за раз: `100`")]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    public async Task ClearAsync([Summary("Нижняя граница удаляемых сообщений")] ulong fromId,
                                   [Summary("Верхняя граница удаляемых сообщений")] ulong toId)
    {
        if (Context.Channel is not ITextChannel textChannel)
        {
            await ReplyEmbedAndDeleteAsync("Невозможно удалить сообщения из данного канала", EmbedStyle.Error);

            return;
        }

        if (await textChannel.GetMessageAsync(fromId) is not IMessage from)
        {
            await ReplyEmbedAndDeleteAsync("Нижняя граница сообщений не найдена", EmbedStyle.Error);

            return;
        }

        if (await textChannel.GetMessageAsync(toId) is not IMessage to)
        {
            await ReplyEmbedAndDeleteAsync("Верхняя граница сообщений не найдена", EmbedStyle.Error);

            return;
        }


        var count = await _adminService.ClearAsync(textChannel, from, to);

        try { await Context.Message.DeleteAsync(); }
        catch { }

        await ReplyEmbedAndDeleteAsync($"Сообщения успешно удалены ({count} шт)", EmbedStyle.Successfull);
    }



    [Command("РольЦвет")]
    [Alias("РЦ")]
    [Summary("Изменить цвет роли на новый")]
    [Remarks("Форматы ввода цвета:" +
        "\n**0-255:** `(r, g, b)` **Пример:** `(150, 255, 0)`" +
        "\n**0-1:** `(r, g, b)` **Пример:** `(0.4, 0, 1)`" +
        "\n**HEX:** `#RRGGBB` **Пример:** `#280af0`")]
    [RequireBotPermission(ChannelPermission.ManageRoles)]
    [RequireUserPermission(ChannelPermission.ManageRoles)]
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

        await _adminService.UpdateRoleColorAsync(role, color);

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

            Stream? compressedAvatar = null;
            if (attachment.Size > 256 * 1024)
            {
                var attachmentResponse = await new HttpClient().GetAsync(attachment.Url);

                var attachmentStream = await attachmentResponse.Content.ReadAsStreamAsync();

                compressedAvatar = await CompressAvatarAsync(attachmentStream);

                ReplyEmbedAsync("Compressing...", EmbedStyle.Debug).Wait();

                if (compressedAvatar.Length > 256 * 1024)
                {
                    await ReplyEmbedAsync($"Изображение имеет слишком большой размер. Допустимый размер: 256Кб; Размер изображения: {attachment.Size / 1024}Кб",
                        EmbedStyle.Error);

                    return;
                }
            }


            var stream = compressedAvatar ?? await httpClient.GetStreamAsync(attachment.Url);

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




        private async Task<Stream> CompressAvatarAsync(Stream avatarStream)
        {
            using HttpClient client = new();

            MultipartFormDataContent form = new();
            form.Add(new StreamContent(avatarStream), "uploadfile", "avatar.png");
            form.Add(new StringContent("1"), "sizeperc");
            form.Add(new StringContent("256"), "kbmbsize");
            form.Add(new StringContent("1"), "kbmb");
            form.Add(new StringContent("1"), "mpxopt");
            form.Add(new StringContent("1"), "jpegtype");
            form.Add(new StringContent("1"), "jpegmeta");

            var result = await client.PostAsync("https://www.imgonline.com.ua/compress-image-size-result.php", form);

            var htmlStream = await result.Content.ReadAsStreamAsync();

            string content;

            using (StreamReader reader = new(htmlStream))
                content = reader.ReadToEnd();

            var href = content[content.IndexOf("https")..];

            var imgUrl = href[..(href.IndexOf(".jpg") + 4)];

            var compressedAvatar = await new HttpClient().GetAsync(imgUrl);

            var compressedAvatarStream = await compressedAvatar.Content.ReadAsStreamAsync();

            return compressedAvatarStream;
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
}