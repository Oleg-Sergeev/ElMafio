using System.Threading.Tasks;
using Core.Common;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Settings;

[Group("Настройки")]
[Alias("н")]
[RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
[RequireOwner(Group = "perm")]
public class SettingsModule : CommandGuildModuleBase
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


        await ReplyEmbedAsync($"Префикс успешно изменен с `{oldPrefix}` на `{newPrefix}`", EmbedStyle.Successfull);
    }



    [Command("КаналЛог")]
    [Summary("Задать канал логгирования бота")]
    public async Task UpdateLogChannelAsync([Summary("ID канала для логов")] ulong logChannelId)
    {
        var logChannel = Context.Guild.GetTextChannel(logChannelId);

        if (logChannel is null)
        {
            await ReplyEmbedAsync("Канал с указанным ID не найден", EmbedStyle.Error);

            return;
        }

        await UpdateLogChannelAsync(logChannel);
    }

    [Command("КаналЛог")]
    [Summary("Задать канал логгирования бота")]
    public async Task UpdateLogChannelAsync([Summary("Канал для логов")] ITextChannel logChannel)
    {
        var guildSettings = await Context.GetGuildSettingsAsync();

        guildSettings.LogChannelId = logChannel.Id;

        await Context.Db.SaveChangesAsync();


        await ReplyEmbedStampAsync($"Канал для логов [{logChannel.Mention}] успешно установлен", EmbedStyle.Successfull);
    }
}