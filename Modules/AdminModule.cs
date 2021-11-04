using System;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Data.Models;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Modules.Extensions;
using Infrastructure;
using Infrastructure.Data;

namespace Modules
{
    [Group("админ")]
    [Alias("а", "admin", "a")]
    [RequireContext(ContextType.Guild)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotContext _db;


        public AdminModule(BotContext db)
        {
            _db = db;
        }


        [Command("слоумод")]
        [Alias("slowmode")]
        [Summary("Установить слоумод для канала (от 0 до 300 секунд)")]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetSlowMode(int secs)
        {
            if (Context.Channel is not ITextChannel textChannel) return;


            secs = Math.Clamp(secs, 0, 300);


            await textChannel.ModifyAsync(props => props.SlowModeInterval = secs);

            await textChannel.SendMessageAsync($"Слоумод успешно установлен на {secs}с");
        }



        [Command("очистить")]
        [Alias("clear")]
        [Summary("Удалить сообщения из канала (от 0 до 100 сообщений)")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearAsync(int count)
        {
            if (Context.Channel is not ITextChannel textChannel) return;


            count = Math.Clamp(count, 0, 100);

            var messagesToDelete = await textChannel.GetMessagesAsync(count + 1).FlattenAsync();

            await textChannel.DeleteMessagesAsync(messagesToDelete);


            var msg = await textChannel.SendMessageAsync("Сообщения успешно удалены");

            await Task.Delay(2000);

            await textChannel.DeleteMessageAsync(msg);
        }



        [Command("бан")]
        [Alias("ban")]
        [Summary("Забанить указанного пользователя")]
        [RequireBotPermission(GuildPermission.BanMembers)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(IGuildUser guildUser, int pruneDays = 0, [Remainder] string? reason = null)
        {
            await Context.Guild.AddBanAsync(guildUser, pruneDays, reason);

            await ReplyAsync($"Пользователь {guildUser.GetFullName()} успешно забанен");
        }



        [Command("разбан")]
        [Alias("unban")]
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



        [Command("кик")]
        [Alias("kick")]
        [Summary("Выгнать указанного пользователя")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task KickAsync(IGuildUser guildUser, [Remainder] string? reason = null)
        {
            await guildUser.KickAsync(reason);

            await ReplyAsync($"Пользователь {guildUser.GetFullName()} успешно выгнан");
        }

        [Command("мьют")]
        [Alias("мут")]
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

        [Command("размьют")]
        [Alias("размут")]
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
        [Command("getlogtoday")]
        public async Task GetFileLogAsync()
        {
            var filepath = LoggingService.GetGuildLogFilePathToday(Context.Guild.Id);

            if (filepath is not null)
                await Context.User.SendFileAsync(filepath);
            else
                await Context.User.SendMessageAsync($"File not found. Filepath: {filepath}");
        }


        [RequireOwner]
        [Command("add")]
        public async Task AddUserToDb(IGuildUser guildUser)
        {
            if (_db.Users.Any(u => u.Id == guildUser.Id))
            {
                await ReplyAsync($"{guildUser.Id} already exists");

                return;
            }

            var user = new User
            {
                Id = guildUser.Id,
                JoinedAt = guildUser.JoinedAt!.Value.DateTime
            };

            await _db.Users.AddAsync(user);

            await ReplyAsync("Add user");


            if (!_db.MafiaStats.Any(s => s.UserId == guildUser.Id))
            {
                await _db.MafiaStats.AddAsync(new MafiaStats
                {
                    User = user
                });

                await ReplyAsync("Add mafia stats");
            }

            if (!_db.RussianRouletteStats.Any(s => s.UserId == guildUser.Id))
            {
                await _db.RussianRouletteStats.AddAsync(new RussianRouletteStats
                {
                    User = user
                });

                await ReplyAsync("Add russian roullete stats");
            }


            await _db.SaveChangesAsync();


            await ReplyAsync("Saved");
        }

        [RequireOwner]
        [Command("get")]
        public async Task GetUserInfo(IGuildUser guildUser)
        {
            var user = await _db.Users.FindAsync(guildUser.Id);

            if (user is null)
            {
                await ReplyAsync("User not found");

                return;
            }

            await ReplyAsync($"Found user\nID: [{user.Id}]\nJoined at: {user.JoinedAt}");
        }

        [RequireOwner]
        [Command("getm")]
        public async Task GetMafiaStat(IGuildUser guildUser)
        {
            var mafiaStat = await _db.MafiaStats
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == guildUser.Id);

            if (mafiaStat is null)
            {
                await ReplyAsync("Mafia stats not found");

                return;
            }

            await ReplyAsync("Found");

            await ReplyAsync($"{mafiaStat.TotalWinRate.ToPercent()}");
        }

        [RequireOwner]
        [Command("getrr")]
        public async Task GetRussianRoulleteStat(IGuildUser guildUser)
        {
            var rrStat = await _db.RussianRouletteStats
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == guildUser.Id);

            if (rrStat is null)
            {
                await ReplyAsync("Roullete stats not found");

                return;
            }

            await ReplyAsync("Found");

            await ReplyAsync($"{rrStat}");
        }
    }
}
