using System.Threading.Tasks;
using Core.Common;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules;

[Group("Настройки")]
[Alias("н")]
[RequireContext(ContextType.Guild)]
[RequireUserPermission(GuildPermission.Administrator)]
public class SettingsModule : GuildModuleBase
{
    public SettingsModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Префикс")]
    [Alias("преф", "п")]
    [Summary("Изменить префикс бота")]
    public async Task UpdatePrefixAsync([Summary("Новый префикс бота")] string newPrefix)
    {
        var guildSettings = await Context.GetGuildSettingsAsync();

        var oldPrefix = guildSettings.Prefix;
        guildSettings.Prefix = newPrefix;

        await Context.Db.SaveChangesAsync();


        await ReplyEmbedAsync(EmbedStyle.Successfull, $"Префикс успешно изменен с **{oldPrefix}** на **{newPrefix}**");
    }


    [Command("рольмут")]
    public async Task UpdateMuteRoleAsync([Summary("Роль для мута")] IRole newMuteRole)
        => await UpdateMuteRoleAsync(newMuteRole.Id);

    [Command("рольмут")]
    public async Task UpdateMuteRoleAsync([Summary("ID роли для мута")] ulong newMuteRoleId)
    {
        var muteRole = Context.Guild.GetRole(newMuteRoleId);

        if (muteRole is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Роль с указанным ID не найдена");

            return;
        }

        var guildSettings = await Context.GetGuildSettingsAsync();

        guildSettings.RoleMuteId = newMuteRoleId;

        await Context.Db.SaveChangesAsync();


        await ReplyEmbedStampAsync(EmbedStyle.Successfull, $"Роль [{muteRole.Mention}] успешна установлена");
    }


    [Command("каналлог")]
    public async Task UpdateLogChannelAsync([Summary("Канал для логов")] ITextChannel logChannel)
        => await UpdateLogChannelAsync(logChannel.Id);

    [Command("каналлог")]
    public async Task UpdateLogChannelAsync([Summary("ID канала для логов")] ulong logChannelId)
    {
        var logChannel = Context.Guild.GetTextChannel(logChannelId);

        if (logChannel is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Канал с указанным ID не найден");

            return;
        }

        var guildSettings = await Context.GetGuildSettingsAsync();

        guildSettings.LogChannelId = logChannelId;

        await Context.Db.SaveChangesAsync();


        await ReplyEmbedStampAsync(EmbedStyle.Successfull, $"Канал для логов [{logChannel.Mention}] успешно установлен");
    }
}