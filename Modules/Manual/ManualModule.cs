using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.Extensions.Configuration;
using Modules.Common.MultiSelect;
using Modules.Common.Preconditions.Commands;

namespace Modules.Manual;

[Group("Руководство")]
[Alias("Гайд", "Мануал")]
[Summary("Данный раздел содержит полезные сведения о работе с ботом, его настройке и прочих нюансах")]
public class ManualModule : CommandGuildModuleBase
{
    private readonly IConfiguration _config;

    public ManualModule(InteractiveService interactiveService, IConfiguration configuration) : base(interactiveService)
    {
        _config = configuration;
    }

    [Command("Мафия")]
    [Alias("м")]
    [Summary("Здесь находится вся необходимая информация о настройке мафии и полезных советах")]
    public async Task ShowMafiaManualAsync()
    {
        var mafiaManuals = _config.GetSection("Manuals:Mafia").GetChildren();

        var manuals = mafiaManuals.ToDictionary(m => m.Key, m => m.Value);

        if (manuals.Count == 0)
        {
            await ReplyEmbedAsync("Руководства по мафии отсутствуют", EmbedStyle.Error);

            return;
        }

        string? selectedManual = null;

        IUserMessage? msg = null;

        InteractiveMessageResult<MultiSelectionOption<string>?>? res = null;

        do
        {
            var title = "Выберите руководство";
            var description = "Руководства помогут быстро разобраться в работе с мафией";

            if (selectedManual is not null)
            {
                title = $"Руководство `{selectedManual}`";

                description = manuals[selectedManual];
            }

            var pageBuilder = PageBuilder.FromEmbedBuilder(new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithRandomColor()
                .WithUserFooter(Context.User));

            var options = manuals.Keys
                .Select(k => new MultiSelectionOption<string>(k, 0, k == selectedManual))
                .ToArray();

            var selection = new MultiSelectionBuilder<string>()
                .WithOptions(options)
                .WithCancelButton("Закрыть")
                .WithAllowCancel(true)
                .WithSelectionPage(pageBuilder)
                .WithActionOnCancellation(ActionOnStop.DeleteMessage)
                .WithActionOnTimeout(ActionOnStop.DeleteMessage)
                .AddUser(Context.User)
                .Build();

            res = msg is not null
                ? await Interactive.SendSelectionAsync(selection, msg, TimeSpan.FromMinutes(10))
                : await Interactive.SendSelectionAsync(selection, Context.Channel, TimeSpan.FromMinutes(10));

            msg ??= res.Message;

            if (res.IsSuccess)
                selectedManual = res.Value.Option;
        }
        while (!res.IsCanceled && !res.IsTimeout);
    }


    [Command("Администрирование")]
    [Alias("Админ", "а")]
    [Summary("Руководство для администраторов поможет разобраться с первичной настройкой бота и дальнейшей работой с ним")]
    [RequireStandartAccessLevel(StandartAccessLevel.Administrator, Group = "perm")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public async Task ShowAdminManualList()
    {
        await ReplyAsync("OK");
    }
}