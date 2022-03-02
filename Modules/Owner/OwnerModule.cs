using System;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord.Interactions;
using Fergun.Interactive;
using Infrastructure.Data.Models.Guild;
using Serilog;

namespace Modules.Owner;

[RequireContext(ContextType.Guild)]
[RequireOwner]
[Group("разработка", "Раздел для разработчиков")]
public class OwnerModule : InteractionGuildModuleBase
{
    public OwnerModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    //[SlashCommand("Лог", "")]
    //public async Task GetFileLogTodayAsync()
    //{
    //    using var filestream = File.OpenRead("");

    //    if (filestream is null)
    //    {
    //        await ReplyEmbedAsync("Файл не найден", EmbedStyle.Error);

    //        return;
    //    }

    //    await ReplyEmbedAsync("Файл успешно отправлен", EmbedStyle.Successfull);

    //    await Context.User.SendFileAsync(filestream, filestream.Name);
    //}

    [SlashCommand("рестарт", "Перезапустить бота")]
    public async Task RestartAsync()
    {
        await RespondEmbedAsync("Перезапуск...", EmbedStyle.Debug);

        Log.Debug("({0:l}): Restart request received from server {1} by user {2}",
                  nameof(RestartAsync),
                  Context.Guild.Name,
                  Context.User.GetFullName());

        var asm = Assembly.GetEntryAssembly();

        if (asm is null)
        {
            await RespondEmbedAsync("Не удалось найти входную сборку", EmbedStyle.Error);

            return;
        }

        System.Diagnostics.Process.Start(asm.Location.Replace("dll", "exe"));

        Environment.Exit(0);
    }


    [SlashCommand("тест", "жесть тест")]
    public Task Test()
    {
        var a = 0;
        var b = 5 / a;

        return Task.CompletedTask;
    }

    [Group("дебаг", "Переключить режим отладки или узнать текущий режим")]
    public class DebugModule : InteractionGuildModuleBase
    {
        public DebugModule(InteractiveService interactive) : base(interactive)
        {
        }



        [SlashCommand("переключить", "Переключить режим отладки")]
        public async Task SwitchDebugMode(DebugMode mode)
        {
            var guildSettings = await Context.Db.GuildSettings.FindAsync(Context.Guild.Id);

            if (guildSettings is null)
            {
                await RespondEmbedAsync("Сервер не найден", EmbedStyle.Error);

                return;
            }

            if (guildSettings.DebugMode == mode)
            {
                await RespondEmbedAsync("Указанный режим уже установлен", EmbedStyle.Warning);

                return;
            }

            var oldMode = guildSettings.DebugMode;

            guildSettings.DebugMode = mode;

            await Context.Db.SaveChangesAsync();


            await RespondEmbedAsync($"Режим отладки успешно изменен: `{oldMode} -> {mode}`", EmbedStyle.Successfull);
        }


        [SlashCommand("текущий", "Переключить режим отладки")]
        public async Task ShowDebugMode()
        {
            var guildSettings = await Context.Db.GuildSettings.FindAsync(Context.Guild.Id);

            if (guildSettings is null)
            {
                await RespondEmbedAsync("Сервер не найден", EmbedStyle.Error);

                return;
            }

            await RespondEmbedAsync($"Текущий режим отладки: `{guildSettings.DebugMode}`");
        }
    }

}
