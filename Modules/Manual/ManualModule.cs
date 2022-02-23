using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Manual;

[Group("Руководство")]
[Alias("Гайд", "Мануал")]
[Summary("Данный раздел содержит полезные сведения о работе с ботом, его настройке и прочих нюансах")]
public class ManualModule : GuildModuleBase
{
    public ManualModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }

    [Command("Мафия")]
    [Alias("м")]
    [Summary("Здесь находится вся необходимая информация о настройке мафии и полезных советах")]
    public async Task ShowMafiaManualAsync()
    {
        var temp = "`бубубубюэбэээбэбэ`";

        await ReplyEmbedAsync(temp, "Руководство по настройкам Мафии");
    }


    [Command("Администрирование")]
    [Alias("Админ", "а")]
    [Summary("Руководство для администраторов поможет разобраться с первичной настройкой бота и дальнейшей работой с ним")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ShowAdminManualList()
    {
        await ReplyAsync("OK");
    }
}