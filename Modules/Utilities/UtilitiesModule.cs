using System.Threading.Tasks;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Utilities;

[Name("Утилиты")]
public class UtilitiesModule : CommandGuildModuleBase
{
    public UtilitiesModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Пинг")]
    [Summary("Команда для проверки ответа от бота")]
    [Remarks("Ответ также содержит значение задержки между отправкой и получением сообщением (значение пинга)")]
    public Task PingAsync()
        => ReplyEmbedAsync($"Понг! {Context.Client.Latency}");



    [Command("РольЦвет")]
    [Alias("РЦ")]
    [Summary("Вывести информацию о цвете роли")]
    [Priority(-1)]
    public Task ShowRoleColorAsync([Summary("Роль, цвет которой необходимо показать")] IRole role)
        => ReplyAsync(embed: new EmbedBuilder()
            .WithTitle($"Роль {role.Name}")
            .WithDescription($"Цвет в RGB: {role.Color.ToRgbString()}\nЦвет в HEX: {role.Color}")
            .WithColor(role.Color)
            .Build());
}
