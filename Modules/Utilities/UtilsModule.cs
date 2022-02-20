using System.Threading.Tasks;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Utilities;

[Name("Утилиты")]
public class UtilitiesModule : GuildModuleBase
{
    public UtilitiesModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    [Command("Пинг")]
    [Summary("Команда для проверки ответа от бота")]
    public Task PingAsync()
        => ReplyEmbedAsync("Понг!");
}
