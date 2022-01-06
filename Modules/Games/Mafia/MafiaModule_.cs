using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using Fergun.Interactive.Selection;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Services;
using Serilog;

namespace Modules.Games.Mafia;

[Group("Мафия")]
[Alias("м")]
public sealed partial class MafiaModule_ : GameModule<MafiaData>
{
    private readonly IMafiaSetupService _mafiaService;
    private readonly IOptionsSnapshot<GameRoleData> _gameRoleOptions;

    public MafiaModule_(InteractiveService interactiveService, IMafiaSetupService mafiaService, IOptionsSnapshot<GameRoleData> gameRoleOptions) : base(interactiveService)
    {
        _mafiaService = mafiaService;
        _gameRoleOptions = gameRoleOptions;
    }


    protected override MafiaData CreateGameData(IGuildUser host)
        => new("Мафия", 3, host, new());


    public override async Task StartAsync()
    {
        if (!CheckPreconditions(out var msg))
        {
            await ReplyEmbedAsync(msg, EmbedStyle.Error);

            return;
        }


        var data = GetGameData();
        data.Players.Shuffle(3);

        await ReplyEmbedAsync($"{data.Name} запущена", EmbedStyle.Successfull);


        var settings = await Context.GetGameSettingsAsync<MafiaSettings>();
        settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


        var context = await CreateMafiaContextAsync(settings, data);

        var tasks = new List<Task>
        {
            _mafiaService.SetupGuildAsync(context),
            _mafiaService.SetupUsersAsync(context)
        };

        await Task.WhenAll(tasks);

        _mafiaService.SetupRoles(context);

        context.RolesData.AssignRoles();

        if (settings.Current.ServerSubSettings.SendWelcomeMessage)
        {
            await _mafiaService.SendRolesInfoAsync(context);

            await Task.Delay(5000);
        }


        var game = new MafiaGame(context);

        await game.RunAsync();
    }

    [Command("хуяк")]
    [RequireOwner]
    public async Task Stop()
    {
        await ReplyAsync("Дергаем ручник");

        GetGameData().TokenSource.Cancel();
    }

    private async Task<MafiaContext> CreateMafiaContextAsync(MafiaSettings settings, MafiaData data)
    {
        settings.CategoryChannelId ??= (await Context.Guild.CreateCategoryChannelAsync("Мафия")).Id;


        var _guildData = new MafiaGuildData(
               await Context.Guild.GetTextChannelOrCreateAsync(settings.GeneralTextChannelId, "мафия-общий", SetCategoryChannel),
               await Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-убийцы", SetCategoryChannel),
               Context.Guild.GetTextChannel(settings.WatchersTextChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.GeneralVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.MurdersVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.WatchersVoiceChannelId ?? 0),
               await Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
               Context.Guild.GetRole(settings.WatcherRoleId ?? 0));


        if (settings.ClearChannelsOnStart)
        {
            await _guildData.GeneralTextChannel.ClearAsync();

            await _guildData.MurderTextChannel.ClearAsync();

            if (_guildData.SpectatorTextChannel is not null)
                await _guildData.SpectatorTextChannel.ClearAsync();
        }

        var context = new MafiaContext(_guildData, data, settings, Context, Interactive, _gameRoleOptions);


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
    [RequireUserPermission(GuildPermission.Administrator)]
    public class TemplatesModule : GuildModuleBase
    {
        public TemplatesModule(Fergun.Interactive.InteractiveService interactiveService) : base(interactiveService)
        {
        }

        [Command("Клонировать")]
        [Alias("клон", "к")]
        public async Task CloneTemplate(string newTemplateName, string? originalTemplateName = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            originalTemplateName ??= settings.CurrentTemplateName;

            if (newTemplateName == originalTemplateName)
            {
                await ReplyEmbedAsync($"Шаблон с именем **{originalTemplateName}** уже существует", EmbedStyle.Error);

                return;
            }

            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var template = originalTemplateName == settings.CurrentTemplateName
                ? settings.Current
                : await Context.Db.MafiaSettingsTemplates
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.MafiaSettingsId == settings.Id && s.Name == originalTemplateName);

            if (template is null)
            {
                await ReplyEmbedAsync("Шаблон-образец не найден", EmbedStyle.Error);

                return;
            }

            var newTemplate = new SettingsTemplate(newTemplateName)
            {
                MafiaSettingsId = settings.Id,
                ServerSubSettings = template.ServerSubSettings,
                GameSubSettings = template.GameSubSettings,
                RoleAmountSubSettings = template.RoleAmountSubSettings,
                RolesInfoSubSettings = template.RolesInfoSubSettings
            };


            await Context.Db.MafiaSettingsTemplates.AddAsync(newTemplate);

            settings.CurrentTemplateName = newTemplateName;

            await Context.Db.SaveChangesAsync();



            await ReplyEmbedAsync($"Шаблон **{newTemplateName}** успешно клонирован из шаблона **{originalTemplateName}**", EmbedStyle.Successfull);
        }


        [Command("Загрузить")]
        [Alias("згр", "з")]
        public async Task LoadTemplate(string name = MafiaSettings.DefaultTemplateName)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var template = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync("Шаблон не найден", EmbedStyle.Error);

                return;
            }

