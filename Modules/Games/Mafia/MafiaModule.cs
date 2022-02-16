using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Chronology;
using Core.Common.Data;
using Core.Extensions;
using Core.TypeReaders;
using Core.ViewModels;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Infrastructure.Data.Models;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Modules.Common.MultiSelect;
using Modules.Common.Preconditions;
using Modules.Games.Mafia.Common;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.Services;
using Modules.Games.Services;
using Serilog;
using static System.Formats.Asn1.AsnWriter;

namespace Modules.Games.Mafia;

[Group("Мафия")]
[Alias("м")]
public class MafiaModule : GameModule<MafiaData, MafiaStats>
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

        data.IsPlaying = true;

        await ReplyEmbedStampAsync($"{data.Name} успешно запущена", EmbedStyle.Successfull);


        try
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            if (settings.CurrentTemplate.ServerSubSettings.MentionPlayersOnGameStart)
                await MentionPlayersAsync();

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

            var (winner, chronology) = await game.RunAsync();

            Task? updateStatsTask = null;

            if (winner.Role is not null)
            {
                if (settings.CurrentTemplate.GameSubSettings.IsRatingGame)
                    updateStatsTask = UpdateStatsAsync(context.RolesData.AllRoles.Values, winner);


                await ReplyEmbedAsync($"Победителем оказался: {winner.Role.Name}!");
            }
            else
                await ReplyEmbedAsync("Город опустел... Никто не победил");

            await ReplyEmbedAsync("Хронология игры");



            _ = ShowChronology(context.RolesData.AllRoles.Keys, chronology);


            await ReplyEmbedStampAsync($"{data.Name} успешно завершена", EmbedStyle.Successfull);

            if (updateStatsTask is not null)
            {
                await updateStatsTask;

                await ReplyEmbedAsync("Статистика успешно обновлена", EmbedStyle.Successfull);
            }
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



    private async Task<int> UpdateStatsAsync(IEnumerable<GameRole> roles, Winner winner)
    {
        var rolesDict = roles.ToDictionary(p => p.Player.Id);

        var stats = await Context.Db.MafiaStats
            .Where(ms => ms.GuildSettingsId == Context.Guild.Id && rolesDict.Keys.Any(id => id == ms.UserId))
            .ToDictionaryAsync(s => s.UserId);

        int n = 0;

        if (stats.Count < rolesDict.Count)
            n = await AddNewStatsAsync(rolesDict.Keys, stats);

        if (n > 0)
            await ReplyEmbedAsync($"Добавлено новых статистик: {n}", EmbedStyle.Debug);

        foreach (var role in rolesDict.Values)
            role.UpdateStats(stats[role.Player.Id], winner);

        return await Context.Db.SaveChangesAsync();
    }


    private async Task ShowChronology(IEnumerable<IGuildUser> players, MafiaChronology chronology)
    {
        try
        {
            var entryEmbed = EmbedHelper.CreateEmbed("Просмотреть хронологию игры");

            var entryComponent = new ComponentBuilder()
                .WithButton("Открыть", "showChronology")
                .Build();

            var entryMsg = await ReplyAsync(embed: entryEmbed, components: entryComponent);


            var paginator = chronology.BuildActionsHistoryPaginator(players);


            var timeout = TimeSpan.FromMinutes(3);

            var cts = new CancellationTokenSource(timeout);

            var playersIds = new HashSet<ulong>();

            while (!cts.IsCancellationRequested)
            {
                var res = await Interactive.NextMessageComponentAsync(
                    x =>
                    x.Message.Id == entryMsg.Id && !playersIds.Contains(x.User.Id),
                    timeout: timeout, cancellationToken: cts.Token);

                if (res.IsSuccess)
                {
                    try
                    {
                        var value = res.Value;

                        await value.DeferAsync(true);

                        playersIds.Add(value.User.Id);

                        _ = Interactive.SendPaginatorAsync(paginator, value, timeout, InteractionResponseType.DeferredChannelMessageWithSource, ephemeral: true, resetTimeoutOnInput: true);
                    }
                    catch (Exception e)
                    {
                        GuildLogger.Error(e, LogTemplate, nameof(ShowChronology),
                            "Error occured when send paginator");

                        var embed = EmbedHelper.CreateEmbed("Произошла ошибка во время отправки хронологии", EmbedStyle.Error);

                        await res.Value.FollowupAsync(embed: embed, ephemeral: true);
                    }
                }
            }

            try
            {
                await entryMsg.DeleteAsync();
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.NotFound)
            { }
        }
        catch (Exception e)
        {
            GuildLogger.Error(e, LogTemplate, nameof(ShowChronology),
                "Error occured when show chronology");

            await ReplyEmbedAsync("Произошла ошибка во время показа хронологии", EmbedStyle.Error);
        }
    }


    public class MafiaStatsModule : GameStatsModule
    {
        public MafiaStatsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }

        [Command("Рейтинг")]
        [Alias("Рейт", "Р")]
        public async Task ShowRatingAsync(int playersPerPage = 10)
        {
            var allStats = await Context.Db.MafiaStats
                .AsNoTracking()
                .Where(s => s.GuildSettingsId == Context.Guild.Id)
                .OrderByDescending(stat => stat.Rating)
                .ThenByDescending(stat => stat.WinRate + stat.BlacksWinRate)
                .ThenBy(stat => stat.GamesCount)
                .ToListAsync();

            if (allStats.Count == 0)
            {
                await ReplyEmbedAsync("Рейтинг отсутствует", EmbedStyle.Warning);

                return;
            }


            var playersId = allStats
                .Select(s => s.UserId)
                .ToHashSet();

            if (Context.Guild.Users.Count < Context.Guild.MemberCount)
            {
                await ReplyEmbedAsync($"Downloading users ({Context.Guild.MemberCount - Context.Guild.Users.Count})...", EmbedStyle.Debug);
                await Context.Guild.DownloadUsersAsync();
            }

            var players = Context.Guild.Users
                .Where(u => playersId.Contains(u.Id))
                .ToDictionary(u => u.Id);

            playersPerPage = Math.Clamp(playersPerPage, 1, 30);

            var lazyPaginator = new LazyPaginatorBuilder()
                .WithActionOnCancellation(ActionOnStop.DeleteMessage)
                .WithActionOnTimeout(ActionOnStop.DeleteMessage)
                .WithMaxPageIndex((allStats.Count - 1) / playersPerPage)
                .WithCacheLoadedPages(true)
                .WithPageFactory(page =>
                {
                    int n = playersPerPage * page + 1;

                    var pageBuilder = new PageBuilder()
                    {
                        Title = $"Рейтинг [{page * playersPerPage + 1} - {(page + 1) * playersPerPage}]",
                        Color = Utils.GetRandomColor(),
                        Description = string.Join('\n', allStats
                        .Skip(page * playersPerPage)
                        .Take(playersPerPage)
                        .Select(ms => $"{n++}. **{players[ms.UserId].GetFullName()}** - {ms.Rating:0.##}"))
                    };

                    return pageBuilder;
                })
                .Build();

            _ = Interactive.SendPaginatorAsync(lazyPaginator, Context.Channel, timeout: 10d.ToTimeSpanMinutes());
        }




        protected override EmbedBuilder GetStatsEmbedBuilder(MafiaStats stats, IUser user)
        {
            var embedBuilder = base.GetStatsEmbedBuilder(stats, user);

            return embedBuilder
                .AddField("% побед за мафию", $"{stats.BlacksWinRate:P2} ({stats.BlacksWinsCount}/{stats.BlacksGamesCount})", true)
                .AddField("Эффективность доктора", $"{stats.DoctorEfficiency:P2} ({stats.DoctorHealsCount}/{stats.DoctorMovesCount})", true)
                .AddField("Эффективность шерифа", $"{stats.SheriffEfficiency:P2} ({stats.SheriffRevealsCount}/{stats.SheriffMovesCount})", true)
                .AddField("Эффективность дона", $"{stats.DonEfficiency:P2} ({stats.DonRevealsCount}/{stats.DonMovesCount})", true)
                .AddField("Кол-во основных очков", stats.Scores.ToString("0.##"), true)
                .AddField("Кол-во доп. очков", stats.ExtraScores.ToString("0.##"), true)
                .AddField("Кол-во штрафных очков", stats.PenaltyScores.ToString("0.##"), true)
                .AddEmptyField(true)
                .AddField("Рейтинг", stats.Rating.ToString("0.##"));
        }




        [Group]
        [RequireUserPermission(GuildPermission.Administrator)]
        public class AdminModule : GuildModuleBase
        {
            public AdminModule(InteractiveService interactiveService) : base(interactiveService)
            {
            }

            [Command("РейтСброс")]
            [Alias("РСброс")]
            [RequireConfirmAction]
            public async Task ResetRatingAsync()
            {
                var allStats = await Context.Db.MafiaStats
                    .Where(s => s.GuildSettingsId == Context.Guild.Id)
                    .ToListAsync();

                foreach (var stat in allStats)
                    stat.Reset();

                await Context.Db.SaveChangesAsync();

                await ReplyEmbedStampAsync("Рейтинг Мафии успешно сброшен", EmbedStyle.Successfull);
            }


            [Group]
            [RequireConfirmAction(false)]
            public class ScoresModule : GuildModuleBase
            {
                public ScoresModule(InteractiveService interactiveService) : base(interactiveService)
                {
                }



                [Command("ДопОчки+")]
                [Alias("ДО+")]
                public async Task AddExtraScoresAsync(float scores, IUser? user = null)
                {
                    if (!await TryUpdateScoresAsync(scores, user, true))
                        return;

                    await ReplyEmbedStampAsync("Дополнительные очки успешно начислены", EmbedStyle.Successfull);
                }

                [Command("ДопОчки-")]
                [Alias("ДО-")]
                public async Task RemoveExtraScoresAsync(float scores, IUser? user = null)
                {
                    if (!await TryUpdateScoresAsync(-scores, user, true))
                        return;

                    await ReplyEmbedStampAsync("Дополнительные очки успешно списаны", EmbedStyle.Successfull);
                }

                [Command("ДопОчкиСброс")]
                [Alias("ДОС")]
                public async Task ResetExtraScoresAsync(IUser? user = null)
                {
                    if (!await TryUpdateScoresAsync(null, user, true))
                        return;

                    await ReplyEmbedStampAsync("Дополнительные очки успешно сброшены", EmbedStyle.Successfull);
                }


                [Command("ШтрафОчки+")]
                [Alias("ШО+")]
                public async Task AddPenaltyScoresAsync(float scores, IUser? user = null)
                {
                    if (!await TryUpdateScoresAsync(scores, user, false))
                        return;

                    await ReplyEmbedStampAsync("Штрафные очки успешно начислены", EmbedStyle.Successfull);
                }

                [Command("ШтрафОчки-")]
                [Alias("ШО-")]
                public async Task RemovePenaltyScoresAsync(float scores, IUser? user = null)
                {
                    if (!await TryUpdateScoresAsync(-scores, user, false))
                        return;

                    await ReplyEmbedStampAsync("Штрафные очки успешно списаны", EmbedStyle.Successfull);
                }

                [Command("ШтрафОчкиСброс")]
                [Alias("ШОС")]
                public async Task ResetPenaltyScoresAsync(IUser? user = null)
                {
                    if (!await TryUpdateScoresAsync(null, user, false))
                        return;

                    await ReplyEmbedStampAsync("Штрафные очки успешно сброшены", EmbedStyle.Successfull);
                }


                private async Task<bool> TryUpdateScoresAsync(float? scores, IUser? user, bool extra)
                {
                    user ??= Context.User;

                    var userStat = await Context.Db.MafiaStats
                        .FirstOrDefaultAsync(ms => ms.GuildSettingsId == Context.Guild.Id && ms.UserId == user.Id);

                    if (userStat is null)
                    {
                        await ReplyEmbedAsync($"Статистика игрока {user.GetFullMention()} не найдена", EmbedStyle.Error);

                        return false;
                    }

                    if (extra)
                        userStat.ExtraScores += scores ?? -userStat.ExtraScores;
                    else
                        userStat.PenaltyScores += scores ?? -userStat.PenaltyScores;

                    await Context.Db.SaveChangesAsync();

                    return true;
                }
            }
        }
    }


    [Group("Шаблоны")]
    [Alias("Ш")]
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
                RolesExtraInfoSubSettings = settings.CurrentTemplate.RolesExtraInfoSubSettings
            };


            Context.Db.MafiaSettingsTemplates.Add(newTemplate);

            await Context.Db.SaveChangesAsync();


            settings.CurrentTemplateId = newTemplate.Id;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Шаблон **{newTemplateName}** успешно клонирован из шаблона **{originalTemplateName}**", EmbedStyle.Successfull);

            await ReplyEmbedAsync($"Текущий шаблон: {newTemplate.Name}");
        }


        [Command("Загрузить")]
        [Alias("згр", "з")]
        public async Task LoadTemplate([Remainder] string name = MafiaSettingsTemplate.DefaultTemplateName)
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
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

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
            settings.CurrentTemplate.GameSubSettings.PreGameMessage = null;

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

            settings.CurrentTemplateId = null;

            Context.Db.MafiaSettingsTemplates.Remove(template);

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync($"Шаблон **{name}** успешно удален", EmbedStyle.Successfull);
        }
    }



    [Group("Настройки")]
    [Alias("Н")]
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


            SetDataParameters(settings, settigsVM);

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


        [Name("Текущие настройки")]
        [Command]
        public async Task SetSettingsAsync()
        {
            const string CloseOption = "Закрыть";
            //const string CancelOption = "Отменить";

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            string? selectedBlock = null;
            string? selectedSetting = null;

            (string? displayName, object? previousValue, object? currentValue, bool isModified) = (null, null, null, false);

            var isClosed = false;
            var wasSettingsModified = false;

            IUserMessage? message = null;
            InteractiveMessageResult<MultiSelectionOption<string>?>? result = null;

            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

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

                embedBuilder.WithColor(new Color(49, 148, 146));


                if (result is not null && selectedBlock is not null)
                {
                    title = $"Блок {selectedBlock}";

                    var settingsBlockValues = settingsBlocks[selectedBlock].Keys
                        .Select(n => new MultiSelectionOption<string>(n, 1, description: $"Добавить краткое описание параметра, 50 символов"));

                    options = options.Concat(settingsBlockValues);

                    description = isModified
                    ? $"Значение **{displayName}** успешно изменено:" +
                        $" {previousValue ?? "[Н/д]"} -> {currentValue ?? "[Н/д]"}".Truncate(100)
                    : "Выберите интересующий вас параметр";


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

                                return v?.ToString()?.Truncate(300) ?? "[Н/д]";
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

                if (!result.IsSuccess)
                    continue;

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

                        previousValue = settingsDisplay.ModelValue;

                        displayName = settingsDisplay.DisplayName;


                        var vmParam = settingsDisplay.ViewModel.GetType().GetProperty(selectedSetting)
                            ?? throw new InvalidOperationException($"Property \"{selectedSetting}\" was not found in \'vmParam\'");

                        var dataParam = settingsDisplay.ModelParameter;


                        var fullDisplayName = vmParam.GetFullDisplayName();

                        var rawValue = await NextValueAsync(fullDisplayName);

                        if (rawValue is null)
                        {
                            isModified = false;

                            break;
                        }

                        var success = await TrySetParameterAsync(vmParam, settingsDisplay.ViewModel, rawValue, fullDisplayName);

                        if (success)
                        {
                            var vmValue = vmParam.GetValue(settingsDisplay.ViewModel);

                            if (vmValue?.ToString() != previousValue?.ToString())
                            {
                                SetDataParameter(dataParam, vmParam, settingsDisplay.Model, vmValue);

                                currentValue = settingsDisplay.ModelValue;

                                wasSettingsModified = currentValue?.ToString() != previousValue?.ToString();

                                if (wasSettingsModified)
                                {
                                    var check = CheckSettings(settings);

                                    if (!check.IsSuccess)
                                    {
                                        var errorEmbed = EmbedHelper.CreateEmbed(check.ErrorReason, EmbedStyle.Error, "Ошибка настроек");

                                        _ = Interactive.DelayedSendMessageAndDeleteAsync(Context.Channel,
                                        embed: errorEmbed,
                                        deleteDelay: TimeSpan.FromSeconds(15),
                                        messageReference: new(Context.Message.Id));

                                        SetDataParameter(dataParam, vmParam, settingsDisplay.Model, previousValue);

                                        currentValue = previousValue;

                                        success = false;

                                        wasSettingsModified = false;
                                    }
                                }
                            }
                            else
                            {
                                var errorEmbed = EmbedHelper.CreateEmbed("Значения совпадают", EmbedStyle.Warning);

                                _ = Interactive.DelayedSendMessageAndDeleteAsync(Context.Channel,
                                embed: errorEmbed,
                                deleteDelay: TimeSpan.FromSeconds(15),
                                messageReference: new(Context.Message.Id));

                                wasSettingsModified = false;
                            }
                        }

                        isModified = success;

                        break;

                    default:
                        throw new InvalidOperationException($"Unknown select row. Value: {result.Value.Row}");
                }
            }
            while (result is null || result.IsSuccess && !isClosed);


            if (!wasSettingsModified)
                return;

            Embed embed;

            var checkSettings = CheckSettings(settings);

            if (!checkSettings.IsSuccess)
            {
                embed = EmbedHelper.CreateEmbed(checkSettings.ErrorReason, EmbedStyle.Error, "Ошибка настроек");

                _ = Interactive.DelayedSendMessageAndDeleteAsync(Context.Channel,
                embed: embed,
                deleteDelay: TimeSpan.FromSeconds(15),
                messageReference: new(Context.Message.Id));

                return;
            }

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                embed = EmbedHelper.CreateEmbed("Настройки успешно сохранены", EmbedStyle.Successfull);
            else
                embed = EmbedHelper.CreateEmbed("Изменения не найдены", EmbedStyle.Warning);


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

        private async Task<string?> NextValueAsync(string displayName, string? description = null)
        {
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
                timeout: TimeSpan.FromSeconds(120));

            var cancelTask = Interactive.NextMessageComponentAsync(x => x.Message.Id == msg.Id && x.User.Id == Context.User.Id,
                timeout: TimeSpan.FromSeconds(125));

            var task = await Task.WhenAny(valueTask, cancelTask);

            await msg.DeleteAsync();

            if (task == cancelTask)
                return null;


            var valueMessageResult = await valueTask;

            if (!valueMessageResult.IsSuccess)
            {
                await ReplyEmbedAndDeleteAsync($"Вы не указали значение параметра **{displayName}**", EmbedStyle.Warning);

                return null;
            }

            await valueMessageResult.Value.DeleteAsync();

            if (valueMessageResult.Value is not SocketMessage valueMessage)
            {
                await ReplyEmbedAndDeleteAsync("Неверное значение параметра", EmbedStyle.Error);

                return null;
            }

            return valueMessage.Content;
        }

        private async Task<bool> TrySetParameterAsync(PropertyInfo parameter, object obj, string content, string? displayName = null)
        {
            displayName ??= parameter.GetFullDisplayName();

            if (parameter.PropertyType == typeof(bool?) || parameter.PropertyType == typeof(bool))
            {
                var result = await new BooleanTypeReader().ReadAsync(Context, content);

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
                if (parameter.PropertyType == typeof(ulong) || parameter.PropertyType == typeof(ulong?))
                {
                    if (content.Contains('<'))
                    {
                        content = Regex.Replace(content, @"[\D]", string.Empty);
                    }
                }

                var converter = TypeDescriptor.GetConverter(parameter.PropertyType);
                if (converter.IsValid(content))
                {
                    var value = converter.ConvertFrom(content);

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


        private static void SetDataParameter(PropertyInfo dataParam, PropertyInfo vmParam, object data, object? vmValue)
        {
            if (vmParam.PropertyType == typeof(bool?) || vmParam.PropertyType == typeof(bool))
            {
                if (vmValue is not null)
                    dataParam.SetValue(data, vmValue);
            }
            else if (vmParam.PropertyType == typeof(int?) || vmParam.PropertyType == typeof(int))
            {
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
                dataParam.SetValue(data, vmValue);
            }
        }

        private void SetDataParameters(object data, object viewModel)
        {
            var dataParameters = data.GetType().GetProperties().Where(p => p.CanWrite).ToDictionary(p => p.Name);
            var vmParameters = viewModel.GetType().GetProperties().Where(p => p.CanWrite);

            foreach (var vmParam in vmParameters)
            {
                if (!dataParameters.TryGetValue(vmParam.Name, out var dataParam))
                {
                    GuildLogger.Warning(LogTemplate, nameof(SetDataParameters),
                        $"View Model parameter {vmParam.Name} was not found in Data parameters");

                    continue;
                }

                if (vmParam.PropertyType != dataParam.PropertyType && Nullable.GetUnderlyingType(vmParam.PropertyType) != dataParam.PropertyType)
                {
                    var msg = $"Param {dataParam.Name}: Data param type ({dataParam.PropertyType}) is not equals to ViewModel param type ({vmParam.PropertyType})";

                    GuildLogger.Warning(LogTemplate, nameof(SetDataParameters), msg);

                    Log.Warning(LogTemplate, nameof(SetDataParameters), $"[{Context.Guild.Name} {Context.Guild.Id}] {msg}");

                    continue;
                }

                var vmValue = vmParam.GetValue(viewModel);

                SetDataParameter(dataParam, vmParam, data, vmValue);
            }
        }

        private static PreconditionResult CheckSettings(MafiaSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);


            var gameSettings = settings.CurrentTemplate.GameSubSettings;

            if (gameSettings.IsRatingGame && gameSettings.IsCustomGame)
                return PreconditionResult.FromError($"Конфликт настроек. " +
                    $"Параметры {nameof(gameSettings.IsRatingGame)} ({gameSettings.IsRatingGame}) " +
                    $"и {nameof(gameSettings.IsCustomGame)} ({gameSettings.IsCustomGame}) взаимоисключают друг друга. " +
                    $"Измените значение одного или двух параметров для устранения конфликта");

            if (gameSettings.MafiaCoefficient < 2)
                return PreconditionResult.FromError("Коэффициент мафии не может быть меньше 2");

            if (gameSettings.VoteTime < 10)
                return PreconditionResult.FromError("Время голосования не может быть меньше 10 секунд");


            var extraInfoSettings = settings.CurrentTemplate.RolesExtraInfoSubSettings;

            if (!extraInfoSettings.MurdersKnowEachOther && extraInfoSettings.MurdersVoteTogether)
                return PreconditionResult.FromError($"Конфликт настроек. " +
                    $"Параметры {nameof(extraInfoSettings.MurdersKnowEachOther)} ({extraInfoSettings.MurdersKnowEachOther}) " +
                    $"и {nameof(extraInfoSettings.MurdersVoteTogether)} ({extraInfoSettings.MurdersVoteTogether}) взаимоисключают друг друга. " +
                    $"Измените значение одного или двух параметров для устранения конфликта");

            return PreconditionResult.FromSuccess();
        }


        private class SettingsDisplay
        {
            private readonly string _propName;


            public PropertyInfo ModelParameter { get; }

            public string DisplayName { get; }
            public object Model { get; }
            public object ViewModel { get; }


            public object? ModelValue => ModelParameter.GetValue(Model);


            public SettingsDisplay(string propName, string displayName, object model, object viewModel)
            {
                DisplayName = displayName;
                Model = model;
                ViewModel = viewModel;

                _propName = propName;

                ModelParameter = Model.GetType().GetProperty(_propName) ?? throw new InvalidOperationException($"Property \"{_propName}\" was not found");
            }
        }
    }





    public class MafiaHelpModule : HelpModule
    {
        private readonly IOptionsSnapshot<GameRoleData> _conf;


        public MafiaHelpModule(InteractiveService interactiveService, IConfiguration config, IOptionsSnapshot<GameRoleData> conf) : base(interactiveService, config)
        {
            _conf = conf;
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


                if (!roleFields.TryGetValue("Name", out var name) || !roleFields.TryGetValue("Description", out var value))
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



        [RequireOwner]
        [Command("к")]
        public async Task Test()
        {
            var roles = new List<GameRole>
            {
                new Innocent((IGuildUser)Context.User, _conf),
                new Doctor((IGuildUser)Context.User, _conf, 1),
                new Sheriff((IGuildUser)Context.User, _conf, 1, Enumerable.Empty<Murder>()),
                new Murder((IGuildUser)Context.User, _conf),
                new Don((IGuildUser)Context.User, _conf, Enumerable.Empty<Sheriff>()),
                new Hooker((IGuildUser)Context.User, _conf),
                new Maniac((IGuildUser)Context.User, _conf)
            };

            var embeds = new List<Embed>();

            foreach (var role in roles)
                embeds.Add(MafiaHelper.GetEmbed(role, Config));

            await ReplyAsync(embeds: embeds.ToArray());
        }
    }
}