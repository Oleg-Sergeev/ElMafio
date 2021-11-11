using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Infrastructure.Data;

namespace Modules;

[Group("Настройки")]
[Alias("н")]
[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
public class SettingsModule : GuildModuleBase
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


        await ReplyEmbedAsync(EmbedType.Successfull, $"Префикс успешно изменен с **{oldPrefix}** на **{newPrefix}**");
    }

    [Command("рольмут")]
    public async Task UpdateMuteRoleAsync([Summary("Роль для мута")] IRole newMuteRole)
        => await UpdateMuteRoleAsync(newMuteRole.Id);

    [Command("рольмут")]
    public async Task UpdateMuteRoleAsync([Summary("ID роли для мута")] ulong newMuteRoleId)
    {
        var guildSettings = await _db.GuildSettings.FindAsync(Context.Guild.Id);

        if (guildSettings is null)
            throw new ArgumentException($"Guild with id [{Context.Guild.Id}] was not found in database.");

        var muteRole = Context.Guild.GetRole(newMuteRoleId);

        if (muteRole is null)
        {
            await ReplyEmbedAsync(EmbedType.Error, "Указанный ID роли недействителен");

            return;
        }

        guildSettings.RoleMuteId = newMuteRoleId;

        await _db.SaveChangesAsync();


        await ReplyEmbedAsync(EmbedType.Successfull, $"Роль успешна установлена [{muteRole.Mention}]", withDefaultFooter: true);
    }


    [Command("каналлог")]
    public async Task UpdateLogChannelAsync([Summary("Канал для логов")] ITextChannel logChannel)
        => await UpdateLogChannelAsync(logChannel.Id);

    [Command("каналлог")]
    public async Task UpdateLogChannelAsync([Summary("ID канала для логов")] ulong logChannelId)
    {
        var guildSettings = await _db.GuildSettings.FindAsync(Context.Guild.Id);

        if (guildSettings is null)
            throw new ArgumentException($"Guild with id [{Context.Guild.Id}] was not found in database.");


        var logChannel = Context.Guild.GetTextChannel(logChannelId);

        if (logChannel is null)
        {
            await ReplyEmbedAsync(EmbedType.Error, "Указанный ID канала недействителен");

            return;
        }

        guildSettings.LogChannelId = logChannelId;

        await _db.SaveChangesAsync();


        await ReplyEmbedAsync(EmbedType.Successfull, $"Канал для логов успешно установлен [{logChannel.Mention}]", withDefaultFooter: true);
    }
}