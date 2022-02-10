using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Data;
using Core.Extensions;
using Core.TypeReaders;
using Core.ViewModels;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Common.MultiSelect;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.Services;
using Modules.Games.Services;
using Serilog;

namespace Modules.Games.Mafia;

[Group("Мафия")]
[Alias("м")]
public class MafiaModule : GameModule<MafiaData>
{
    private readonly IMafiaSetupService _mafiaService;
    private readonly IGameSettingsService<MafiaSettings> _settingsService;

    public MafiaModule(InteractiveService interactiveService, IMafiaSetupService mafiaService, IGameSettingsService<MafiaSettings> settingsService) : base(interactiveService)
    {
        _mafiaService = mafiaService;
        _settingsService = settingsService;
    }


    protected override MafiaData CreateGameData(IGuildUser host)
        => new("Мафия", 3, host, new());


    public override async Task StartAsync()
    {
        var check = await CheckPreconditionsAsync();
        if (!check.IsSuccess)
        {
            await ReplyEmbedAsync(check.ErrorReason, EmbedStyle.Error);

            return;
        }

        var data = GetGameData();
        data.Players.Shuffle(3);

        await ReplyEmbedStampAsync($"{data.Name} успешно запущена", EmbedStyle.Successfull);


        try
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            var context = await CreateMafiaContextAsync(settings, data);

            var tasks = new List<Task>
        {
            _mafiaService.SetupGuildAsync(context),
            _mafiaService.SetupUsersAsync(context)
        };

            await Task.WhenAll(tasks);

            _mafiaService.SetupRoles(context);

            if (settings.CurrentTemplate.ServerSubSettings.SendWelcomeMessage)
            {
                await _mafiaService.SendRolesInfoAsync(context);

                await Task.Delay(5000);
            }


            var game = new MafiaGame(context);

            var winner = await game.RunAsync();


            if (winner.Role is not null)
                await ReplyEmbedAsync($"Победителем оказался: {winner.Role.Name}!");
            else
                await ReplyEmbedAsync("Город опустел... Никто не победил");

            var str = "Голоса игроков:\n";
            foreach (var role in context.RolesData.AllRoles.Values)
            {
                str += $"\n{role.Player.Mention}\n";
                for (int i = 0; i < role.Votes.Count; i++)
                    str += $"{i + 1}: {role.Votes[i].Option?.GetFullName() ?? "None"} [{role.Votes[i].IsSkip}]\n";
            }

            await ReplyAsync(str);

            await ReplyEmbedStampAsync($"{data.Name} успешно завершена", EmbedStyle.Successfull);
        }
        finally
        {
            DeleteGameData();
        }
    }


    public override async Task StopAsync()
    {
        if (TryGetGameData(out var data))
            data.TokenSource.Cancel();

        await base.StopAsync();
    }


    protected override async Task<PreconditionResult> CheckPreconditionsAsync()
    {
        var check = await base.CheckPreconditionsAsync();

        if (!check.IsSuccess)
            return check;


        var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

        ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

        if (!settings.CurrentTemplate.GameSubSettings.IsCustomGame)
            return PreconditionResult.FromSuccess();


        var data = GetGameData();

        var rolesSettings = settings.CurrentTemplate.RoleAmountSubSettings;
        var gameSettings = settings.CurrentTemplate.GameSubSettings;


        if (data.Players.Count < rolesSettings.MinimumPlayersCount)
            return PreconditionResult.FromError($"Недостаточно игроков." +
                $"Минимальное количество игроков согласно пользовательским настройкам игры: {rolesSettings.MinimumPlayersCount}");


        if (!gameSettings.IsFillWithMurders && rolesSettings.BlackRolesCount == 0)
            return PreconditionResult.FromError("Для игры необходимо наличие хотя бы одной черной роли. " +
                    "Измените настройки ролей, добавив черную роль, или назначьте автозаполнение ролями мафии");

        if (rolesSettings.RedRolesCount + rolesSettings.NeutralRolesCount == data.Players.Count)
            return PreconditionResult.FromError("Невозможно добавить черную роль на стол: не хватает места." +
                "Уберите мирную или нейтральную роль, или добавьте еще хотя бы одного игрока");


        if (gameSettings.IsFillWithMurders && rolesSettings.RedRolesCount == 0)
            return PreconditionResult.FromError("Для игры необходимо наличие хотя бы одной красной роли. " +
                    "Измените настройки ролей, добавив красную роль, или назначьте автозаполнение ролями мирных жителей");

        if (rolesSettings.BlackRolesCount + rolesSettings.NeutralRolesCount == data.Players.Count)
            return PreconditionResult.FromError("Невозможно добавить красную роль на стол: не хватает места." +
                "Уберите черную или нейтральную роль, или добавьте еще хотя бы одного игрока");

        return PreconditionResult.FromSuccess();
    }



    private async Task<MafiaContext> CreateMafiaContextAsync(MafiaSettings settings, MafiaData data)
    {
        settings.CategoryChannelId ??= (await Context.Guild.CreateCategoryChannelAsync("Мафия")).Id;


        var _guildData = new MafiaGuildData(
               await Context.Guild.GetTextChannelOrCreateAsync(settings.GeneralTextChannelId, "мафия-общий", SetCategoryChannel),
               await Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-убийцы", SetCategoryChannel),
               Context.Guild.GetTextChannel(settings.SpectatorsTextChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.GeneralVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.MurdersVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.SpectatorsVoiceChannelId ?? 0),
               await Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
               Context.Guild.GetRole(settings.WatcherRoleId ?? 0));


        if (settings.ClearChannelsOnStart)
        {
            await _guildData.GeneralTextChannel.ClearAsync();

            await _guildData.MurderTextChannel.ClearAsync();

            if (_guildData.SpectatorTextChannel is not null)
                await _guildData.SpectatorTextChannel.ClearAsync();
        }

        var context = new MafiaContext(_guildData, data, settings, Context, Interactive);


        return context;


        void SetCategoryChannel(GuildChannelProperties props)
        {
            props.CategoryId = settings.CategoryChannelId;

            var overwrites = new List<Overwrite>
                {
                    new(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny))
                };

            props.PermissionOverwrites = overwrites;
        }
    }



    [Group("Шаблоны")]
    [Alias("ш")]
    [RequireOwner(Group = "perm")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    public class TemplatesModule : GuildModuleBase
    {
        private readonly IGameSettingsService<MafiaSettings> _settingsService;

        public TemplatesModule(InteractiveService interactiveService, IGameSettingsService<MafiaSettings> settingsService) : base(interactiveService)
        {
            _settingsService = settingsService;
        }



        [Command("Клонировать")]
        [Alias("клон", "к")]
        public async Task CloneTemplate([Remainder] string newTemplateName)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            var originalTemplateName = settings.CurrentTemplate.Name;

            if (newTemplateName == originalTemplateName)
            {
                await ReplyEmbedAsync($"Укажите имя, отличное от имени активного шаблона", EmbedStyle.Error);

                return;
            }

            var templateNames = await Context.Db.MafiaSettingsTemplates
                .AsNoTracking()
                .Where(t => t.MafiaSettingsId == settings.Id)
                .Select(t => t.Name)
                .ToListAsync();

            if (templateNames.Contains(newTemplateName))
            {
                await ReplyEmbedAsync("Имя шаблона уже используется", EmbedStyle.Error);

                return;
            }

            var newTemplate = new MafiaSettingsTemplate()
            {
                Name = newTemplateName,
                MafiaSettingsId = settings.Id,
                ServerSubSettings = settings.CurrentTemplate.ServerSubSettings,
                GameSubSettings = settings.CurrentTemplate.GameSubSettings,
                RoleAmountSubSettings = settings.CurrentTemplate.RoleAmountSubSettings,
                RolesExtraInfoSubSettings = settings.CurrentTemplate.RolesExtraInfoSubSettings,
                PreGameMessage = settings.CurrentTemplate.PreGameMessage
            };


            await Context.Db.MafiaSettingsTemplates.AddAsync(newTemplate);

            await Context.Db.SaveChangesAsync();


            settings.CurrentTemplateId = newTemplate.Id;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Шаблон **{newTemplateName}** успешно клонирован из шаблона **{originalTemplateName}**", EmbedStyle.Successfull);

            await ReplyEmbedAsync($"Текущий шаблон: {newTemplate.Name}");
        }


        [Command("Загрузить")]
        [Alias("згр", "з")]
        public async Task LoadTemplate([Remainder] string name)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            var template = await Context.Db.MafiaSettingsTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync("Шаблон не найден", EmbedStyle.Error);

                return;
            }

            settings.CurrentTemplateId = template.Id;

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync($"Шаблон **{name}** успешно загружен", EmbedStyle.Successfull);
        }


        [Command("Текущий")]
        [Alias("тек", "т")]
        public async Task ShowCurrentTemplate()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            await ReplyEmbedAsync($"Текущий шаблон - **{settings.CurrentTemplate.Name}**");
        }


        [Command("Список")]
        [Alias("сп")]
        public async Task ShowAllTemplates()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

            var templates = await Context.Db.MafiaSettingsTemplates
                .AsNoTracking()
                .Where(t => t.MafiaSettingsId == settings.Id)
                .ToListAsync();

            var str = "";

            foreach (var template in templates)
                str += settings.CurrentTemplateId == template.Id 
                    ? $"**{template.Name}**\n"
                    : $"{template.Name}\n";

            await ReplyEmbedAsync(str, "Список шаблонов");
        }


        [Command("Имя")]
        public async Task UpdateTemplateName([Remainder] string newName)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            var templateNames = await Context.Db.MafiaSettingsTemplates
                .AsNoTracking()
                .Where(t => t.MafiaSettingsId == settings.Id)
                .Select(t => t.Name)
                .ToListAsync();

            if (templateNames.Contains(newName))
            {
                await ReplyEmbedAsync("Имя шаблона уже используется", EmbedStyle.Error);

                return;
            }


            var oldName = settings.CurrentTemplate.Name;

            settings.CurrentTemplate.Name = newName;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Имя шаблона успешно изменено: **{oldName}** -> **{newName}**", EmbedStyle.Successfull);
        }


        [Command("Сброс")]
        public async Task ResetTemplate()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            settings.CurrentTemplate.GameSubSettings = new();
            settings.CurrentTemplate.ServerSubSettings = new();
            settings.CurrentTemplate.RolesExtraInfoSubSettings = new();
            settings.CurrentTemplate.RoleAmountSubSettings = new();
            settings.CurrentTemplate.PreGameMessage = null;

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync($"Найстройки шаблона успешно сброшены", EmbedStyle.Successfull);
        }


        [Command("Удалить")]
        public async Task DeleteTemplate([Remainder] string name)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);


            if (name == settings.CurrentTemplate.Name)
            {
                await ReplyEmbedAsync("Невозможно удалить активный шаблон", EmbedStyle.Error);

                return;
            }


            var template = await Context.Db.MafiaSettingsTemplates
                .FirstOrDefaultAsync(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync("Шаблон не найден", EmbedStyle.Error);

                return;
            }


            Context.Db.MafiaSettingsTemplates.Remove(template);

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Шаблон **{name}** успешно удален", EmbedStyle.Successfull);
        }
    }



    [Group("Настройки")]
    [Alias("н")]
    [RequireOwner(Group = "perm")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [Summary("Настройки для мафии включают в себя настройки сервера(используемые роли, каналы и категорию каналов) и настройки самой игры. " +
        "Для подробностей введите команду **Мафия.Настройки.Помощь**")]
    public class SettingsModule : GuildModuleBase
    {
        private readonly IGameSettingsService<MafiaSettings> _settingsService;

        public SettingsModule(InteractiveService interactiveService, IGameSettingsService<MafiaSettings> settingsService) : base(interactiveService)
        {
            _settingsService = settingsService;
        }


        [Command]
        [Priority(-2)]
        public Task SetSettingsAsync()
            => ShowSettingsAsync(true);


        [Command("Текущие")]
        [Alias("тек", "т")]
        public Task ShowAllSettingsAsync()
            => ShowSettingsAsync(false);




        [Command("Автонастройка")]
        [Alias("ан")]
        public async Task AutoSetGeneralSettingsAsync()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);


            var categoryChannel = await Context.Guild.GetCategoryChannelOrCreateAsync(settings.CategoryChannelId, "Мафия", SetCategoryChannel);

            settings.CategoryChannelId = categoryChannel.Id;


            var generalTextChannelTask = Context.Guild.GetTextChannelOrCreateAsync(settings.GeneralTextChannelId, "мафия-общий", SetCategoryChannel);
            var murderTextChannelTask = Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-убийцы", SetCategoryChannel);
            var spectatorsTextChannelTask = Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-наблюдатели", SetCategoryChannel);
            var generalVoiceChannelTask = Context.Guild.GetVoiceChannelOrCreateAsync(settings.GeneralVoiceChannelId, "мафия-общий", SetCategoryChannel);
            var murdersVoiceChannelTask = Context.Guild.GetVoiceChannelOrCreateAsync(settings.MurdersVoiceChannelId, "мафия-убийцы", SetCategoryChannel);

            var spectatorsVoiceChannelTask = Context.Guild.GetVoiceChannelOrCreateAsync(settings.SpectatorsVoiceChannelId, "мафия-наблюдатели", SetCategoryChannel);
            var mafiaRoleTask = Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true);
            var spectatorRoleTask = Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Наблюдатель мафии", null, Color.DarkBlue, true, true);


            var settigsVM = new MafiaSettingsViewModel()
            {
                GeneralTextChannelId = (await generalTextChannelTask).Id,
                MurdersTextChannelId = (await murderTextChannelTask).Id,
                SpectatorsTextChannelId = (await spectatorsTextChannelTask).Id,
                GeneralVoiceChannelId = (await generalVoiceChannelTask).Id,
                MurdersVoiceChannelId = (await murdersVoiceChannelTask).Id,
                SpectatorsVoiceChannelId = (await spectatorsVoiceChannelTask).Id,
                MafiaRoleId = (await mafiaRoleTask).Id,
                WatcherRoleId = (await spectatorRoleTask).Id
            };


            SetParameters(settings, settigsVM);

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedStampAsync("Автонастройка успешно завершена", EmbedStyle.Successfull);


            void SetCategoryChannel(GuildChannelProperties props)
            {
                props.CategoryId = settings.CategoryChannelId;

                var overwrites = new List<Overwrite>
                {
                    new(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny))
                };

                props.PermissionOverwrites = overwrites;
            }
        }


        [Command("Сброс")]
        public async Task ResetGeneralSettingsAsync()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);


            var tasksNullable = new List<Task?>
            {
                Context.Guild.GetCategoryChannel(settings.CategoryChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetTextChannel(settings.GeneralTextChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetTextChannel(settings.MurdersTextChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetTextChannel(settings.SpectatorsTextChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetVoiceChannel(settings.GeneralVoiceChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetVoiceChannel(settings.MurdersVoiceChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetVoiceChannel(settings.SpectatorsVoiceChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetRole(settings.MafiaRoleId ?? 0)?.DeleteAsync(),
                Context.Guild.GetRole(settings.WatcherRoleId ?? 0)?.DeleteAsync()
            };

            List<Task> tasks = tasksNullable.Where(t => t is not null).ToList()!;

            await Task.WhenAll(tasks);


            settings.CategoryChannelId = null;
            settings.GeneralTextChannelId = null;
            settings.MurdersTextChannelId = null;
            settings.SpectatorsTextChannelId = null;
            settings.GeneralVoiceChannelId = null;
            settings.MurdersVoiceChannelId = null;
            settings.SpectatorsVoiceChannelId = null;
            settings.MafiaRoleId = null;
            settings.WatcherRoleId = null;


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedStampAsync("Общие настройки успешно сброшены", EmbedStyle.Successfull);
        }


        [Command("Проверка")]
        [Alias("чек")]
        public async Task CheckSettingsAsync()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);


            var message = "";

            message += Handle(settings.CategoryChannelId, nameof(settings.CategoryChannelId), Context.Guild.GetCategoryChannel) + "\n";
            message += Handle(settings.MafiaRoleId, nameof(settings.MafiaRoleId), Context.Guild.GetRole) + "\n";
            message += Handle(settings.WatcherRoleId, nameof(settings.WatcherRoleId), Context.Guild.GetRole) + "\n";
            message += Handle(settings.GeneralTextChannelId, nameof(settings.GeneralTextChannelId), Context.Guild.GetTextChannel) + "\n";
            message += Handle(settings.MurdersTextChannelId, nameof(settings.MurdersTextChannelId), Context.Guild.GetTextChannel) + "\n";
            message += Handle(settings.SpectatorsTextChannelId, nameof(settings.SpectatorsTextChannelId), Context.Guild.GetTextChannel) + "\n";
            message += Handle(settings.GeneralVoiceChannelId, nameof(settings.GeneralVoiceChannelId), Context.Guild.GetVoiceChannel) + "\n";
            message += Handle(settings.MurdersVoiceChannelId, nameof(settings.MurdersVoiceChannelId), Context.Guild.GetVoiceChannel) + "\n";
            message += Handle(settings.SpectatorsVoiceChannelId, nameof(settings.SpectatorsVoiceChannelId), Context.Guild.GetVoiceChannel);


            await ReplyEmbedStampAsync(message, "Проверка настроек");


            static string Handle<T>(ulong? id, string propName, Func<ulong, T?> action)
            {
                var str = $"**{propName}** [{id?.ToString() ?? "Null"}]";

                if (id is null)
                    return $"{str} - ()";

                var result = action(id ?? 0);

                return result is not null
                    ? $"{str} - {ConfirmEmote.Name}"
                    : $"{str} - {DenyEmote.Name}";
            }
        }


        private async Task ShowSettingsAsync(bool withSetSettings)
        {
            const string CloseOption = "Закрыть";
            //const string CancelOption = "Отменить";

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            string? selectedBlock = null;
            string? selectedSetting = null;

            (string? displayName, string? previousValue, string? currentValue, bool isModified) = (null, null, null, false);

            var isClosed = false;
            var wasSettingsModified = false;

            IUserMessage? message = null;
            InteractiveMessageResult<MultiSelectionOption<string>?>? result = null;

            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, withSetSettings);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);


            var current = settings.CurrentTemplate;

            var settingsBlocks = new Dictionary<string, Dictionary<string, SettingsDisplay>>()
            {
                {"Общее", CreateSettingsDisplays(settings, new MafiaSettingsViewModel())},
                {"Игра", CreateSettingsDisplays(current.GameSubSettings, new GameSubSettingsViewModel())},
                {"Сервер", CreateSettingsDisplays(current.ServerSubSettings, new ServerSubSettingsViewModel())},
                {"Состав ролей", CreateSettingsDisplays(current.RoleAmountSubSettings, new RoleAmountSubSettingsViewModel())},
                {"Доп. настройки ролей", CreateSettingsDisplays(current.RolesExtraInfoSubSettings, new RolesInfoSubSettingsViewModel())}
            };


            do
            {
                var title = "Настройки";
                var description = "Выберите интересующий вас блок настроек";

                var options = settingsBlocks.Keys
                    .Select(k => new MultiSelectionOption<string>(k, 0, selectedBlock == k));

                var embedBuilder = new EmbedBuilder();

                if (withSetSettings)
                    embedBuilder.WithColor(new Color(49, 148, 146));
                else
                    embedBuilder.WithColor(new Color(116, 45, 173));


                if (result is not null && selectedBlock is not null)
                {
                    title = $"Блок {selectedBlock}";

                    if (withSetSettings)
                    {
                        var settingsBlockValues = settingsBlocks[selectedBlock].Keys
                            .Select(n => new MultiSelectionOption<string>(n, 1, description: $"Добавить краткое описание параметра, 50 символов"));

                        options = options.Concat(settingsBlockValues);

                        description = isModified
                        ? $"Значение **{displayName}** успешно изменено: {previousValue ?? "[Н/д]"} -> {currentValue ?? "[Н/д]"}"
                        : "Выберите интересующий вас параметр";
                    }
                    else
                    {
                        description = null;
                    }


                    var displayNames = settingsBlocks[selectedBlock].Values.Select(x => x.DisplayName);

                    var values = settingsBlocks[selectedBlock].Values.Select(x => x.ModelValue);

                    var fields = new List<EmbedFieldBuilder>()
                    {
                        new()
                        {
                            Name = "Параметр",
                            Value = string.Join('\n', displayNames),
                            IsInline = true
                        },
                        new()
                        {
                            Name = "Значение",
                            Value = string.Join('\n', values.Select(v =>
                            {
                                if (ulong.TryParse(v?.ToString(), out var id))
                                    return Context.Guild.GetMentionFromId(id);

                                return v?.ToString() ?? "[Н/д]";
                            })),
                            IsInline = true
                        }
                    };

                    embedBuilder.WithFields(fields);
                }


                embedBuilder.WithTitle(title);

                if (description is not null)
                    embedBuilder.WithDescription(description);


                var pageBuilder = PageBuilder.FromEmbedBuilder(embedBuilder);


                var multiSelection = new MultiSelectionBuilder<string>()
                    .AddUser(Context.User)
                    .WithOptions(options.ToArray())
                    .WithCancelButton(CloseOption)
                    .WithStringConverter(s =>
                    {
                        if (s.Option != CloseOption && !settingsBlocks.TryGetValue(s.Option, out _) && selectedBlock is not null)
                            return settingsBlocks[selectedBlock][s.Option].DisplayName;

                        return s.ToString() ?? "[Н/д]";
                    })
                    .WithSelectionPage(pageBuilder)
                    .WithActionOnCancellation(ActionOnStop.DeleteMessage)
                    .WithActionOnTimeout(ActionOnStop.DeleteMessage)
                    .Build();


                result = message is null
                ? await Interactive.SendSelectionAsync(multiSelection, Context.Channel, TimeSpan.FromMinutes(2), null, cts.Token)
                : await Interactive.SendSelectionAsync(multiSelection, message, TimeSpan.FromMinutes(2), null, cts.Token);

                message = result.Message;

                if (result.IsSuccess)
                {
                    if (result.Value.Option == CloseOption)
                    {
                        isClosed = true;

                        await message.DeleteAsync();

                        break;
                    }

                    switch (result.Value.Row)
                    {
                        case 0:
                            selectedBlock = result.Value.Option;
                            break;

                        case 1:
                            selectedSetting = result.Value.Option;

                            if (selectedBlock is null)
                                throw new InvalidOperationException("selectedBlock cannot be null when a row 1 is selected");

                            var settingsDisplay = settingsBlocks[selectedBlock][selectedSetting];

                            var prop = settingsDisplay.ViewModel.GetType().GetProperty(selectedSetting)
                                ?? throw new InvalidOperationException($"Property \"{selectedSetting}\" was not found");

                            previousValue = settingsDisplay.ModelValue?.ToString();

                            var success = await TrySetParameterAsync(prop, settingsDisplay.ViewModel);

                            if (success)
                            {
                                SetParameters(settingsDisplay.Model, settingsDisplay.ViewModel);

                                currentValue = settingsDisplay.ModelValue?.ToString();

                                displayName = settingsDisplay.DisplayName;

                                wasSettingsModified = true;
                            }

                            isModified = success;

                            break;

                        default:
                            throw new InvalidOperationException($"Unknown select row. Value: {result.Value.Row}");
                    }
                }
            }
            while (result is null || result.IsSuccess && !isClosed);


            if (!withSetSettings)
                return;

            Embed embed;

            if (wasSettingsModified)
            {
                var n = await Context.Db.SaveChangesAsync();

                if (n > 0)
                    embed = EmbedHelper.CreateEmbed("Настройки успешно сохранены", EmbedStyle.Successfull);
                else
                    embed = EmbedHelper.CreateEmbed("Изменения не найдены", EmbedStyle.Warning);

            }
            else
            {
                embed = EmbedHelper.CreateEmbed("Настройки не были сохранены", EmbedStyle.Warning);
            }

            _ = Interactive.DelayedSendMessageAndDeleteAsync(Context.Channel,
                embed: embed,
                deleteDelay: TimeSpan.FromSeconds(10),
                messageReference: new(Context.Message.Id));




            static Dictionary<string, SettingsDisplay> CreateSettingsDisplays(object model, object viewModel)
                => viewModel
                .GetType()
                .GetProperties()
                .ToDictionary(
                    p => p.Name,
                    p => new SettingsDisplay(p.Name, p.GetShortDisplayName(), model, viewModel));
        }

        private async Task<bool> TrySetParameterAsync(PropertyInfo parameter, object obj, string? description = null, string? displayName = null)
        {
            displayName ??= parameter.GetFullDisplayName();

            var cancelComponent = new ComponentBuilder()
                .WithButton("Отмена", "cancel", ButtonStyle.Danger)
                .Build();

            var embed = description is null
                ? EmbedHelper.CreateEmbed($"Укажите значение выбранного параметра **{displayName}**")
                : EmbedHelper.CreateEmbed(description, $"Укажите значение выбранного параметра {displayName}");

            var data = new MessageData()
            {
                Embed = embed,
                MessageReference = new(Context.Message.Id),
                MessageComponent = cancelComponent
            };


            var msg = await ReplyAsync(data);


            var valueTask = Interactive.NextMessageAsync(x => x.Channel.Id == msg.Channel.Id && x.Author.Id == Context.User.Id,
                timeout: 30d.ToTimeSpanSeconds());

            var cancelTask = Interactive.NextMessageComponentAsync(x => x.Message.Id == msg.Id && x.User.Id == Context.User.Id,
                timeout: 35d.ToTimeSpanSeconds());

            var task = await Task.WhenAny(valueTask, cancelTask);

            await msg.DeleteAsync();

            if (task == cancelTask)
                return false;


            var valueMessageResult = await valueTask;

            if (!valueMessageResult.IsSuccess)
            {
                await ReplyEmbedAndDeleteAsync($"Вы не указали значение параметра **{displayName}**", EmbedStyle.Warning);

                return false;
            }

            await valueMessageResult.Value.DeleteAsync();

            if (valueMessageResult.Value is not SocketMessage valueMessage)
            {
                await ReplyEmbedAndDeleteAsync("Неверное значение параметра", EmbedStyle.Error);

                return false;
            }
            if (parameter.PropertyType == typeof(bool?) || parameter.PropertyType == typeof(bool))
            {
                var result = await new BooleanTypeReader().ReadAsync(Context, valueMessage.Content);

                if (result.IsSuccess)
                    parameter.SetValue(obj, result.Values is not null ? result.BestMatch : null);
                else
                {
                    await ReplyEmbedAndDeleteAsync($"Не удалось установить значение параметра **{displayName}**", EmbedStyle.Error);

                    return false;
                }
            }
            else
            {
                var rawValue = valueMessage.Content;

                if (parameter.PropertyType == typeof(ulong) || parameter.PropertyType == typeof(ulong?))
                {
                    if (rawValue.Contains('<'))
                    {
                        rawValue = Regex.Replace(rawValue, @"[\D]", string.Empty);
                    }
                }

                var converter = TypeDescriptor.GetConverter(parameter.PropertyType);
                if (converter.IsValid(rawValue))
                {
                    var value = converter.ConvertFrom(rawValue);

                    parameter.SetValue(obj, value);
                }
                else
                {
                    await ReplyEmbedAndDeleteAsync($"Не удалось установить значение параметра **{displayName}**", EmbedStyle.Error);

                    return false;
                }
            }

            return true;
        }

        private void SetParameters<TData, TViewModel>(TData data, TViewModel viewModel)
            where TData : notnull
            where TViewModel : notnull
        {
            var dataParameters = data.GetType().GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);
            var vmParameters = viewModel.GetType().GetProperties().Where(p => p.CanWrite);

            foreach (var vmParam in vmParameters)
            {
                if (!dataParameters.TryGetValue(vmParam.Name, out var dataParam))
                {
                    GuildLogger.Warning(LogTemplate, nameof(SetParameters),
                        $"View Model parameter {vmParam.Name} was not found in Data parameters");

                    continue;
                }

                if (vmParam.PropertyType != dataParam.PropertyType && Nullable.GetUnderlyingType(vmParam.PropertyType) != dataParam.PropertyType)
                {
                    var msg = $"Param {dataParam.Name}: Data param type ({dataParam.PropertyType}) is not equals to ViewModel param type ({vmParam.PropertyType})";

                    GuildLogger.Warning(LogTemplate, nameof(SetParameters), msg);

                    Log.Warning(LogTemplate, nameof(SetParameters), $"[{Context.Guild.Name} {Context.Guild.Id}] {msg}");

                    continue;
                }


                if (vmParam.PropertyType == typeof(bool?) || vmParam.PropertyType == typeof(bool))
                {
                    var vmValue = vmParam.GetValue(viewModel);

                    if (vmValue is not null)
                        dataParam.SetValue(data, vmValue);
                }
                else if (vmParam.PropertyType == typeof(int?) || vmParam.PropertyType == typeof(int))
                {
                    var vmValue = vmParam.GetValue(viewModel);

                    if (vmValue is not null)
                    {
                        if (vmValue.Equals(-1))
                            dataParam.SetValue(data, default);
                        else
                            dataParam.SetValue(data, vmValue);
                    }
                }
                else if (vmParam.PropertyType == typeof(ulong?) || vmParam.PropertyType == typeof(ulong))
                {
                    var vmValue = vmParam.GetValue(viewModel);

                    if (vmValue is not null)
                    {
                        if (vmValue.Equals(0ul))
                            dataParam.SetValue(data, default);
                        else
                            dataParam.SetValue(data, vmValue);
                    }
                }
                else
                {
                    var vmValue = vmParam.GetValue(viewModel);

                    dataParam.SetValue(data, vmValue);
                }
            }
        }



        private class SettingsDisplay
        {
            private readonly string _propName;

            private readonly PropertyInfo _prop;


            public string DisplayName { get; }
            public object Model { get; }
            public object ViewModel { get; }


            public object? ModelValue => _prop.GetValue(Model);


            public SettingsDisplay(string propName, string displayName, object model, object viewModel)
            {
                DisplayName = displayName;
                Model = model;
                ViewModel = viewModel;

                _propName = propName;

                _prop = Model.GetType().GetProperty(_propName) ?? throw new InvalidOperationException($"Property \"{_propName}\" was not found");
            }
        }
    }





    public class MafiaHelpModule : HelpModule
    {
        public MafiaHelpModule(InteractiveService interactiveService, IConfiguration config) : base(interactiveService, config)
        {
        }


        [Command("Роли")]
        public virtual async Task ShowGameRolesAsync(bool sendToServer = false)
        {
            var gameRolesSection = GetGameSection("Roles");

            if (gameRolesSection is null)
            {
                await ReplyEmbedAsync("Список ролей не найден", EmbedStyle.Error);

                return;
            }


            var title = gameRolesSection.GetTitle() ?? "Роли";

            var builder = new EmbedBuilder()
                .WithTitle(title)
                .WithInformationMessage();

            var paginatorBuilder = new StaticPaginatorBuilder()
                .WithActionOnCancellation(ActionOnStop.DeleteMessage);

            foreach (var section in gameRolesSection.GetChildren())
            {
                var roleFields = section.GetSectionFields();


                if (!roleFields.TryGetValue("Key", out var name) || !roleFields.TryGetValue("Value", out var value))
                    continue;

                var pageBuilder = new PageBuilder()
                        .WithTitle(title)
                        .AddField(name, value);

                if (roleFields.TryGetValue("Color", out var colorStr) && uint.TryParse(colorStr, NumberStyles.HexNumber, null, out var rawColor))
                    pageBuilder.WithColor(new Color(rawColor));

                paginatorBuilder.AddPage(pageBuilder);
            }

            if (!sendToServer)
                await Interactive.SendPaginatorAsync(paginatorBuilder.Build(), await Context.User.CreateDMChannelAsync(), TimeSpan.FromMinutes(10));
            else
                await Interactive.SendPaginatorAsync(paginatorBuilder.Build(), Context.Channel, TimeSpan.FromMinutes(10));
        }
    }
}