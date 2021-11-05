using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Infrastructure.Data;

namespace Modules
{
    [Group("Настройки")]
    [Alias("н")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class SettingsModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotContext _db;


        public SettingsModule(BotContext db)
        {
            _db = db;
        }


        [Command("Префикс")]
        [Alias("преф", "п")]
        [Summary("Изменить префикс бота")]
        public async Task UpdatePrefixAsync([Summary("Новый префикс бота")] string newPrefix)
        {
            var guildSettings = await _db.GuildSettings.FindAsync(Context.Guild.Id);

            if (guildSettings is null)
                throw new ArgumentException($"Guild with id [{Context.Guild.Id}] was not found in database.");

            var oldPrefix = guildSettings.Prefix;
            guildSettings.Prefix = newPrefix;

            await _db.SaveChangesAsync();


            await ReplyAsync($"Префикс успешно изменен с **{oldPrefix}** на **{newPrefix}**");
        }
    }
}
