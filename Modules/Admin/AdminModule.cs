using System;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Services;

namespace Modules.Admin;

[Group("Админ")]
[Alias("а")]
[Summary("Набор команд для управления сервером и получения полезной информации")]
public class AdminModule : GuildModuleBase
{
    public AdminModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Слоумод")]
    [Alias("смод")]
    [Summary("Установить слоумод в текущем канале")]
    [Remarks("Диапазон интервала: `0-300с`")]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    [RequireUserPermission(GuildPermission.ManageChannels)]
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
    [RequireUserPermission(GuildPermission.Administrator)]
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
    [RequireUserPermission(GuildPermission.Administrator)]
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
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task UpdateRoleColorAsync([Summary("Роль, для которой нужно изменить цвет")] IRole role, [Summary("Новый цвет")] Color color)
    {
        var guildUser = (IGuildUser)Context.User;

        if (guildUser.Hierarchy < role.Position)
        {
            await ReplyEmbedAsync("Невозможно управлять ролью: недостаточно полномочий", EmbedStyle.Error);

            return;
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



    [Group("Смайл")]
    [Alias("С")]
    [Summary("Добавление и удаление пользовательских смайлов")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class SmileModule : GuildModuleBase
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


    [RequireOwner]
    [Group]
    public class OwnerModule : GuildModuleBase
    {
        public OwnerModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Command("Лог")]
        public async Task GetFileLogTodayAsync()
        {
            using var filestream = LoggingService.GetGuildLogFileToday(Context.Guild.Id);

            if (filestream is null)
            {
                await ReplyEmbedAsync("Файл не найден", EmbedStyle.Error);

                return;
            }

            await ReplyEmbedAsync("Файл успешно отправлен", EmbedStyle.Successfull);

            await Context.User.SendFileAsync(filestream, filestream.Name);
        }


        [Command("Рестарт")]
        public async Task RestartAsync()
        {
            await ReplyEmbedAsync("Перезапуск...", EmbedStyle.Debug);

            Log.Debug("({0:l}): Restart request received from server {1} by user {2}",
                      nameof(RestartAsync),
                      Context.Guild.Name,
                      Context.User.GetFullName());

            System.Diagnostics.Process.Start(Assembly.GetEntryAssembly()!.Location.Replace("dll", "exe"));

            Environment.Exit(0);
        }
    }
}