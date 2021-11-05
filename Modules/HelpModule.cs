using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace Modules
{
    [Name("Помощь")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly IConfiguration _config;


        public HelpModule(CommandService commands, IConfiguration config)
        {
            _commandService = commands;
            _config = config;
        }


        [Priority(0)]
        [Command("Помощь")]
        [Alias("help", "команды")]
        [Summary("Получить список доступных команд")]
        [Remarks("Список содержит только те команды, которые доступны вам")]
        public async Task HelpAsync()
        {
            string prefix = "";
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "Список команд"
            };

            foreach (var module in _commandService.Modules)
            {
                string description = "";
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Name}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }


        [Priority(1)]
        [Command("Помощь")]
        [Alias("help", "команда")]
        [Summary("Получить подробности указанной команды")]
        public async Task HelpAsync([Summary("Указанная команда")] string command)
        {
            var result = _commandService.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Команда **{command}** не найдена");
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Другие псевдонимы для команды **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Параметры: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                              $"О команде: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync(embed: builder.Build());
        }


        [Command("Контакты")]
        [Summary("Показать контакты для связи")]
        public async Task ShowContactsAsync()
        {
            await ReplyAsync("Ok");
        }
    }
}
