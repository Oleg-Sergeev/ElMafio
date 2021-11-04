﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Infrastructure.Data;

namespace Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("настройки")]
    [Alias("н", "settings", "s")]
    public class SettingsModule : ModuleBase<SocketCommandContext>
    {
        private readonly BotContext _db;


        public SettingsModule(BotContext db)
        {
            _db = db;
        }


        [Command("префикс")]
        [Alias("prefix")]
        public async Task UpdatePrefixAsync(string? newPrefix)
        {
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                await ReplyAsync("Префикс не может быть пустой строкой");

                return;
            }


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
