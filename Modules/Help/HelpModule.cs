using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Comparers;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Fergun.Interactive;
using Microsoft.Extensions.Configuration;
using Modules.Common.MultiSelect;

namespace Modules.Help;

[Name("Помощь")]
public class HelpModule : GuildModuleBase
{
    private const char EmptySpace = '⠀';

    private readonly CommandService _commandService;
    private readonly IConfiguration _config;


    public HelpModule(InteractiveService interactiveService, CommandService commands, IConfiguration config) : base(interactiveService)
    {
        _commandService = commands;
        _config = config;
    }


    [Command("ПомощьСтарое")]
    [Alias("командыСтарое")]
    [Summary("Получить список доступных команд")]
    [Remarks("Список содержит только те команды, которые доступны вам")]
    public async Task HelpLegacyAsync(bool sendToServer = false)
    {
        IMessageChannel channel = !sendToServer ? await Context.User.CreateDMChannelAsync() : Context.Channel;

        await channel.TriggerTypingAsync();


        var builder = new EmbedBuilder()
            .WithThumbnailUrl(Context.Guild.CurrentUser.GetAvatarUrl() ?? Context.Guild.CurrentUser.GetDefaultAvatarUrl())
            .WithTitle("Список доступных команд")
            .WithDescription("Для получения подробностей команды наберите команду **помощь [имена блоков].{имя команды}**")
            .WithInformationMessage()
            .WithCurrentTimestamp()
            .WithUserFooter(Context.User);



        foreach (var module in _commandService.Modules)
        {
            var commands = module.Commands.Distinct(new CommandInfoComparer());

            if (module.IsSubmodule)
                commands = commands.Except(module.Parent.Commands, new CommandInfoComparer(false));

            var tab = new string(EmptySpace, 4 * GetParentsCount(module));
            var commandList = "";

            foreach (var cmd in commands)
            {
                var result = await cmd.CheckPreconditionsAsync(Context);
                if (result.IsSuccess)
                    commandList += $"{EmptySpace}{tab}{cmd.Name}\n";
            }
            if (!string.IsNullOrWhiteSpace(commandList))
            {
                if (!string.IsNullOrEmpty(tab))
                    tab += '➥';

                var modulePath = module.GetModulePath();
                builder.AddField(tab + (!string.IsNullOrEmpty(modulePath) ? modulePath : EmptySpace), commandList, false);
            }
        }

        try
        {
            await channel.SendMessageAsync(embed: builder.Build());
        }
        catch (HttpException)
        {
            await ReplyEmbedAsync($"Не удалось отправить сообщение от пользователя {Context.User.GetFullMention()}", EmbedStyle.Error);
        }


    }


