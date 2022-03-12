using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Modules.Common.Preconditions.Commands;

public class RequireConfirmActionAttribute : PreconditionAttribute
{
    private const string ConfirmId = "Confirm";
    private const string RejectId = "Reject";

    private readonly bool _logAction;


    public RequireConfirmActionAttribute(bool logAction = true)
    {
        _logAction = logAction;
    }


    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var interactive = services.GetService<InteractiveService>();

        // If interactive is null, then checking for permissions is not required. Check is called from code
        if (interactive is null)
            return PreconditionResult.FromSuccess();

        var component = GetConfirmComponent();

        var embed = GetConfirmEmbed(context, command);

        var msg = await context.Channel.SendMessageAsync(embed: embed,
            components: component,
            messageReference: new(context.Message.Id, context.Channel.Id, context.Guild.Id));

        var res = await interactive.NextMessageComponentAsync(x => x.Message.Id == msg.Id && x.User.Id == context.User.Id, timeout: TimeSpan.FromSeconds(10));

        await msg.DeleteAsync();

        if (!res.IsSuccess)
            return PreconditionResult.FromError("Действие не подтверждено");

        await res.Value.DeferAsync();

        var data = res.Value.Data;

        if (data.Type != ComponentType.Button)
            return PreconditionResult.FromError("Действие не подтверждено. Неверный тип компонента");

        if (data.CustomId == RejectId)
            return PreconditionResult.FromError("Действие не подтверждено. Действие отклонено");

        if (data.CustomId != ConfirmId)
            return PreconditionResult.FromError("Действие не подтверждено. Неизвестная кнопка");

        if (_logAction)
        {
            var db = services.GetService<BotContext>();

            if (db is not null)
                await LogAsync(context, command, db);
        }

        return PreconditionResult.FromSuccess();
    }


    private static MessageComponent GetConfirmComponent()
        => new ComponentBuilder()
        .WithButton("Подтвердить", ConfirmId, ButtonStyle.Success)
        .WithButton("Отклонить", RejectId, ButtonStyle.Danger)
        .Build();

    private static Embed GetConfirmEmbed(ICommandContext context, CommandInfo command)
        => EmbedHelper.CreateEmbedStamp($"Выполнить команду `{command.Module.GetModulePath()}.{command.Name}`",
            EmbedStyle.Warning,
            "Подтверждение действия",
            context.User, context.Client.CurrentUser);

    private static async Task LogAsync(ICommandContext context, CommandInfo command, BotContext db)
    {
        var logChannelId = await db.Servers
            .AsNoTracking()
            .Where(gs => gs.Id == context.Guild.Id)
            .Select(gs => gs.LogChannelId)
            .FirstOrDefaultAsync();

        if (logChannelId is null)
            return;

        var logChannel = await context.Guild.GetChannelAsync(logChannelId.Value);

        if (logChannel is null || logChannel is not IMessageChannel messageChannel)
            return;

        var channelName = context.Channel is IMentionable m ? m.Mention : $"**#{context.Channel.Name}**";

        var message = $"Пользователь {context.User.GetFullMention()} выполнил команду `{command.Module.GetModulePath()}.{command.Name}` в канале {channelName}";

        await messageChannel.SendEmbedStampAsync(message,
            "Действие выполнено",
            context.User, context.Client.CurrentUser);
    }
}