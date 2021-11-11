using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net;
using Microsoft.Extensions.Configuration;
using Modules.Comparers;
using Modules.Extensions;

namespace Modules;

[Name("Помощь")]
public class HelpModule : InteractiveBase
{
    private const char EmptySpace = '⠀';

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
    public async Task HelpDMAsync() => await HelpAsync(await Context.User.GetOrCreateDMChannelAsync());

    [Command("Помощьсервер")]
    [Alias("help", "команды")]
    [Summary("Получить список доступных команд")]
    [Remarks("Список содержит только те команды, которые доступны вам")]
    public async Task HelpGuildAsync() => await HelpAsync(Context.Channel);

    private async Task HelpAsync(IMessageChannel channel)
    {
        var builder = new EmbedBuilder()
            .WithThumbnailUrl(Context.Guild.CurrentUser.GetAvatarUrl() ?? Context.Guild.CurrentUser.GetDefaultAvatarUrl())
            .WithTitle("Список доступных команд")
            .WithDescription("Для получения подробностей команды наберите команду **помощь {имя команды}**")
            .WithColor(Color.DarkGreen)
            .WithCurrentTimestamp()
            .WithFooter(new EmbedFooterBuilder()
            {
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                Text = Context.User.GetFullName()
            });

        foreach (var module in _commandService.Modules)
        {
            var commands = module.Commands.Distinct(new CommandInfoComparer());

            var tab = new string(EmptySpace, 4 * GetParentsCount(module));
            var commandList = "";

            foreach (var cmd in commands)
            {
                var result = await cmd.CheckPreconditionsAsync(Context);
                if (result.IsSuccess)
                    commandList += $"{EmptySpace}{tab}{GetParentModulesGroupsPath(module.Group, module)}{cmd.Name}\n";
            }
            if (!string.IsNullOrWhiteSpace(commandList))
            {
                if (!string.IsNullOrEmpty(tab))
                    tab += '➥';
                builder.AddField(tab + module.Name, commandList, false);
            }
        }

        try
        {
            await channel.TriggerTypingAsync();

            await channel.SendMessageAsync(embed: builder.Build());
        }
        catch (HttpException)
        {
            await ReplyAsync($"Не удалось отправить сообщение пользователю {Context.User.GetFullMention()}");
        }






        static int GetParentsCount(ModuleInfo module)
        {
            if (!module.IsSubmodule)
                return 0;

            return 1 + GetParentsCount(module.Parent);
        }

        static string? GetParentModulesGroupsPath(string? subModuleGroup, ModuleInfo module)
        {
            if (string.IsNullOrEmpty(subModuleGroup))
                return null;

            if (!module.IsSubmodule)
                return subModuleGroup + '.';


            subModuleGroup = $"{module.Parent.Group}.{subModuleGroup}";

            var t = GetParentModulesGroupsPath(subModuleGroup, module.Parent);

            if (string.IsNullOrEmpty(module.Group) && t is not null)
                return t.TrimEnd('.');

            return t;
        }

        static string GetParentModulesNames(string subModuleName, ModuleInfo module)
        {
            if (!module.IsSubmodule)
                return subModuleName;

            subModuleName = $"{module.Parent.Name}.{subModuleName}";

            return GetParentModulesNames(subModuleName, module.Parent);
        }
    }



    [Priority(1)]
    [Command("Помощь")]
    [Alias("help", "команда")]
    [Summary("Получить подробности указанной команды")]
    [Remarks("Необходимую команду необходимо указывать вместе со всеми блоками. Например: **помощь Мафия.Настройки.Параметры**" +
        "\n Если команда имеет несколько вариаций, каждая вариация будет показана отдельным сообщением")]
    public async Task HelpAsync([Summary("Указанная команда")] string command)
    {
        var result = _commandService.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Команда **{command}** не найдена");
            return;
        }


        var hasAnyAvailableCommand = false;

        foreach (var match in result.Commands)
        {
            var cmd = match.Command;

            var res = await cmd.CheckPreconditionsAsync(Context);
            if (!res.IsSuccess)
                continue;

            hasAnyAvailableCommand = true;

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Title = $"Команда {command}"
            };

            builder.AddField("Псевдонимы", GetAllAliases(cmd));

            if (cmd.Parameters.Count > 0)
                builder.AddField("Параметры", string.Join("\n", cmd.Parameters.Select(GetParameterInfo)));

            if (!string.IsNullOrEmpty(cmd.Summary))
                builder.AddField("О команде ", cmd.Summary);

            if (!string.IsNullOrEmpty(cmd.Remarks))
                builder.AddField("Примечание ", cmd.Remarks);


            await ReplyAsync(embed: builder.Build());
        }

        if (!hasAnyAvailableCommand)
            await ReplyAsync($"У вас нет прав на использование команды {command}");
    }




    [Command("Контакты")]
    [Summary("Показать контакты для связи")]
    public async Task ShowContactsAsync()
    {
        var contactsSection = _config.GetSection("Contacts");

        var msg = "Если у вас есть вопросы, или другие пожелания, то данный список для вас:\n";

        foreach (var contact in contactsSection.GetChildren())
        {
            msg += $"**{contact.Key}**: {contact.Value}\n";
        }

        await ReplyAsync(msg);
    }



    private string GetParameterInfo(ParameterInfo parameter)
    {
        var info = "**";

        if (!parameter.IsOptional)
            info += $"{{{parameter.Name}}}**";
        else
            info += $"[{parameter.Name}]**";

        if (!string.IsNullOrEmpty(parameter.Summary))
            info += $" – {parameter.Summary}";


        return info;
    }

    private static string GetAllAliases(CommandInfo command)
    {
        var modules = new List<ModuleInfo>();
        var parentModule = command.Module;
        while (parentModule is not null)
        {
            modules.Add(parentModule);

            parentModule = parentModule.Parent;
        }

        var allAliases = modules.Select(m => $"[{string.Join('/', GetOwnAliases(m))}]").ToList();
        if (allAliases.Count == 1 && allAliases[0] == "[]")
            allAliases.RemoveAt(0);

        allAliases.Reverse();

        allAliases.Add($"[{string.Join("/", GetOwnAliases(command))}]");

        var aliases = $"`{string.Join('.', allAliases)}`";

        return aliases;
    }


    private static IEnumerable<string> GetOwnAliases(IEnumerable<string> allAliases)
    {
        var ownAliases = new HashSet<string>();

        foreach (var alias in allAliases)
        {
            var ownAlias = alias.Split('.')[^1];

            ownAliases.Add(ownAlias);
        }

        return ownAliases;
    }
    private static IEnumerable<string> GetOwnAliases(ModuleInfo module) => GetOwnAliases(module.Aliases);
    private static IEnumerable<string> GetOwnAliases(CommandInfo command) => GetOwnAliases(command.Aliases);
}