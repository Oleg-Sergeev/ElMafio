﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Common;
using Core.Comparers;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Fergun.Interactive;
using Microsoft.Extensions.Configuration;

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


    [Command("Помощь")]
    [Alias("команды")]
    [Summary("Получить список доступных команд")]
    [Remarks("Список содержит только те команды, которые доступны вам")]
    public async Task HelpAsync(bool sendToServer = false)
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

    [Priority(-1)]
    [Command("Помощь")]
    [Alias("команда")]
    [Summary("Получить подробности указанной команды")]
    [Remarks("Необходимую команду необходимо указывать вместе со всеми блоками. Например: **помощь Мафия.Настройки.Параметры**" +
        "\n Если команда имеет несколько вариаций, каждая вариация будет показана отдельным сообщением")]
    public async Task HelpAsync([Summary("Указанная команда")] string command)
    {
        var result = _commandService.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyEmbedAsync($"Команда **{command}** не найдена", EmbedStyle.Error);
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
                builder.AddField("О команде ", cmd.Summary);

            if (!string.IsNullOrEmpty(cmd.Remarks))
                builder.AddField("Примечание ", cmd.Remarks);


            await ReplyAsync(embed: builder.Build());
        }

        if (!hasAnyAvailableCommand)
            await ReplyEmbedAsync($"У вас нет прав на использование команды {command}", EmbedStyle.Error);
    }


    [Discord.Interactions.SlashCommand("", "")]
    [Command("Помощьблок")]
    [Summary("Получить подробности указанного модуля")]
    [Remarks("Необходимый модуль необходимо указывать вместе со всеми родительскими блоками. Например: **помощьблок Мафия.Настройки**")]
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
}