    [Command("Помощь")]
    [Alias("команды")]
    [Summary("Получить список доступных команд")]
    [Remarks("Список содержит только те команды, которые доступны вам")]
    [Priority(-1)]
    public async Task HelpAsync()
    {
        var reset = "[Сбросить]";

        var modulesTree = await GetModuleTreeAsync();

        IUserMessage? message = null;
        InteractiveMessageResult<MultiSelectionOption<object>?>? result = null;

        Dictionary<int, ModuleNode> selectedNodes = new();
        CommandInfo? selectedCommand = null;
        ModuleInfo? selectedModule = null;

        int selectedRow = 0;

        var rootNodes = modulesTree.Select(mt =>
        new MultiSelectionOption<object>(mt, 0, selectedNodes.TryGetValue(0, out var node) && mt.Module == node.Module))
            .Append(new MultiSelectionOption<object>($"{reset}{selectedNodes.GetValueOrDefault(0)?.Module.Name}", 0));

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        var embedBuilder = new EmbedBuilder()
            .WithThumbnailUrl(Context.Guild.CurrentUser.GetAvatarUrlOrDefaultAvatarUrl())
            .WithCurrentTimestamp()
            .WithUserFooter(Context.User)
            .WithColor(new Color(6, 65, 120));

        do
        {
            var commandsVariants = new List<EmbedBuilder>();

            var title = "Помощь";
            var description = "С помощью выпадающих списков выберите интересующий вас блок и его команды";

            var childNodes = new List<MultiSelectionOption<object>>();


            for (int row = 0; row <= selectedRow; row++)
            {
                int n = childNodes.Count;

                childNodes.AddRange(selectedNodes.TryGetValue(row, out var selectedNode)
                ? selectedNode.Children
                .Select(c => new MultiSelectionOption<object>(c, row + 1, selectedNodes.TryGetValue(row + 1, out var node) && node.Module == c.Module))
                : Enumerable.Empty<MultiSelectionOption<object>>());

                if (childNodes.Count > n)
                    childNodes.Add(new MultiSelectionOption<object>($"{reset}{new string(EmptySpace, row + 1)}", row + 1));
            }

            int commandsRow = childNodes.Count > 0 ? selectedRow + 2 : selectedRow + 1;

            var commands = selectedNodes.TryGetValue(selectedRow, out var commandsNode)
                ? commandsNode.Commands
                .DistinctBy(c => c.Name)
                .Select(c => new MultiSelectionOption<object>(c, commandsRow, c == selectedCommand))
                .Append(new MultiSelectionOption<object>($"{reset} {new string(EmptySpace, selectedRow)}", commandsRow))
                : Enumerable.Empty<MultiSelectionOption<object>>();

            var selectionOptions = rootNodes.Concat(childNodes).Concat(commands).ToList();


            embedBuilder.Fields.Clear();

            if (selectedCommand is not null)
            {
                title = $"Команда `{selectedCommand.Name}`";

                description = null;

                var module = selectedModule ?? selectedCommand.Module;

                foreach (var cmd in module.Commands.Where(cmd => cmd.Name == selectedCommand.Name))
                {
                    var res = await cmd.CheckPreconditionsAsync(Context);

                    if (!res.IsSuccess)
                        continue;

                    var commandEmbedBuilder = new EmbedBuilder()
                        .WithTitle($"Вариация команды `{cmd.Name}`")
                        .WithColor(new Color(54, 77, 191))
                        .AddField("Псевдонимы", GetAllAliases(cmd));

                    if (cmd.Parameters.Count > 0)
                        commandEmbedBuilder.AddField("Параметры", string.Join('\n', cmd.Parameters.Select(GetParameterInfo)));

                    if (!string.IsNullOrEmpty(cmd.Summary))
                        commandEmbedBuilder.AddField("О команде", cmd.Summary);

                    if (!string.IsNullOrEmpty(cmd.Remarks))
                        commandEmbedBuilder.AddField("Примечание", cmd.Remarks);

                    commandsVariants.Add(commandEmbedBuilder);
                }

                if (commandsVariants.Count == 0)
                    throw new InvalidOperationException("Executable commands not found");

                embedBuilder.WithFields(commandsVariants[0].Fields);
            }
            else if (selectedModule is not null)
            {
                title = $"Блок {selectedModule.Name}";

                description = null;

                var aliases = GetAllAliases(selectedModule);

                if (aliases is not null)
                    embedBuilder.AddField("Псевдонимы", aliases);

                if (!string.IsNullOrEmpty(selectedModule.Summary))
                    embedBuilder.AddField("О модуле", selectedModule.Summary);

                if (!string.IsNullOrEmpty(selectedModule.Remarks))
                    embedBuilder.AddField("Примечание", selectedModule.Remarks);
            }

            embedBuilder.WithTitle(title);

            if (description is not null)
                embedBuilder.WithDescription(description);

            if (commandsVariants.Count > 0)
                commandsVariants[0] = embedBuilder;
            else
                commandsVariants.Add(embedBuilder);

            var multiPageBuilder = new MultiEmbedPageBuilder()
                .WithBuilders(commandsVariants);

            var placeholders = Enumerable.Repeat("Выберите модуль", commandsRow).Append("Выберите команду");

            var multiSelection = new MultiSelectionBuilder<object>()
                .AddUser(Context.User)
                .WithPlaceholders(placeholders)
                .WithOptions(selectionOptions.ToArray())
                .WithAllowCancel(true)
                .WithCancelButton("Закрыть")
                .WithStringConverter(StringConverter)
                .WithSelectionPage(multiPageBuilder)
                .WithActionOnCancellation(ActionOnStop.DeleteMessage)
                .WithActionOnTimeout(ActionOnStop.DeleteMessage)
                .Build();


            result = message is null
            ? await Interactive.SendSelectionAsync(multiSelection, Context.Channel, TimeSpan.FromMinutes(2), null)
            : await Interactive.SendSelectionAsync(multiSelection, message, TimeSpan.FromMinutes(2), null);

            message = result.Message;

            if (!result.IsSuccess)
            {
                if (result.IsCanceled || result.IsTimeout)
                    break;

                continue;
            }

            var value = result.Value;

            if (value.Row == commandsRow)
            {
                if (!value.Option.ToString()?.Contains(reset) ?? false)
                    selectedCommand = (CommandInfo)value.Option;
                else
                    selectedCommand = null;
            }
            else
            {
                selectedCommand = null;

                if (!value.Option.ToString()?.Contains(reset) ?? false)
                {
                    var node = (ModuleNode)value.Option;

                    selectedNodes[value.Row] = node;

                    selectedModule = node.Module;

                    selectedRow = value.Row;
                }
                else
                {
                    selectedNodes.Remove(value.Row);

                    if (value.Row > 0)
                    {
                        selectedRow = value.Row - 1;

                        selectedModule = selectedNodes[selectedRow].Module;
                    }
                    else
                    {
                        selectedRow = 0;

                        selectedModule = null;
                    }
                }
            }
        }
        while (!cts.IsCancellationRequested);

        try
        {
            await message.DeleteAsync();
        }
        catch (HttpException)
        { }



        static string StringConverter(MultiSelectionOption<object> select)
        {
            if (select.Option is ModuleNode n)
                return !string.IsNullOrEmpty(n.Module.Aliases[0]) ? n.Module.Aliases[0] : n.Module.Name;

            if (select.Option is CommandInfo c)
                return c.Aliases[0];

            return select.Option.ToString() ?? "Без имени";
        }
    }