            settings.CurrentTemplateName = name;

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync($"Шаблон **{name}** успешно загружен", EmbedStyle.Successfull);
        }


        [Command("Текущий")]
        [Alias("тек", "т")]
        public async Task ShowCurrentTemplate()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            await ReplyEmbedAsync($"Текущий шаблон - **{settings.CurrentTemplateName}**");
        }


        [Command("Список")]
        [Alias("сп")]
        public async Task ShowAllTemplates()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            var templates = Context.Db.MafiaSettingsTemplates
                .AsNoTracking()
                .Where(t => t.MafiaSettingsId == settings.Id);

            var str = "";

            foreach (var template in templates)
                str += $"**{template.Name}**\n";

            await ReplyEmbedAsync(str, "Список шаблонов");
        }



        [Command("Сообщение")]
        [Alias("сбщ")]
        [Priority(-1)]
        public async Task ShowPreGameMessage()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            await ReplyEmbedAsync(settings.Current.PreGameMessage ?? "*Сообщение отсутствует*", "Сообщение перед игрой");
        }

        [Command("Сообщение")]
        [Alias("сбщ")]
        public async Task UpdatePreGameMessage([Remainder] string message)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            var template = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            template.PreGameMessage = message;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync("Сообщение успешно изменено", EmbedStyle.Successfull);
        }


        [Command("Имя")]
        public async Task UpdateTemplateName(string newName, string? templateName = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            templateName ??= settings.CurrentTemplateName;

            var templateToUpdate = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == templateName);


            if (templateToUpdate is null)
            {
                await ReplyEmbedAsync($"Шаблон для замены имени **{templateName}** не найден", EmbedStyle.Error);

                return;
            }

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


            var oldName = templateToUpdate.Name;

            templateToUpdate.Name = newName;
            settings.CurrentTemplateName = newName;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Имя шаблона успешно изменено: **{oldName}** -> **{newName}**", EmbedStyle.Successfull);
        }


        [Command("Сброс")]
        public async Task ResetTemplate(string? name = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            name ??= settings.CurrentTemplateName;

            var template = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync("Шаблон не найден", EmbedStyle.Error);

                return;
            }

            template.GameSubSettings = new();
            template.ServerSubSettings = new();
            template.RolesInfoSubSettings = new();
            template.RoleAmountSubSettings = new();

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync($"Шаблон **{name}** успешно сброшен", EmbedStyle.Successfull);
        }


        [Command("Удалить")]
        public async Task DeleteTemplate(string? name = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            name ??= settings.CurrentTemplateName;

            if (name == MafiaSettings.DefaultTemplateName)
            {
                await ReplyEmbedAsync("Невозможно удалить шаблон по умолчанию", EmbedStyle.Error);

                return;
            }

            if (name == settings.CurrentTemplateName)
            {
                await ReplyEmbedAsync("Невозможно удалить активный шаблон", EmbedStyle.Error);

                return;
            }


            var template = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == name);

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
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Настройки для мафии включают в себя настройки сервера(используемые роли, каналы и категорию каналов) и настройки самой игры. " +
        "Для подробностей введите команду **Мафия.Настройки.Помощь**")]
    public class SettingsModule : GuildModuleBase
    {
        public SettingsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Command]
        [Priority(-2)]
        public async Task SetSettingsAsync()
        {
            var builder = new ComponentBuilder();

            builder.WithButton(customId: $"stop", style: ButtonStyle.Danger, emote: new Emoji("❌"));
            var msg = await ReplyAsync("Press this button!");

            InteractiveMessageResult<PropertyInfo?>? result = null;

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var settingsVM = new MafiaSettingsViewModel();

            var message = "Выберите параметр из списка ниже, чтобы изменить значение";

            var wasSettingsUpdated = false;

            do
            {
                // var cancelTask = Interactive.NextMessageComponentAsync(x => x.Message.Id == msg.Id, timeout: TimeSpan.FromSeconds(300));

                var parametersVM = settingsVM.GetType().GetProperties().Where(p => p.CanWrite).ToList();
                var parameters = settings.GetType().GetProperties().Where(p => p.CanWrite).IntersectBy(parametersVM.Select(p => p.Name), p => p.Name);

                var parameterNames = GetPropertiesName(parametersVM).ToList();

                var page = new PageBuilder()
                    .WithTitle("Настройки")
                    .WithDescription(message)
                    .AddField("Параметр", string.Join('\n', parameterNames), true)
                    .AddField("Значение", string.Join('\n', parameters.Select(p =>
                    {
                        var value = p.GetValue(settings);

                        if (ulong.TryParse(value?.ToString(), out var id))
                            return Context.Guild.GetMentionFromId(id);

                        return value ?? "[Null]";

                    })), true);


                var selection = new SelectionBuilder<PropertyInfo>()
                    .AddUser(Context.User)
                    .WithOptions(parametersVM)
                    .WithSelectionPage(page)
                    .WithInputType(InputType.SelectMenus)
                    .WithStringConverter(p => GetPropertyName(p))
                    .WithAllowCancel(true)
                    .Build();

                //builder.

                var selectTask = Interactive.SendSelectionAsync(selection, msg, TimeSpan.FromMinutes(2));

                //await msg.ModifyAsync(x => x.Components = selection.BuildComponents(false));

                //var task = await Task.WhenAny(cancelTask, selectTask);

                //if (task == cancelTask)
                //    break;


                result = await selectTask;


                if (result.IsSuccess && result.Value is not null)
                {
                    if (await TrySetParameterAsync(result.Value, settingsVM))
                    {
                        message = $"Параметр успешно настроен: {result.Value.Name}: {result.Value.GetValue(settingsVM)}";

                        SetParameters(settings, settingsVM);

                        wasSettingsUpdated = true;
                    }
                }
            }
            while (result?.IsSuccess ?? false);


            if (!wasSettingsUpdated)
            {
                await ReplyEmbedAsync("Общие настройки мафии не были сохранены", EmbedStyle.Warning);

                return;
            }

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync("Общие настройки мафии успешно сохранены", EmbedStyle.Successfull);
        }

        [Command("menu", RunMode = RunMode.Async)]
        public async Task MenuAsync()
        {
            // Create CancellationTokenSource that will be canceled after 10 minutes.
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            var options = new[]
            {
                "Cache messages",
                "Cache users",
                "Allow using mentions as prefix",
                "Ignore command errors",
                "Cache messages",
                "Cache users",
                "Allow using mentions as prefix",
                "Ignore command errors",
                "Allow using mentions as prefix",
                "Cache users"
            };

            var values = new[]
            {
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                false
            };

            // Dynamically create the number emotes
            var emotes = Enumerable.Range(1, options.Length)
                .ToDictionary(x => new Emoji($"{x}\ufe0f\u20e3") as IEmote, y => y);

            // Add the cancel emote at the end of the dictionary
            emotes.Add(new Emoji("❌"), -1);

            var color = Color.Green;

            // Prefer disabling the input (buttons, select menus) instead of removing them from the message.
            var actionOnStop = ActionOnStop.DisableInput;

            InteractiveMessageResult<KeyValuePair<IEmote, int>> result = null!;
            IUserMessage message = null!;

            while (result is null || result.Status == InteractiveStatus.Success)
            {
                var pageBuilder = new PageBuilder()
                    .WithTitle("Bot Control Panel")
                    .WithDescription("Use the reactions/buttons to enable or disable an option.")
                    .AddField("Option", string.Join('\n', options.Select((x, i) => $"**{i + 1}**. {x}")), true)
                    .AddField("Value", string.Join('\n', values), true)
                    .WithColor(color);

                var selection = new EmoteSelectionBuilder<int>()
                    .AddUser(Context.User)
                    .WithSelectionPage(pageBuilder)
                    .WithOptions(emotes)
                    .WithAllowCancel(true)
                    .WithActionOnCancellation(actionOnStop)
                    .WithActionOnTimeout(actionOnStop)
                    .Build();

                // if message is null, SendSelectionAsync() will send a message, otherwise it will modify the message.
                // The cancellation token persists here, so it will be canceled after 10 minutes no matter how many times the selection is used.
                result = message is null
                    ? await Interactive.SendSelectionAsync(selection, Context.Channel, TimeSpan.FromMinutes(10), cancellationToken: cts.Token)
                    : await Interactive.SendSelectionAsync(selection, message, TimeSpan.FromMinutes(10), cancellationToken: cts.Token);



                // Store the used message.
                message = result.Message;

                // Break the loop if the result isn't successful
                if (!result.IsSuccess)
                    break;

                int selected = result.Value.Value;

                // Invert the value of the selected option
                values[selected - 1] = !values[selected - 1];

                // Do stuff with the selected option
            }
        }


        [Command("Общие")]
        [Alias("о")]
        public async Task SetGeneralSettingsAsync()
        {
            var settingsVM = new MafiaSettingsViewModel();

            var success = await TrySetParametersAsync(settingsVM);


            if (!success)
            {
                await ReplyEmbedAsync("Общие настройки мафии не были сохранены", EmbedStyle.Warning);

                return;
            }

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            SetParameters(settings, settingsVM);

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync("Общие настройки мафии успешно сохранены", EmbedStyle.Successfull);
        }


        [Command("Автонастройка")]
        [Alias("ан")]
        public async Task AutoSetGeneralSettingsAsync()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();


            var categoryChannel = await Context.Guild.GetCategoryChannelOrCreateAsync(settings.CategoryChannelId, "Мафия", SetCategoryChannel);

            settings.CategoryChannelId = categoryChannel.Id;


            var generalTextChannelTask = Context.Guild.GetTextChannelOrCreateAsync(settings.GeneralTextChannelId, "мафия-общий", SetCategoryChannel);
            var murderTextChannelTask = Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-убийцы", SetCategoryChannel);
            var spectatorsTextChannelTask = Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-наблюдатели", SetCategoryChannel);
            var generalVoiceChannelTask = Context.Guild.GetVoiceChannelOrCreateAsync(settings.GeneralVoiceChannelId, "мафия-общий", SetCategoryChannel);
            var murdersVoiceChannelTask = Context.Guild.GetVoiceChannelOrCreateAsync(settings.MurdersVoiceChannelId, "мафия-убийцы", SetCategoryChannel);

            var spectatorssVoiceChannelTask = Context.Guild.GetVoiceChannelOrCreateAsync(settings.WatchersVoiceChannelId, "мафия-наблюдатели", SetCategoryChannel);
            var mafiaRoleTask = Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true);
            var spectatorRoleTask = Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Наблюдатель мафии", null, Color.DarkBlue, true, true);


            var settigsVM = new MafiaSettingsViewModel()
            {
                GeneralTextChannelId = (await generalTextChannelTask).Id,
                MurdersTextChannelId = (await murderTextChannelTask).Id,
                WatchersTextChannelId = (await spectatorsTextChannelTask).Id,
                GeneralVoiceChannelId = (await generalVoiceChannelTask).Id,
                MurdersVoiceChannelId = (await murdersVoiceChannelTask).Id,
                WatchersVoiceChannelId = (await spectatorssVoiceChannelTask).Id,
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
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();


            var tasksNullable = new List<Task?>
            {
                Context.Guild.GetCategoryChannel(settings.CategoryChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetTextChannel(settings.GeneralTextChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetTextChannel(settings.MurdersTextChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetTextChannel(settings.WatchersTextChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetVoiceChannel(settings.GeneralVoiceChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetVoiceChannel(settings.MurdersVoiceChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetVoiceChannel(settings.WatchersVoiceChannelId ?? 0)?.DeleteAsync(),
                Context.Guild.GetRole(settings.MafiaRoleId ?? 0)?.DeleteAsync(),
                Context.Guild.GetRole(settings.WatcherRoleId ?? 0)?.DeleteAsync()
            };

            List<Task> tasks = tasksNullable.Where(t => t is not null).ToList()!;

            await Task.WhenAll(tasks);


            settings.CategoryChannelId = null;
            settings.GeneralTextChannelId = null;
            settings.MurdersTextChannelId = null;
            settings.WatchersTextChannelId = null;
            settings.GeneralVoiceChannelId = null;
            settings.MurdersVoiceChannelId = null;
            settings.WatchersVoiceChannelId = null;
            settings.MafiaRoleId = null;
            settings.WatcherRoleId = null;


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedStampAsync("Общие настройки успешно сброшены", EmbedStyle.Successfull);
        }


        [Command("Роли")]
        [Alias("р")]
        public async Task SetRoleAmountSettingsAsync()
        {
            var settingsVM = new RoleAmountSubSettingsViewModel();

            var success = await TrySetParametersAsync(settingsVM);

            if (!success)
            {
                await ReplyEmbedAsync("Настройки ролей не были сохранены", EmbedStyle.Warning);

                return;
            }

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();
            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);

            var roleAmountSettings = settings.Current.RoleAmountSubSettings;
            SetParameters(roleAmountSettings, settingsVM);
            settings.Current.RoleAmountSubSettings = roleAmountSettings;


            if (settings.Current.RoleAmountSubSettings.MurdersCount == 0 && settings.Current.RoleAmountSubSettings.DonsCount == 0)
            {
                await ReplyEmbedAndDeleteAsync("Для игры необходима хотя бы одна черная роль", EmbedStyle.Error);

                return;
            }


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAndDeleteAsync("Настройки ролей успешно сохранены", EmbedStyle.Successfull);
        }

        [Command("РолиДействия")]
        [Alias("РДействия", "рд")]
        public async Task SetRolesInfoSubSettingsAsync()
        {
            var settingsVM = new RolesInfoSubSettingsViewModel();

            var success = await TrySetParametersAsync(settingsVM);

            if (!success)
            {
                await ReplyEmbedAsync("Дополнительные настройки ролей не были сохранены", EmbedStyle.Warning);

                return;
            }

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current ??= Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var rolesSettings = settings.Current.RolesInfoSubSettings;
            SetParameters(rolesSettings, settingsVM);
            settings.Current.RolesInfoSubSettings = rolesSettings;


            var MurdersKnowEachOther = settings.Current.RolesInfoSubSettings.MurdersKnowEachOther;
            var MurdersVoteTogether = settings.Current.RolesInfoSubSettings.MurdersVoteTogether;

            if (!MurdersKnowEachOther && MurdersVoteTogether)
            {
                await ReplyEmbedAndDeleteAsync($"Конфликт настроек. " +
                    $"Параметры {nameof(MurdersKnowEachOther)} ({MurdersKnowEachOther}) и {nameof(MurdersVoteTogether)} ({MurdersVoteTogether}) взаимоисключают друг друга. " +
                    $"Измените значение одного или двух параметров для устранения конфликта", EmbedStyle.Error);

                return;
            }


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAndDeleteAsync("Дополнительные настройки ролей успешно сохранены", EmbedStyle.Successfull);
        }

        [Command("Сервер")]
        [Alias("серв", "с")]
        public async Task SetServerSubSettingsAsync()
        {
            var serverSettingsVM = new ServerSubSettingsViewModel();

            var success = await TrySetParametersAsync(serverSettingsVM);


            if (!success)
            {
                await ReplyEmbedAsync("Серверные настройки не были сохранены", EmbedStyle.Warning);

                return;
            }

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current ??= Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var serverSettings = settings.Current.ServerSubSettings;
            SetParameters(serverSettings, serverSettingsVM);
            settings.Current.ServerSubSettings = serverSettings;


            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync("Настройки сервера успешно сохранены", EmbedStyle.Successfull);
        }

        [Command("Игра")]
        [Alias("и")]
        public async Task SetGameSubSettingsAsync()
        {
            var settingsVM = new GameSubSettingsViewModel();

            var success = await TrySetParametersAsync(settingsVM);


            if (!success)
            {
                await ReplyEmbedAsync("Настройки игры не были сохранены", EmbedStyle.Warning);

                return;
            }

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var gameSettings = settings.Current.GameSubSettings;
            SetParameters(gameSettings, settingsVM);
            settings.Current.GameSubSettings = gameSettings;


            if (settings.Current.GameSubSettings.MafiaCoefficient <= 1)
            {
                await ReplyEmbedAndDeleteAsync( "Коэффиент мафии не может быть меньше 2. Установлено стандартное значение **3**", EmbedStyle.Warning);

                settings.Current.GameSubSettings = gameSettings with
                {
                    MafiaCoefficient = 3
                };
            }

            if (settings.Current.GameSubSettings.VoteTime <= 0)
            {
                await ReplyEmbedAndDeleteAsync( "Установлено стандартное значение времени голосования: **40**", EmbedStyle.Warning);

                settings.Current.GameSubSettings = gameSettings with
                {
                    VoteTime = 40
                };
            }

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync("Настройки сервера успешно сохранены", EmbedStyle.Successfull);
        }


        [Command("Текущие")]
        [Alias("тек", "т")]
        public async Task ShowAllSettingsAsync()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);

            // Mapper

            var settingsVM = new MafiaSettingsViewModel()
            {
                CategoryChannelId = settings.CategoryChannelId,
                GeneralTextChannelId = settings.GeneralTextChannelId,
                MurdersTextChannelId = settings.MurdersTextChannelId,
                WatchersTextChannelId = settings.WatchersTextChannelId,
                GeneralVoiceChannelId = settings.GeneralVoiceChannelId,
                MurdersVoiceChannelId = settings.MurdersVoiceChannelId,
                WatchersVoiceChannelId = settings.WatchersVoiceChannelId,
                MafiaRoleId = settings.MafiaRoleId,
                WatcherRoleId = settings.WatcherRoleId
            };

            await ShowSettingsAsync(settingsVM, "Общие настройки");
            await ShowSettingsAsync(settings.Current.ServerSubSettings, "Настройки сервера");
            await ShowSettingsAsync(settings.Current.GameSubSettings, "Настройки игры");
            await ShowSettingsAsync(settings.Current.RoleAmountSubSettings, "Настройки количества ролей");
            await ShowSettingsAsync(settings.Current.RolesInfoSubSettings, "Настройки действий ролей");
        }


        [Command("Проверка")]
        [Alias("чек")]
        public async Task CheckSettingsAsync()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);


            var message = "";

            message += Handle(settings.CategoryChannelId, nameof(settings.CategoryChannelId), Context.Guild.GetCategoryChannel) + "\n";
            message += Handle(settings.MafiaRoleId, nameof(settings.MafiaRoleId), Context.Guild.GetRole) + "\n";
            message += Handle(settings.WatcherRoleId, nameof(settings.WatcherRoleId), Context.Guild.GetRole) + "\n";
            message += Handle(settings.GeneralTextChannelId, nameof(settings.GeneralTextChannelId), Context.Guild.GetTextChannel) + "\n";
            message += Handle(settings.MurdersTextChannelId, nameof(settings.MurdersTextChannelId), Context.Guild.GetTextChannel) + "\n";
            message += Handle(settings.WatchersTextChannelId, nameof(settings.WatchersTextChannelId), Context.Guild.GetTextChannel) + "\n";
            message += Handle(settings.GeneralVoiceChannelId, nameof(settings.GeneralVoiceChannelId), Context.Guild.GetVoiceChannel) + "\n";
            message += Handle(settings.MurdersVoiceChannelId, nameof(settings.MurdersVoiceChannelId), Context.Guild.GetVoiceChannel) + "\n";
            message += Handle(settings.WatchersVoiceChannelId, nameof(settings.WatchersVoiceChannelId), Context.Guild.GetVoiceChannel);


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


        private async Task ShowSettingsAsync<T>(T settings, string title) where T : notnull
        {
            var parameters = settings.GetType().GetProperties().ToList();

            var fields = parameters.Select(p => new EmbedFieldBuilder()
            {
                Name = GetPropertyName(p),
                Value = p.GetValue(settings) ?? "Null",
                IsInline = true,
            });

            var embedBuilder = new EmbedBuilder()
                .WithTitle(title)
                .WithFields(fields)
                .WithInformationMessage(false)
                .WithUserFooter(Context.User)
                .WithCurrentTimestamp();


            await ReplyAsync(embed: embedBuilder.Build());
        }


        private async Task<bool> TrySetParametersAsync<T>(T settings) where T : notnull
        {
            var wasSetSettings = false;


            var parameters = settings.GetType().GetProperties().Where(p => p.CanWrite).ToList();

            var parameterNames = GetPropertiesName(parameters).ToList();

            var emotes = GetEmotesList(parameterNames.Count, parameterNames, out var text);


            while (true)
            {
                var message = await ReplyEmbedAsync($"Выберите интересующий вас параметр\n{text}");


                var reactionResult = await NextReactionAsync(message, TimeSpan.FromSeconds(30), emotes, true);


                if (!reactionResult.IsSuccess)
                {
                    await ReplyEmbedAsync("Вы не выбрали параметр", EmbedStyle.Warning);

                    break;
                }

                if (reactionResult?.Value.Emote is not IEmote selectedEmote)
                {
                    await ReplyEmbedAsync("Неверный параметр", EmbedStyle.Warning);

                    break;
                }

                if (selectedEmote.Name == CancelEmote.Name)
                {
                    await ReplyEmbedAsync("Вы отменили выбор");

                    break;
                }

                var index = emotes.IndexOf(selectedEmote);


                if (index == -1 || index > parameters.Count)
                {
                    await ReplyEmbedAsync("Параметр не найден", EmbedStyle.Error);

                    break;
                }

                if (!await TrySetParameterAsync(parameters[index], settings, parameterNames[index]))
                    break;

                wasSetSettings = true;

                await ReplyEmbedAndDeleteAsync($"Значение параметра **{parameterNames[index]}** успешно установлено", EmbedStyle.Successfull);


                var isContinue = await ConfirmActionAsync("Продолжить настройку?");

                if (isContinue is not true)
                    break;
            }


            if (wasSetSettings)
                await ReplyEmbedAndDeleteAsync("Настройки успешно установлены", EmbedStyle.Successfull);
            else
                await ReplyEmbedAndDeleteAsync("Настройки не были установлены", EmbedStyle.Warning);

            return wasSetSettings;
        }

        private async Task<bool> TrySetParameterAsync(PropertyInfo parameter, object obj, string? displayName = null)
        {
            displayName ??= parameter.GetPropertyFullName();

            var embed = EmbedHelper.CreateEmbed($"Укажите значение выбранного параметра **{displayName}**");
            var valueMessageResult = await NextMessageAsync(embed: embed);

            if (!valueMessageResult.IsSuccess)
            {
                await ReplyEmbedAndDeleteAsync($"Вы не указали значение параметра **{displayName}**", EmbedStyle.Warning);

                return false;
            }

            if (valueMessageResult?.Value is not SocketMessage valueMessage)
            {
                await ReplyEmbedAsync("Неверное значение параметра", EmbedStyle.Warning);

                return false;
            }
            if (parameter.PropertyType == typeof(bool?) || parameter.PropertyType == typeof(bool))
            {
                var result = await new BooleanTypeReader().ReadAsync(Context, valueMessage.Content);

                if (result.IsSuccess)
                    parameter.SetValue(obj, result.Values is not null ? result.BestMatch : null);
                else
                {
                    await ReplyEmbedAsync($"Не удалось установить значение параметра **{displayName}**", EmbedStyle.Error);

                    return false;
                }
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(parameter.PropertyType);
                if (converter.IsValid(valueMessage.Content))
                {
                    var value = converter.ConvertFrom(valueMessage.Content);

                    parameter.SetValue(obj, value);
                }
                else
                {
                    await ReplyEmbedAsync($"Не удалось установить значение параметра **{displayName}**", EmbedStyle.Error);

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

        private static IEnumerable<string> GetPropertiesName(IEnumerable<PropertyInfo> props)
            => props.Select(p => GetPropertyName(p));

        private static string GetPropertyName(PropertyInfo prop)
        {
            var displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();

            return displayNameAttribute is not null
            ? $"{displayNameAttribute.DisplayName} {prop.GetPropertyShortType()}"
            : prop.GetPropertyFullName();
        }
    }

}