using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Services;

namespace Modules;

[Group("Админ")]
[Alias("а")]
[RequireContext(ContextType.Guild)]
public class AdminModule : GuildModuleBase
{
    [Command("Ник")]
    [RequireUserPermission(GuildPermission.ChangeNickname)]
    [RequireBotPermission(GuildPermission.ManageNicknames)]
    public async Task UpdateNickname(string nickName) =>
        await UpdateNickname((IGuildUser)Context.User, nickName);

    [Priority(-1)]
    [RequireUserPermission(GuildPermission.ManageNicknames)]
    [RequireBotPermission(GuildPermission.ManageNicknames)]
    [Command("Ник")]
    public async Task UpdateNickname(IGuildUser guildUser, string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Никнейм не может быть пустым");

            return;
        }

        if (nickname.Length > 32)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Никнейм не может быть длиннее, чем 32 символа");

            return;
        }


        await guildUser.ModifyAsync(props => props.Nickname = nickname);

        await ReplyEmbedAsync(EmbedStyle.Successfull, "Никнейм успешно измененен");
    }



    [Command("Слоумод")]
    [Alias("смод")]
    [Summary("Установить слоумод для канала (от 0 до 300 секунд)")]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task SetSlowMode([Summary("Количество секунд")] int secs)
    {
        if (Context.Channel is not ITextChannel textChannel)
            return;


        secs = Math.Clamp(secs, 0, 300);


        await textChannel.ModifyAsync(props => props.SlowModeInterval = secs);

        await textChannel.SendMessageAsync($"Слоумод успешно установлен на {secs}с");
    }



    [Command("Очистить")]
    [Alias("оч")]
    [Summary("Удалить сообщения из канала (от 0 до 100 сообщений)")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ClearAsync([Summary("Количество удаляемых сообщений")] int count)
    {
        if (Context.Channel is not ITextChannel textChannel)
            return;

        count = Math.Clamp(count, 0, 100);

        var messagesToDelete = (await textChannel
            .GetMessagesAsync(count + 1)
            .FlattenAsync())
            .Where(msg => (DateTime.UtcNow - msg.Timestamp).TotalDays <= 14);

        await textChannel.DeleteMessagesAsync(messagesToDelete);


        await ReplyEmbedAndDeleteAsync(EmbedStyle.Successfull, $"Сообщения успешно удалены ({count} шт)", withDefaultFooter: true);
    }



    [Command("Бан")]
    [Summary("Забанить указанного пользователя")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(IGuildUser guildUser, int pruneDays = 0, [Remainder] string? reason = null)
    {
        var guildSettings = await Context.GetGuildSettingsAsync();

        var confirmed = await ConfirmActionWithHandlingAsync($"Забанить {guildUser.GetFullName()}", guildSettings.LogChannelId);

        if (confirmed)
        {
            if (Context.Guild.GetUser(guildUser.Id) is not null)
            {
                await Context.Guild.AddBanAsync(guildUser, pruneDays, reason);

                await ReplyEmbedAsync(EmbedStyle.Successfull, $"Пользователь {guildUser.GetFullName()} успешно забанен", withDefaultFooter: true);
            }
            else
                await ReplyEmbedAsync(EmbedStyle.Error, $"Пользователь {guildUser.GetFullName()} не найден", withDefaultFooter: true);
        }
    }


    [Command("Разбан")]
    [Summary("Разбанить указанного пользователя")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task UnbanAsync(string str)
    {
        var arr = str.Replace("@", null).Split('#');

        if (arr.Length < 2)
        {
            await ReplyEmbedAsync(EmbedStyle.Warning, $"Пожалуйста, укажите имя пользователя и его тег. Пример: @{Context.User.GetFullName()}");

            return;
        }

        var userName = (arr[0], arr[1]);

        var bans = await Context.Guild.GetBansAsync();

        var user = bans.FirstOrDefault(ban => (ban.User.Username, ban.User.Discriminator) == userName)?.User;

        if (user == null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, $"Пользователь с именем {userName.Item1}#{userName.Item2} не найден в списке банов.");

            return;
        }

        await UnbanAsync(user.Id);
    }
    [Command("Разбан")]
    [Summary("Разбанить указанного пользователя")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task UnbanAsync(ulong id)
    {
        var bans = await Context.Guild.GetBansAsync();

        var user = bans.FirstOrDefault(ban => ban.User.Id == id)?.User;

        if (user == null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, $"Пользователь с айди {id} не найден в списке банов.");

            return;
        }

        var settings = await Context.GetGuildSettingsAsync();

        var confirmed = await ConfirmActionWithHandlingAsync($"Разбанить {user.GetFullName()}", settings.LogChannelId);


        if (confirmed)
        {
            await Context.Guild.RemoveBanAsync(user);

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Пользователь {user.GetFullName()} успешно разбанен", withDefaultFooter: true);
        }
    }



    [Command("Кик")]
    [Summary("Выгнать указанного пользователя")]
    [RequireBotPermission(GuildPermission.KickMembers)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task KickAsync(IGuildUser guildUser, [Remainder] string? reason = null)
    {
        var guildSettings = await Context.GetGuildSettingsAsync();

        var confirmed = await ConfirmActionWithHandlingAsync($"Выгнать {guildUser.GetFullName()}", guildSettings.LogChannelId);


        if (confirmed)
        {
            await guildUser.KickAsync(reason);

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Пользователь {guildUser.GetFullName()} успешно выгнан");
        }
    }


    [Command("Мьют")]
    [Alias("мут")]
    [Summary("Замьютить указанного пользователя")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task MuteAsync(IGuildUser guildUser)
    {
        var guildSettings = await Context.GetGuildSettingsAsync();


        var confirmed = await ConfirmActionWithHandlingAsync($"Замьютить {guildUser.GetFullName()}", guildSettings.LogChannelId);

        if (!confirmed)
            return;

        IRole? roleMute;

        if (guildSettings.RoleMuteId is not null)
            roleMute = Context.Guild.GetRole(guildSettings.RoleMuteId.Value);
        else
        {
            roleMute = await Context.Guild.CreateRoleAsync(
                "Muted",
                new GuildPermissions(sendMessages: false),
                Color.DarkerGrey,
                true,
                true
                );

            foreach (var channel in Context.Guild.Channels)
                await channel.AddPermissionOverwriteAsync(roleMute, OverwritePermissions.DenyAll(channel).Modify(
                    readMessageHistory: PermValue.Inherit,
                    viewChannel: PermValue.Inherit));

            guildSettings.RoleMuteId = roleMute.Id;

            await Context.Db.SaveChangesAsync();
        }

        await guildUser.AddRoleAsync(roleMute);


        await ReplyEmbedAsync(EmbedStyle.Successfull, $"Пользователь {guildUser.GetFullMention()} успешно замьючен", withDefaultFooter: true);
    }


    [Command("Размьют")]
    [Alias("размут")]
    [Summary("Размьютить указанного пользователя")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task UnmuteAsync(IGuildUser guildUser)
    {
        var guildSettings = await Context.GetGuildSettingsAsync();

        if (guildSettings is null)
            throw new NullReferenceException("Guild id was not found in database");

        if (guildSettings.RoleMuteId is null)
            throw new NullReferenceException($"[Guild {guildSettings.Id}] Role id is null");


        if (!guildUser.RoleIds.Contains(guildSettings.RoleMuteId.Value))
        {
            await ReplyEmbedAsync(EmbedStyle.Information, "Пользователь не замьючен");

            return;
        }


        var confirmed = await ConfirmActionWithHandlingAsync($"Размьютить {guildUser.GetFullName()}", guildSettings.LogChannelId);


        if (confirmed)
        {
            await guildUser.RemoveRoleAsync(guildSettings.RoleMuteId.Value);

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Пользователь {guildUser.GetFullMention()} успешно размьючен", withDefaultFooter: true);
        }
    }


    [Command("Рольправа")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task ShowRolePermissionsAsync(IRole role)
    {
        var str = "\n";

        var allPerms = GuildPermissions.All.ToList();
        var rolePerms = role.Permissions.ToList();

        var builder = new EmbedBuilder()
            .WithInformationMessage(false);

        for (int i = 0; i < allPerms.Count; i++)
        {
            str += $"{(rolePerms.Contains(allPerms[i]) ? "✅" : "❌")} {allPerms[i]}\n";
        }

        builder.AddField($"Права роли {role}", str);

        await ReplyAsync(embed: builder.Build());
    }

    [Command("каналправа")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task ShowChannelOverwritePermissionsAsync(IRole role, IGuildChannel guildChannel)
    {
        var rolePerms = guildChannel.GetPermissionOverwrite(role)?.ToAllowList();

        if (rolePerms is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, $"Для роли **{role}** нет переопределений в канале **{guildChannel}**");

            return;
        }


        var str = "\n";
        var allPerms = OverwritePermissions.AllowAll(guildChannel).ToAllowList();


        var builder = new EmbedBuilder()
            .WithInformationMessage(false);

        for (int i = 0; i < allPerms.Count; i++)
        {
            str += $"{(rolePerms.Contains(allPerms[i]) ? "✅" : "❌")} {allPerms[i]}\n";
        }

        builder.AddField($"Права роли {role} в канале {guildChannel}", str);

        await ReplyAsync(embed: builder.Build());
    }

    [Command("каналправа")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task ShowChannelOverwritePermissionsAsync(IGuildUser guildUser, IGuildChannel guildChannel)
    {
        var rolePerms = guildChannel.GetPermissionOverwrite(guildUser)?.ToAllowList();

        if (rolePerms is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, $"Для пользователя **{guildUser}** нет переопределений в канале **{guildChannel}**");

            return;
        }


        var str = "\n";
        var allPerms = OverwritePermissions.AllowAll(guildChannel).ToAllowList();


        var builder = new EmbedBuilder()
            .WithInformationMessage(false);

        for (int i = 0; i < allPerms.Count; i++)
        {
            str += $"{(rolePerms.Contains(allPerms[i]) ? "✅" : "❌")} {allPerms[i]}\n";
        }

        builder.AddField($"Права пользователя {guildUser} в канале {guildChannel}", str);

        await ReplyAsync(embed: builder.Build());
    }


    [Group("Смайл")]
    public class SmileModule : GuildModuleBase
    {
        [Priority(-1)]
        [Command("Добавить")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddEmoteAsync()
            => await AddEmoteAsync($"emoji_{Context.Guild.Emotes.Count + 1}");

        [Command("Добавить")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddEmoteAsync(string name)
        {
            var attachment = Context.Message.Attachments.FirstOrDefault() ?? Context.Message.ReferencedMessage?.Attachments.FirstOrDefault();


            if (attachment is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Прикрепите к своему сообщению картинку, или ответьте на сообщение, содержащее картинку");

                return;
            }

            if (attachment.Height > 256 || attachment.Width > 256)
            {
                await ReplyEmbedAsync(EmbedStyle.Error,
                    $"Изображение имеет слишком большие размеры. Допустимый размер: 256х256; Размер изображения: {attachment.Width}x{attachment.Height}");

                return;
            }

            var stream = await new HttpClient().GetStreamAsync(attachment.Url);

            if (stream is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Не удалось загрузить картинку");

                return;
            }

            var emote = await Context.Guild.CreateEmoteAsync(name, new Image(stream));


            var msg = await ReplyEmbedAsync(EmbedStyle.Successfull, "Смайл успешно добавлен");

            await msg.AddReactionAsync(emote);
        }


        [Command("Удалить")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteEmoteAsync(Emote emote)
        {
            if (!Context.Guild.Emotes.Contains(emote))
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Смайл не найден");

                return;
            }

            var guildEmote = await Context.Guild.GetEmoteAsync(emote.Id);

            await Context.Guild.DeleteEmoteAsync(guildEmote);

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Смайл успешно удален");
        }
    }


    [RequireOwner()]
    [Group]
    public class OwnerModule : GuildModuleBase
    {
        [RequireOwner]
        [Command("лог")]
        public async Task GetFileLogTodayAsync()
        {
            using var filestream = LoggingService.GetGuildLogFileToday(Context.Guild.Id);

            if (filestream is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Файл не найден");

                return;
            }

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Файл успешно отправлен");

            await Context.User.SendFileAsync(filestream, filestream.Name);
        }


        [RequireOwner]
        [Command("рестарт")]
        public async Task RestartAsync()
        {
            await ReplyEmbedAsync(EmbedStyle.Information, "Перезапуск...");

            Log.Debug("({0:l}): Restart request received from server {1} by user {2}",
                      nameof(RestartAsync),
                      Context.Guild.Name,
                      Context.User.GetFullName());

            System.Diagnostics.Process.Start(Assembly.GetEntryAssembly()!.Location.Replace("dll", "exe"));

            Environment.Exit(0);
        }
    }
}