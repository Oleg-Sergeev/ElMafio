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
using Core.Common.Data;
using Core.Exceptions;
using Core.Extensions;
using Core.Resources;
using Core.TypeReaders;
using Core.ViewModels;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Infrastructure.Data.Entities.Games.Settings.Mafia;
using Infrastructure.Data.Entities.Games.Stats;
using Infrastructure.Data.Entities.ServerInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Common.MultiSelect;
using Modules.Common.Preconditions.Commands;
using Modules.Games.Mafia.Common;
using Modules.Games.Mafia.Common.Data;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.Services;
using Modules.Games.Services;
using Serilog;

namespace Modules.Games.Mafia;

[Group("Мафия")]
[Alias("м")]
[Remarks("Перед началом настройки и игры в Мафии рекомендуется прочесть руководство по настройке игры" +
    "\nДля этого введите команду `Руководство.Мафия`")]
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
        => new("Мафия", 3, host);


    [RequireBotPermission(GuildPermission.ManageChannels)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [Remarks("Для того, чтобы бот смог гарантированно ограничить доступ к каналам у всех игроко, рекомендуется выдать право Администратор" +
        "\nВ противном случае **не гарантируется** полный контроль над правами игроков, что может повлечь за собой нечестную игру")]
    public override async Task StartAsync()
    {
        var task = ReplyEmbedAsync("Проверка корректности настроек...", EmbedStyle.Waiting);

        var check = await CheckPreconditionsAsync();
        if (!check.IsSuccess)
        {
            await ReplyEmbedAsync(check.ErrorReason, EmbedStyle.Error);

            return;
        }

        var data = GetGameData();
        data.IsPlaying = true;
        data.Players.Shuffle(3);
        data.RefreshToken();

        await task;

        await ReplyEmbedAsync("Настройки корректны", EmbedStyle.Successfull);

        var server = await Context.GetServerAsync();

        var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

        try
        {
            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            if (settings.CurrentTemplate.ServerSubSettings.MentionPlayersOnGameStart)
                await MentionPlayersAsync();

            var context = await CreateMafiaContextAsync(settings, data);

            var game = new MafiaGame(context, _mafiaService);

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

            _ = ShowChronologyAsync(context.RolesData.AllRoles.Keys, chronology);


            await ReplyEmbedStampAsync($"{data.Name} успешно завершена", EmbedStyle.Successfull);

            if (updateStatsTask is not null)
            {
                await updateStatsTask;

                await ReplyEmbedAsync("Статистика успешно обновлена", EmbedStyle.Successfull);
            }

            if (settings.DisbandPartyAfterGameEnd)
                DeleteGameData();
            else
                data.IsPlaying = false;
        }
        catch (OperationCanceledException)
        {
            await ReplyEmbedAsync("Игра была остановлена вручную", EmbedStyle.Warning);

            data.IsPlaying = false;
        }
        catch (GameSetupAbortedException e)
        {
            var msg = server.DebugMode switch
            {
                DebugMode.ErrorMessages => $"{e.Message}\n{e.InnerException?.Message}",
                DebugMode.StackTrace => e.ToString(),
                _ => null
            };

            await ReplyEmbedAsync($"**Игра была аварийно прервана:**\n{msg}", EmbedStyle.Error, "Ошибка настроек");

            data.IsPlaying = false;
        }
        catch (Exception e)
        {
            var msg = server.DebugMode switch
            {
                DebugMode.ErrorMessages => e.Message,
                DebugMode.StackTrace => e.ToString(),
                _ => null
            };
            await ReplyEmbedAsync($"**Игра была аварийно прервана:**\n{msg}", EmbedStyle.Error, "Ошибка во время игры");

            if (settings.DisbandPartyAfterGameEnd)
                DeleteGameData();
            else
                data.IsPlaying = false;
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

        if (settings.ClearChannelsOnStart && !Bot.HasGuildPermission(GuildPermission.ManageMessages))
            return PreconditionResult.FromError($"Для очистки сообщений необходимо право {GuildPermission.ManageMessages}");

        if ((settings.GeneralVoiceChannelId is not null || settings.MurdersVoiceChannelId is not null) && !Bot.HasGuildPermission(GuildPermission.MoveMembers))
            return PreconditionResult.FromError($"Для корректной работы с голосовыми каналами необходимо право {GuildPermission.MoveMembers}");

        if (settings.CurrentTemplate.ServerSubSettings.RenameUsers && !Bot.HasGuildPermission(GuildPermission.ManageNicknames))
            return PreconditionResult.FromError($"Для возможности менять никнеймы необходимо право {GuildPermission.ManageNicknames}");


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
        var category = await Context.Guild.GetCategoryChannelOrCreateAsync(settings.CategoryChannelId ?? 0, "Мафия");

        settings.CategoryChannelId = category.Id;

        var _guildData = new MafiaGuildData(
               await Context.Guild.GetTextChannelOrCreateAsync(settings.GeneralTextChannelId, "мафия-общий", SetCategoryChannel),
               await Context.Guild.GetTextChannelOrCreateAsync(settings.MurdersTextChannelId, "мафия-убийцы", SetCategoryChannel),
               Context.Guild.GetTextChannel(settings.SpectatorsTextChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.GeneralVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.MurdersVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(settings.SpectatorsVoiceChannelId ?? 0),
               await Context.Guild.GetRoleOrCreateAsync(settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
               Context.Guild.GetRole(settings.WatcherRoleId ?? 0));

        settings.GeneralTextChannelId = _guildData.GeneralTextChannel.Id;
        settings.MurdersTextChannelId = _guildData.MurderTextChannel.Id;
        settings.MafiaRoleId = _guildData.MafiaRole.Id;

        await Context.Db.SaveChangesAsync();

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
                    new(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new(Bot.Id, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow))
                };

            props.PermissionOverwrites = overwrites;
        }
    }


    private async Task<int> UpdateStatsAsync(IEnumerable<GameRole> roles, Winner winner)
    {
        var rolesDict = roles.ToDictionary(p => p.Player.Id);

        var stats = await GetStatsWithAddingNewAsync(rolesDict.Keys);

        foreach (var role in rolesDict.Values)
            role.UpdateStats(stats[role.Player.Id], winner);

        return await Context.Db.SaveChangesAsync();
    }


    private async Task ShowChronologyAsync(IEnumerable<IGuildUser> players, MafiaChronology chronology)
    {
        try
        {
            var entryEmbed = EmbedHelper.CreateEmbed("Просмотреть хронологию игры");

            var entryComponent = new ComponentBuilder()
                .WithButton("Открыть", "showChronology")
                .Build();

            var entryMsg = await ReplyAsync(embed: entryEmbed, components: entryComponent);


            var paginator = chronology.BuildActionsHistoryPaginator(players);


            var timeout = TimeSpan.FromMinutes(30);

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
                        GuildLogger.Error(e, LogTemplate, nameof(ShowChronologyAsync),
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
            GuildLogger.Error(e, LogTemplate, nameof(ShowChronologyAsync),
                "Error occured when show chronology");

            await ReplyEmbedAsync("Произошла ошибка во время показа хронологии", EmbedStyle.Error);
        }
    }


    public class MafiaStatsModule : GameStatsModule
    {
        public MafiaStatsModule(InteractiveService interactiveService) : base(interactiveService)
        {
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




        public class MafiaAdminModule : GameAdminModule
        {
            public MafiaAdminModule(InteractiveService interactiveService) : base(interactiveService)
            {
            }
        }
    }


    public class MafiaRatingModule : RatingModule
    {
        public MafiaRatingModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }

        protected override IOrderedQueryable<MafiaStats> GetRatingQuery()
            => base.GetRatingQuery().ThenByDescending(stat => stat.BlacksWinRate);




        [Group]
        [RequireConfirmAction(false)]
        [RequireStandartAccessLevel(StandartAccessLevel.Administrator, Group = "perm")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
        [RequireOwner(Group = "perm")]
        public class ScoresModule : CommandGuildModuleBase
        {
            public ScoresModule(InteractiveService interactiveService) : base(interactiveService)
            {
            }



            [Command("ДопОчки+")]
            [Alias("ДО+")]
            [Summary("Добавить дополнительные очки")]
            public async Task AddExtraScoresAsync([Summary("Кол-во добавляемых доп. очков")] float scores, [Summary("Игрок")] IUser? user = null)
            {
                if (!await TryUpdateScoresAsync(scores, user, true))
                    return;

                await ReplyEmbedStampAsync("Дополнительные очки успешно начислены", EmbedStyle.Successfull);
            }

            [Command("ДопОчки-")]
            [Alias("ДО-")]
            [Summary("Убрать дополнительные очки")]
            public async Task RemoveExtraScoresAsync([Summary("Кол-во убавляемых доп. очков")] float scores, [Summary("Игрок")] IUser? user = null)
            {
                if (!await TryUpdateScoresAsync(-scores, user, true))
                    return;

                await ReplyEmbedStampAsync("Дополнительные очки успешно списаны", EmbedStyle.Successfull);
            }

            [Command("ДопОчкиСброс")]
            [Alias("ДОС")]
            [Summary("Сбросить дополнительные очки")]
            public async Task ResetExtraScoresAsync([Summary("Игрок")] IUser? user = null)
            {
                if (!await TryUpdateScoresAsync(null, user, true))
                    return;

                await ReplyEmbedStampAsync("Дополнительные очки успешно сброшены", EmbedStyle.Successfull);
            }


            [Command("ШтрафОчки+")]
            [Alias("ШО+")]
            [Summary("Добавить штрафные очки")]
            public async Task AddPenaltyScoresAsync([Summary("Кол-во добалвяемых штрафных очков")] float scores, [Summary("Игрок")] IUser? user = null)
            {
                if (!await TryUpdateScoresAsync(scores, user, false))
                    return;

                await ReplyEmbedStampAsync("Штрафные очки успешно начислены", EmbedStyle.Successfull);
            }

            [Command("ШтрафОчки-")]
            [Alias("ШО-")]
            [Summary("Убрать штрафные очки")]
            public async Task RemovePenaltyScoresAsync([Summary("Кол-во убавляемых штрафных очков")] float scores, [Summary("Игрок")] IUser? user = null)
            {
                if (!await TryUpdateScoresAsync(-scores, user, false))
                    return;

                await ReplyEmbedStampAsync("Штрафные очки успешно списаны", EmbedStyle.Successfull);
            }

            [Command("ШтрафОчкиСброс")]
            [Alias("ШОС")]
            [Summary("Сбросить штрафные очки")]
            public async Task ResetPenaltyScoresAsync([Summary("Игрок")] IUser? user = null)
            {
                if (!await TryUpdateScoresAsync(null, user, false))
                    return;

                await ReplyEmbedStampAsync("Штрафные очки успешно сброшены", EmbedStyle.Successfull);
            }


            private async Task<bool> TryUpdateScoresAsync(float? scores, IUser? user, bool extra)
            {
                user ??= Context.User;

                var userStat = await Context.Db.MafiaStats
                    .FirstOrDefaultAsync(ms => ms.ServerId == Context.Guild.Id && ms.UserId == user.Id);

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


    [Group("Шаблоны")]
    [Alias("Ш")]
    [Summary("Раздел для управления шаблонами: добавление, удаление, изменение имени и прочее")]
    [RequireStandartAccessLevel(StandartAccessLevel.Moderator, Group = "perm")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    public class TemplatesModule : CommandGuildModuleBase
    {
        private readonly IGameSettingsService<MafiaSettings> _settingsService;

        public TemplatesModule(InteractiveService interactiveService, IGameSettingsService<MafiaSettings> settingsService) : base(interactiveService)
        {
            _settingsService = settingsService;
        }



        [Command("Клонировать")]
        [Alias("клон", "к")]
        [Summary("Создать шаблон на основе активного шаблона")]
        [Remarks("Склонированный шаблон автоматически становится активным")]
        public async Task CloneTemplate([Summary("Имя нового шаблона")][Remainder] string newTemplateName)
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
                ServerSubSettings = settings.CurrentTemplate.ServerSubSettings with { Id = 0, MafiaSettingsTemplateId = 0 },
                GameSubSettings = settings.CurrentTemplate.GameSubSettings with { Id = 0, MafiaSettingsTemplateId = 0 },
                RoleAmountSubSettings = settings.CurrentTemplate.RoleAmountSubSettings with { Id = 0, MafiaSettingsTemplateId = 0 },
                RolesExtraInfoSubSettings = settings.CurrentTemplate.RolesExtraInfoSubSettings with { Id = 0, MafiaSettingsTemplateId = 0 }
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
        [Summary("Загрузить указанный шаблон и сделать его активным")]
        [Remarks("По умолчанию загружается стандартный шаблон, если такой существует" +
            "\nЗагруженный шаблон автоматически становится активным")]
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
        [Summary("Показать имя активного шаблона")]
        public async Task ShowCurrentTemplate()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

            ArgumentNullException.ThrowIfNull(settings.CurrentTemplate);

            await ReplyEmbedAsync($"Текущий шаблон - `{settings.CurrentTemplate.Name}`");
        }


        [Command("Список")]
        [Alias("сп")]
        [Summary("Показать список всех шаблонов")]
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
        [Summary("Изменить имя активного шаблона")]
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
        [Summary("Сбросить настройки активного шаблона до стандартных")]
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
        [Summary("Удалить указанный шаблон")]
        [Remarks("Удалить активный шаблон невозможно")]
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
    [RequireStandartAccessLevel(StandartAccessLevel.Moderator, Group = "perm")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    [Summary("Настройки для мафии включают в себя настройки сервера(используемые роли, каналы и категорию каналов) и настройки самой игры. " +
        "Для подробностей введите команду `Мафия.Настройки.Помощь`")]
    public class SettingsModule : CommandGuildModuleBase
    {
        private readonly IGameSettingsService<MafiaSettings> _settingsService;

        public SettingsModule(InteractiveService interactiveService, IGameSettingsService<MafiaSettings> settingsService) : base(interactiveService)
        {
            _settingsService = settingsService;
        }


        [Command("Автонастройка")]
        [Alias("ан")]
        [Summary("Автоматическое создание всех каналов и ролей")]
        [Remarks("Что добавит бот:" +
            "\n 2 роли: `Игрок мафии` и `Наблюдатель мафии`" +
            "\n 1 категория каналов `Мафия`" +
            "\n 3 текстовых канала: `Мафия-общий` `Мафия-убийцы` `Мафия-наблюдатели`" +
            "\n 3 голосовых канала: `Мафия-общий` `Мафия-убийцы` `Мафия-наблюдатели`")]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
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
                    new(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new(Bot.Id, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow))
                };

                props.PermissionOverwrites = overwrites;
            }
        }


        [Command("Сброс")]
        [Summary("Сброс и удаление всех каналов и ролей, используемых для игры")]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
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
        [Summary("Проверка показывает, какие каналы/роли не установлены, или установлены некорректно")]
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
        [Summary("Основное меню для работы со всеми настройками мафии")]
        [Remarks("Для понимания как работать с настройками, введите команду `Руководство.Мафия`")]
        [Priority(-2)]
        public async Task SetSettingsAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

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

            var culture = CultureInfo.GetCultureInfo("ru");


            var settingsBlocks = new Dictionary<string, Dictionary<string, SettingsDisplay>>()
            {
                {Resource.GeneralBlock, CreateSettingsDisplays(settings, new MafiaSettingsViewModel())},
                {Resource.GameBlock, CreateSettingsDisplays(current.GameSubSettings, new GameSubSettingsViewModel())},
                {Resource.ServerBlock, CreateSettingsDisplays(current.ServerSubSettings, new ServerSubSettingsViewModel())},
                {Resource.RolesAmountBlock, CreateSettingsDisplays(current.RoleAmountSubSettings, new RoleAmountSubSettingsViewModel())},
                {Resource.RolesExtraInfoBlock, CreateSettingsDisplays(current.RolesExtraInfoSubSettings, new RolesInfoSubSettingsViewModel())}
            };

            var n_a = Resource.NotAssigned;

            var closeOption = Resource.Close;

            var embedBuilder = new EmbedBuilder()
                .WithColor(new Color(49, 148, 146));

            do
            {
                var title = Resource.Settings;
                var description = Resource.MafiaSettingsBlockSelect;

                var options = settingsBlocks.Keys
                    .Select(k => new MultiSelectionOption<string>(k, 0, selectedBlock == k));

                embedBuilder.Fields.Clear();

                if (result is not null && selectedBlock is not null)
                {
                    title = $"{Resource.Block} {selectedBlock}";

                    var settingsBlockValues = settingsBlocks[selectedBlock].Keys
                        .Select(n => new MultiSelectionOption<string>(n, 1,
                        description: Resource.ResourceManager.GetString($"{n}_Description", culture)?.Truncate(100) ?? Resource.NoDescription));

                    options = options.Concat(settingsBlockValues);

                    description = isModified
                        ? $"{Resource.ValueChanged.Replace("{0}", displayName)} " +
                        $"{previousValue ?? n_a} -> {currentValue ?? n_a}".Truncate(100)
                    : Resource.ParameterSelect;


                    var displayNames = settingsBlocks[selectedBlock].Values.Select(x => x.DisplayName);

                    var values = settingsBlocks[selectedBlock].Values.Select(x => x.ModelValue);

                    var fields = new List<EmbedFieldBuilder>()
                    {
                        new()
                        {
                            Name = Resource.Parameter,
                            Value = string.Join('\n', displayNames),
                            IsInline = true
                        },
                        new()
                        {
                            Name = Resource.Value,
                            Value = string.Join('\n', values.Select(v =>
                            {
                                if (ulong.TryParse(v?.ToString(), out var id))
                                    return Context.Guild.GetMentionFromId(id);

                                return v?.ToString()?.Truncate(900) ?? n_a;
                            })),
                            IsInline = true
                        }
                    };

                    embedBuilder.WithFields(fields);
                }

                title += $" (Шаблон `{settings.CurrentTemplate.Name}`)";

                embedBuilder.WithTitle(title);

                if (description is not null)
                    embedBuilder.WithDescription(description);


                var pageBuilder = PageBuilder.FromEmbedBuilder(embedBuilder);


                var multiSelection = new MultiSelectionBuilder<string>()
                    .AddUser(Context.User)
                    .WithOptions(options.ToArray())
                    .WithCancelButton(closeOption)
                    .WithStringConverter(s =>
                    {
                        if (s.Option != closeOption && !settingsBlocks.TryGetValue(s.Option, out _) && selectedBlock is not null)
                            return settingsBlocks[selectedBlock][s.Option].DisplayName;

                        return s.ToString() ?? n_a;
                    })
                    .WithSelectionPage(pageBuilder)
                    .WithActionOnCancellation(ActionOnStop.DeleteMessage)
                    .WithActionOnTimeout(ActionOnStop.DeleteMessage)
                    .Build();


                result = message is null
                ? await Interactive.SendSelectionAsync(multiSelection, Context.Channel, TimeSpan.FromMinutes(10), null, cts.Token)
                : await Interactive.SendSelectionAsync(multiSelection, message, TimeSpan.FromMinutes(10), null, cts.Token);

                message = result.Message;

                if (!result.IsSuccess)
                    continue;

                if (result.Value.Option == closeOption)
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
                                        var errorEmbed = EmbedHelper.CreateEmbed(check.ErrorReason, EmbedStyle.Error, Resource.SettingsError);

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
                                var errorEmbed = EmbedHelper.CreateEmbed(Resource.ValuesMatch, EmbedStyle.Warning);

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
                embed = EmbedHelper.CreateEmbed(checkSettings.ErrorReason, EmbedStyle.Error, Resource.SettingsError);

                _ = Interactive.DelayedSendMessageAndDeleteAsync(Context.Channel,
                embed: embed,
                deleteDelay: TimeSpan.FromSeconds(15),
                messageReference: new(Context.Message.Id));

                return;
            }

            var n = await Context.Db.SaveChangesAsync();

            if (n > 0)
                embed = EmbedHelper.CreateEmbed(Resource.SettingsSuccessfullySaved, EmbedStyle.Successfull);
            else
                embed = EmbedHelper.CreateEmbed(Resource.ChangesNotFound, EmbedStyle.Warning);


            _ = Interactive.DelayedSendMessageAndDeleteAsync(Context.Channel,
                embed: embed,
                deleteDelay: TimeSpan.FromSeconds(15),
                messageReference: new(Context.Message.Id));




            Dictionary<string, SettingsDisplay> CreateSettingsDisplays(object model, object viewModel)
                => viewModel
                .GetType()
                .GetProperties()
                .ToDictionary(
                    p => p.Name,
                    p => new SettingsDisplay(p.Name, Resource.ResourceManager.GetString($"{p.Name}_Name", culture) ?? p.GetShortDisplayName(), model, viewModel));
        }


        [Command("Преамбула")]
        [Summary("Показать преамбулу - сообщение, выводимое перед началом игры")]
        [Remarks("Для настройки преамбулы вызовите команду `Мафия.Настройки`")]
        public async Task ShowPreGameMessageAsync()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            var preGameMessage = settings.CurrentTemplate?.GameSubSettings.PreGameMessage;

            if (preGameMessage is null)
            {
                await ReplyEmbedAsync("*Преамбула отсутствует*", EmbedStyle.Warning);

                return;
            }

            await ReplyEmbedAsync(preGameMessage, "Преамбула");
        }



        private async Task<string?> NextValueAsync(string displayName, string? description = null)
        {
            var cancelComponent = new ComponentBuilder()
                .WithButton(Resource.Cancel, "cancel", ButtonStyle.Danger)
                .Build();

            var embed = description is null
                ? EmbedHelper.CreateEmbed(Resource.SpecifyValue.Replace("{0}", displayName))
                : EmbedHelper.CreateEmbed(description, Resource.SpecifyValue.Replace("{0}", displayName));

            var data = new MessageData()
            {
                Embed = embed,
                MessageReference = new(Context.Message.Id),
                MessageComponent = cancelComponent
            };


            var msg = await ReplyAsync(data);


            var valueTask = Interactive.NextMessageAsync(x => x.Channel.Id == msg.Channel.Id && x.Author.Id == Context.User.Id,
                timeout: TimeSpan.FromMinutes(5));

            var cancelTask = Interactive.NextMessageComponentAsync(x => x.Message.Id == msg.Id && x.User.Id == Context.User.Id,
                timeout: TimeSpan.FromMinutes(6));

            var task = await Task.WhenAny(valueTask, cancelTask);

            await msg.DeleteAsync();

            if (task == cancelTask)
                return null;


            var valueMessageResult = await valueTask;

            if (!valueMessageResult.IsSuccess)
            {
                await ReplyEmbedAndDeleteAsync(Resource.ValueNotSpecified.Replace("{0}", displayName), EmbedStyle.Warning);

                return null;
            }

            await valueMessageResult.Value.DeleteAsync();

            if (valueMessageResult.Value is not SocketMessage valueMessage)
            {
                await ReplyEmbedAndDeleteAsync(Resource.InvalidParameterValue, EmbedStyle.Error);

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
                    await ReplyEmbedAndDeleteAsync(Resource.FailedSetParameterValue.Replace("{0}", displayName), EmbedStyle.Error);

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
                    await ReplyEmbedAndDeleteAsync(Resource.FailedSetParameterValue.Replace("{0}", displayName), EmbedStyle.Error);

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
                return PreconditionResult.FromError($"{Resource.SettingsConflict}." +
                    $"{Resource.TheOptionsMutuallyExclusive.Replace("{0}", nameof(gameSettings.IsRatingGame)).Replace("{1}", nameof(gameSettings.IsCustomGame))}. " +
                    $"{Resource.ChangeParameterValueForConflictSolve}");

            if (gameSettings.MafiaCoefficient < 2)
                return PreconditionResult.FromError(Resource.MafiaCoefError.Replace("{0}", "2"));

            if (gameSettings.VoteTime < 10)
                return PreconditionResult.FromError(Resource.VoteTimeError.Replace("{0}", "10"));


            var extraInfoSettings = settings.CurrentTemplate.RolesExtraInfoSubSettings;

            if (!extraInfoSettings.MurdersKnowEachOther && extraInfoSettings.MurdersVoteTogether)
                return PreconditionResult.FromError($"{Resource.SettingsConflict}." +
                    $"{Resource.TheOptionsMutuallyExclusive.Replace("{0}", nameof(extraInfoSettings.MurdersKnowEachOther)).Replace("{1}", nameof(extraInfoSettings.MurdersVoteTogether))}. " +
                    $"{Resource.ChangeParameterValueForConflictSolve}");

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
        public MafiaHelpModule(InteractiveService interactiveService, IConfiguration config) : base(interactiveService, config)
        {

        }


        [Command("Роли")]
        [Summary("Получить подробное описание каждой роли")]
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

            var orderedPageBuilders = new List<(int order, IPageBuilder pageBuilder)>();

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

                orderedPageBuilders.Add((Convert.ToInt32(roleFields.GetValueOrDefault("Order")), pageBuilder));
            }


            paginatorBuilder.WithPages(orderedPageBuilders.OrderBy(x => x.order).Select(x => x.pageBuilder));

            if (!sendToServer)
                await Interactive.SendPaginatorAsync(paginatorBuilder.Build(), await Context.User.CreateDMChannelAsync(), TimeSpan.FromMinutes(15));
            else
                await Interactive.SendPaginatorAsync(paginatorBuilder.Build(), Context.Channel, TimeSpan.FromMinutes(15));
        }
    }
}