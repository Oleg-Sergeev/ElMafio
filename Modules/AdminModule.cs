using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Modules.Extensions;
using Serilog;
using Services;

namespace Modules;

[Group("Админ")]
[Alias("а")]
[RequireContext(ContextType.Guild)]
public class AdminModule : ModuleBase<SocketCommandContext>
{
    private readonly BotContext _db;


    public AdminModule(BotContext db)
    {
        _db = db;
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

        var messagesToDelete = await textChannel.GetMessagesAsync(count + 1).FlattenAsync();

        await textChannel.DeleteMessagesAsync(messagesToDelete);


        var msg = await textChannel.SendMessageAsync("Сообщения успешно удалены");

        await Task.Delay(2000);

        await textChannel.DeleteMessageAsync(msg);
    }



    [Command("Бан")]
    [Summary("Забанить указанного пользователя")]
    [RequireBotPermission(GuildPermission.BanMembers)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task BanAsync(IGuildUser guildUser, int pruneDays = 0, [Remainder] string? reason = null)
    {
        await Context.Guild.AddBanAsync(guildUser, pruneDays, reason);

        await ReplyAsync($"Пользователь {guildUser.GetFullName()} успешно забанен");
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
            await ReplyAsync($"Пожалуйста, укажите имя пользователя и его тег. Пример: @{Context.User.GetFullName()}");

            return;
        }

        var userName = (arr[0], arr[1]);

        var bans = await Context.Guild.GetBansAsync();

        var user = bans.FirstOrDefault(ban => (ban.User.Username, ban.User.Discriminator) == userName)?.User;

        if (user == null)
        {
            await ReplyAsync($"Пользователь с именем {userName.Item1}#{userName.Item2} не найден в списке банов.");

            return;
        }

        await Context.Guild.RemoveBanAsync(user);

        await ReplyAsync($"Пользователь {user.GetFullName()} успешно разбанен");
    }



    [Command("Кик")]
    [Summary("Выгнать указанного пользователя")]
    [RequireBotPermission(GuildPermission.KickMembers)]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task KickAsync(IGuildUser guildUser, [Remainder] string? reason = null)
    {
        await guildUser.KickAsync(reason);

        await ReplyAsync($"Пользователь {guildUser.GetFullName()} успешно выгнан");
    }


    [Command("Мьют")]
    [Alias("мут")]
    [Summary("Замьютить указанного пользователя")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task MuteAsync(IGuildUser guildUser)
    {
        var settings = await _db.GuildSettings.FindAsync(Context.Guild.Id);

        if (settings is null)
            throw new NullReferenceException("Guild id was not found in database");

        IRole? roleMute;

        if (settings.RoleMuteId is not null)
            roleMute = Context.Guild.GetRole(settings.RoleMuteId.Value);
        else
        {
            roleMute = await Context.Guild.CreateRoleAsync(
                "Muted",
                new GuildPermissions(sendMessages: false),
                Color.DarkerGrey,
                false,
                true
                );

            foreach (var channel in Context.Guild.Channels)
                await channel.AddPermissionOverwriteAsync(roleMute, OverwritePermissions.DenyAll(channel).Modify(
                    readMessageHistory: PermValue.Inherit,
                    viewChannel: PermValue.Inherit));

            settings.RoleMuteId = roleMute.Id;

            await _db.SaveChangesAsync();
        }

        await guildUser.AddRoleAsync(roleMute);

        await ReplyAsync($"Пользователь {guildUser.GetFullMention()} успешно замьючен");
    }

    [Command("Размьют")]
    [Alias("размут")]
    [Summary("Размьютить указанного пользователя")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task UnmuteAsync(IGuildUser guildUser)
    {
        var settings = await _db.GuildSettings.FindAsync(Context.Guild.Id);

        if (settings is null)
            throw new NullReferenceException("Guild id was not found in database");

        if (settings.RoleMuteId is null)
            throw new NullReferenceException($"[Guild {settings.Id}] Role id is null");


        if (!guildUser.RoleIds.Contains(settings.RoleMuteId.Value))
        {
            await ReplyAsync("Пользователь не замьючен");

            return;
        }

        await guildUser.RemoveRoleAsync(settings.RoleMuteId.Value);

        await ReplyAsync($"Пользователь {guildUser.GetFullMention()} успешно размьючен");
    }


    [RequireOwner]
    [Command("лог")]
    public async Task GetFileLogTodayAsync()
    {
        using var filestream = LoggingService.GetGuildLogFileToday(Context.Guild.Id);

        if (filestream is null)
        {
            await ReplyAsync("Файл не найден");

            return;
        }

        await ReplyAsync("Файл успешно отправлен");

        await Context.User.SendFileAsync(filestream, filestream.Name);
    }


    [RequireOwner]
    [Command("рестарт")]
    public async Task RestartAsync()
    {
        await ReplyAsync("Перезапуск...");

        Log.Debug("({0:l}): Restart request received from server {1} by user {2}",
                  nameof(RestartAsync),
                  Context.Guild.Name,
                  Context.User.GetFullName());

        System.Diagnostics.Process.Start(Assembly.GetEntryAssembly()!.Location.Replace("dll", "exe"));

        Environment.Exit(0);
    }
}