    [Command("Помощь")]
    [Alias("команда")]
    [Summary("Получить подробности указанной команды")]
    [Remarks("Необходимую команду необходимо указывать вместе со всеми блоками. Например: `помощь Мафия.Настройки.Параметры`" +
        "\n Если команда имеет несколько вариаций, каждая вариация будет показана отдельным сообщением")]
    public async Task HelpAsync([Summary("Указанная команда")] string command)
    {
        var result = _commandService.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyEmbedAsync($"Команда `{command}` не найдена", EmbedStyle.Error);
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
                .WithInformationMessage(false)
                .WithTitle($"Команда {command}");

            builder.AddField("Псевдонимы", GetAllAliases(cmd));

            if (cmd.Parameters.Count > 0)
                builder.AddField("Параметры", string.Join("\n", cmd.Parameters.Select(GetParameterInfo)));

            if (!string.IsNullOrEmpty(cmd.Summary))
                builder.AddField("О команде", cmd.Summary);

            if (!string.IsNullOrEmpty(cmd.Remarks))
                builder.AddField("Примечание", cmd.Remarks);


            await ReplyAsync(embed: builder.Build());
        }

        if (!hasAnyAvailableCommand)
            await ReplyEmbedAsync($"У вас нет прав на использование команды `{command}`", EmbedStyle.Error);
    }


    [Command("Помощьблок")]
    [Summary("Получить подробности указанного модуля")]
    [Remarks("Необходимый модуль необходимо указывать вместе со всеми родительскими блоками. Например: `помощьблок Мафия.Настройки`")]
    public async Task HelpModuleAsync(string moduleName)
    {
        moduleName = moduleName.ToLower();

        var module = _commandService.Modules
            .FirstOrDefault(m => m.Aliases.Select(a => a.ToLower()).Contains(moduleName))
            ?? _commandService.Modules
            .FirstOrDefault(m => m.Name.ToLower() == moduleName);

        if (module is null)
        {
            await ReplyEmbedAsync($"Блок **{moduleName}** не найден", EmbedStyle.Error);

            return;
        }

        var builder = new EmbedBuilder()
            .WithThumbnailUrl(Context.Guild.CurrentUser.GetAvatarUrl() ?? Context.Guild.CurrentUser.GetDefaultAvatarUrl())
            .WithTitle($"Блок {moduleName}")
            .WithDescription("Для получения подробностей команды наберите команду **помощь [имена блоков].{имя команды}**")
            .WithInformationMessage()
            .WithCurrentTimestamp()
            .WithUserFooter(Context.User);

        if (!string.IsNullOrEmpty(module.Summary))
        {
            builder.AddField("О блоке", module.Summary);

            if (!string.IsNullOrEmpty(module.Remarks))
                builder.AddField("Примечание", module.Remarks);
        }


        var commands = module.Commands.Distinct(new CommandInfoComparer());

        if (module.IsSubmodule)
            commands = commands.Except(module.Parent.Commands, new CommandInfoComparer(false));

        var commandList = "";

        foreach (var cmd in commands)
        {
            var result = await cmd.CheckPreconditionsAsync(Context);
            if (result.IsSuccess)
            {
                var parentModulesPath = GetParentModulesGroupsPath(module.Group, module) + ".";
                commandList += $"{parentModulesPath.TrimStart('.')}{cmd.Name}\n";
            }
        }
        if (!string.IsNullOrWhiteSpace(commandList))
            builder.AddField("Список команд", commandList, false);


        await ReplyAsync(embed: builder.Build());
    }



    [Command("Контакты")]
    [Summary("Показать контакты для связи")]
    public async Task ShowContactsAsync()
    {
        var contactsSection = _config.GetSection("Contacts");

        var msg = "Если у вас есть вопросы, или другие пожелания, то данный список для вас:\n";

        foreach (var contact in contactsSection.GetChildren())
            msg += $"**{contact.Key}**: {contact.Value}\n";

        await ReplyEmbedAsync(msg);
    }



    private string GetParameterInfo(Discord.Commands.ParameterInfo parameter)
    {
        var info = "`";

        if (!parameter.IsOptional)
            info += $"{{{parameter.Name}}}`";
        else
            info += $"[{parameter.Name}]`";

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

        var moduleAliases = GetAllAliases(command.Module);

        allAliases.Add($"[{string.Join("/", GetOwnAliases(command))}]");

        var aliases = $"`{string.Join('.', allAliases.DistinctBy(a => a.ToLower()))}`";

        return aliases;
    }

    private static string? GetAllAliases(ModuleInfo module)
    {
        var modules = new List<ModuleInfo>();
        var parentModule = module;
        while (parentModule is not null)
        {
            modules.Add(parentModule);

            parentModule = parentModule.Parent;
        }

        var allAliases = modules.Select(m => $"[{string.Join('/', GetOwnAliases(m))}]").ToList();
        if (allAliases.Count == 1 && allAliases[0] == "[]")
            allAliases.RemoveAt(0);

        allAliases.Reverse();

        var aliasesStr = string.Join('.', allAliases);

        if (string.IsNullOrEmpty(aliasesStr))
            return null;

        return $"`{aliasesStr}`";
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


    private static int GetParentsCount(ModuleInfo module)
    {
        if (!module.IsSubmodule)
            return 0;

        if (string.IsNullOrEmpty(module.Group))
            return GetParentsCount(module.Parent);

        return 1 + GetParentsCount(module.Parent);
    }

    private static string? GetParentModulesGroupsPath(string? subModuleGroup, ModuleInfo module)
    {
        if (string.IsNullOrEmpty(subModuleGroup))
            return null;

        if (!module.IsSubmodule || string.IsNullOrEmpty(module.Parent.Group))
            return subModuleGroup;


        subModuleGroup = $"{module.Parent.Group}.{subModuleGroup}";

        var t = GetParentModulesGroupsPath(subModuleGroup, module.Parent);

        if (string.IsNullOrEmpty(module.Group) && t is not null)
            return t.TrimEnd('.');

        return t;
    }

    private async Task<List<ModuleNode>> GetModuleTreeAsync()
    {
        var modules = _commandService.Modules.ToList();

        var rootModules = modules.Where(m => !m.IsSubmodule && m.Commands.Any(cmd => cmd.CheckPreconditionsAsync(Context).Result.IsSuccess));

        var moduleNodeGroups = new List<ModuleNode>(rootModules.Select(rm => new ModuleNode(rm)));

        foreach (var node in moduleNodeGroups)
            await AddChildsAsync(node);

        return moduleNodeGroups;


        async Task AddChildsAsync(ModuleNode parentNode)
        {
            var children = parentNode.Module.Submodules
                .Select(c => new ModuleNode(c))
                .ToList();

            var nestedModules = children.Where(c => string.IsNullOrEmpty(c.Module.Group));

            parentNode.Commands.AddRange(nestedModules.SelectMany(nm => nm.Commands));

            for (int i = parentNode.Commands.Count - 1; i >= 0; i--)
            {
                var res = await parentNode.Commands[i].CheckPreconditionsAsync(Context);

                if (!res.IsSuccess)
                    parentNode.Commands.RemoveAt(i);
            }

            if (parentNode.Commands.Count == 0)
                return;

            parentNode.Children.AddRange(children.Except(nestedModules));

            foreach (var child in children)
                await AddChildsAsync(child);
        }
    }


    private class ModuleNode
    {
        public ModuleInfo Module { get; }

        public List<ModuleNode> Children { get; }

        public List<CommandInfo> Commands { get; }

        public ModuleNode(ModuleInfo root)
        {
            Module = root;

            Children = new();

            Commands = new(root.Commands);
        }
    }
}