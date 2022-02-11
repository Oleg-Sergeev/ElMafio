using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Services;

namespace Modules.Admin;

[Group("Админ")]
[Alias("а")]
[RequireContext(ContextType.Guild)]
public class AdminModule : GuildModuleBase
{
    public AdminModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Слоумод")]
    [Alias("смод")]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task SetSlowMode([Summary("Количество секунд")] int secs)
    {
        if (Context.Channel is not ITextChannel textChannel)
            return;


        secs = Math.Clamp(secs, 0, 300);


        await textChannel.ModifyAsync(props => props.SlowModeInterval = secs);

        await ReplyEmbedStampAsync($"Слоумод успешно установлен на {secs}с", EmbedStyle.Successfull);
    }



    [Command("Очистить")]
    [Alias("оч")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ClearAsync(int count)
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
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ClearToAsync(ulong? messageId = null)
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
    [Alias("Рц")]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task UpdateRoleColor(IRole role, Color color)
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

        await ReplyEmbedStampAsync($"Старый цвет: {oldColor} ({oldColor.R}, {oldColor.G}, {oldColor.B})",
            EmbedStyle.Successfull,
            "Цвет роли успешно изменен");
    }




    [Command("Рольправа")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task ShowRolePermissionsAsync(IRole role)
    {
        var str = "\n";

        var allPerms = GuildPermissions.All.ToList();
        var rolePerms = role.Permissions.ToList();

        var builder = new EmbedBuilder().WithInformationMessage();

        for (int i = 0; i < allPerms.Count; i++)
            str += $"{(rolePerms.Contains(allPerms[i]) ? "✅" : "❌")} {allPerms[i]}\n";

        builder.AddField($"Права роли {role}", str);

        await ReplyAsync(embed: builder.Build());
    }


    [Command("Каналправа")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task ShowChannelOverwritePermissionsAsync(IRole role, IGuildChannel guildChannel)
    {
        var overwritePerms = guildChannel.GetPermissionOverwrite(role);

        var allowPerms = overwritePerms?.ToAllowList();
        var denyPerms = overwritePerms?.ToDenyList();


        if (allowPerms is null && denyPerms is null)
        {
            await ReplyEmbedAsync($"Для роли **{role.Mention}** нет переопределений в канале **{guildChannel}**", EmbedStyle.Warning);

            return;
        }


        allowPerms ??= new();
        denyPerms ??= new();

        var allPerms = OverwritePermissions.AllowAll(guildChannel).ToAllowList();


        var str = GetPermissionsString(allPerms, allowPerms, denyPerms);


        var embed = new EmbedBuilder()
            .WithInformationMessage()
            .AddField($"Права роли {role.Mention} в канале {guildChannel}", str)
            .Build();

        await ReplyAsync(embed: embed);
    }


    [Command("Каналправа")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task ShowChannelOverwritePermissionsAsync(IGuildUser guildUser, IGuildChannel guildChannel)
{
        var overwritePerms = guildChannel.GetPermissionOverwrite(guildUser);

        var allowPerms = overwritePerms?.ToAllowList();
        var denyPerms = overwritePerms?.ToDenyList();

        if (allowPerms is null && denyPerms is null)
        {
            await ReplyEmbedAsync($"Для пользователя {guildUser.Mention} нет переопределений в канале **{guildChannel}**", EmbedStyle.Warning);

            return;
        }

        allowPerms ??= new();
        denyPerms ??= new();

        var allPerms = OverwritePermissions.AllowAll(guildChannel).ToAllowList();


        var str = GetPermissionsString(allPerms, allowPerms, denyPerms);

        var embed = new EmbedBuilder()
            .WithInformationMessage()
            .AddField($"Права пользователя {guildUser} в канале {guildChannel}", str)
            .Build();

        await ReplyAsync(embed: embed);
    }


    private static string GetPermissionsString(List<ChannelPermission> allPerms, List<ChannelPermission>? allowPerms, List<ChannelPermission>? denyPerms)
    {
        allowPerms ??= new();
        denyPerms ??= new();

        var str = "\n";

        for (int i = 0; i < allPerms.Count; i++)
        {
            if (allowPerms.Contains(allPerms[i]))
                str += "✅ ";
            else if (denyPerms.Contains(allPerms[i]))
                str += "❌ ";
            else
                str += "▫️ ";

            str += $"{allPerms[i]}\n";
        }

        return str;
    }


    [Group("Смайл")]
    [Alias("С")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class SmileModule : GuildModuleBase
    {
        public SmileModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Priority(-1)]
        [Command("Добавить")]
        [Alias("+")]
        public async Task AddEmoteAsync()
            => await AddEmoteAsync($"emoji_{Context.Guild.Emotes.Count + 1}");

        [Command("Добавить")]
        [Alias("+")]
        public async Task AddEmoteAsync(string name)
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
                await ReplyEmbedAsync("Не удалось загрузить картинку", EmbedStyle.Error);

                return;
            }

            var image = new Image(stream);

            var emote = await Context.Guild.CreateEmoteAsync(name, image);


            var msg = await ReplyEmbedAsync("Смайл успешно добавлен", EmbedStyle.Successfull);

            await msg.AddReactionAsync(emote);
        }


        [Command("Удалить")]
        [Alias("-")]
        public async Task DeleteEmoteAsync(Emote emote)
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