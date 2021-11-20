﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Exceptions;
using Core.Extensions;
using Core.Interfaces;
using Core.TypeReaders;
using Core.ViewModels;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Infrastructure.Data.Models.Games.Settings.Mafia;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Modules.Games.Mafia.Common.GameRoles;
using Modules.Games.Mafia.Common.GameRoles.Data;
using Modules.Games.Mafia.Common.GameRoles.RolesGroups;
using Modules.Games.Mafia.Common.Interfaces;
using Serilog;

namespace Modules.Games.Mafia;

[Group("Мафия")]
[Alias("маф", "м")]
public class MafiaModule : GameModule
{
    protected const string LogTemplate = "({Context:l}): {Message}";


    private bool CanContinueGame => _settings.GameSubSettings.ConditionAliveAtLeast1Innocent
        ? _mafiaData!.Murders.Values.Any(m => m.IsAlive) && _mafiaData.Innocents.Values.Any(i => i.IsAlive)
        : _mafiaData!.Murders.Values.Any(m => m.IsAlive) && _mafiaData.AlivePlayers.Count > 2 * _mafiaData.Murders.Values.Count(m => m.IsAlive);


    protected ILogger? GuildLogger { get; private set; }

    protected IOptionsMonitor<GameRoleData> GameRoleOptions { get; }

    private MafiaData? _mafiaData;

    private MafiaSettings _settings = null!;


    private OverwritePermissions _allowWrite;
    private OverwritePermissions _denyWrite;
    private OverwritePermissions _allowSpeak;
    private OverwritePermissions _denySpeak;



    public MafiaModule(IConfiguration config, IRandomService random, IOptionsMonitor<GameRoleData> options) : base(config, random)
    {
        GameRoleOptions = options;
    }



    [Group("Настройки")]
    [Alias("н")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Настройки для мафии включают в себя настройки сервера(используемые роли, каналы и категорию каналов) и настройки самой игры. " +
        "Для подробностей введите команду **Мафия.Настройки.Помощь**")]
    public class SettingsModule : GuildModuleBase
    {
        [Command("Роли")]
        [Alias("р")]
        public async Task SetRoleAmountSettingsAsync()
        {
            var settingsVM = new RoleAmountSubSettingsViewModel();

            var success = await TrySetParameters(settingsVM);

            if (!success)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, "Настройки ролей не были сохранены");

                return;
            }

            var mafiaSettings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var settings = mafiaSettings.RoleAmountSubSettings;
            mafiaSettings.RoleAmountSubSettings = settings with
            {
                DoctorsCount = (settingsVM?.DoctorsCount <= -1) ? null : (settingsVM?.DoctorsCount ?? settings.DoctorsCount),
                SheriffsCount = (settingsVM?.SheriffsCount <= -1) ? null : (settingsVM?.SheriffsCount ?? settings.SheriffsCount),
                MurdersCount = (settingsVM?.MurdersCount <= -1) ? null : (settingsVM?.MurdersCount ?? settings.MurdersCount),
                DonsCount = (settingsVM?.DonsCount <= -1) ? null : (settingsVM?.DonsCount ?? settings.DonsCount),
                InnocentCount = (settingsVM?.InnocentCount <= -1) ? null : (settingsVM?.InnocentCount ?? settings.InnocentCount)
            };


            if (mafiaSettings.RoleAmountSubSettings.MurdersCount == 0 && mafiaSettings.RoleAmountSubSettings.DonsCount == 0)
            {
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Error, "Для игры необходима хотя бы одна черная роль");

                return;
            }


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAndDeleteAsync(EmbedStyle.Successfull, "Настройки ролей успешно сохранены");
        }

        [Command("РолиДействия")]
        [Alias("РДействия", "рд")]
        public async Task SetRolesInfoSubSettingsAsync()
        {
            var settingsVM = new RolesInfoSubSettingsViewModel();

            var success = await TrySetParameters(settingsVM);

            if (!success)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, "Дополнительные настройки ролей не были сохранены");

                return;
            }

            var mafiaSettings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var settings = mafiaSettings.RolesInfoSubSettings;
            mafiaSettings.RolesInfoSubSettings = settings with
            {
                DoctorSelfHealsCount = (settingsVM?.DoctorSelfHealsCount <= -1) ? null : (settingsVM?.DoctorSelfHealsCount ?? settings.DoctorSelfHealsCount),
                SheriffShotsCount = (settingsVM?.SheriffShotsCount <= -1) ? null : (settingsVM?.SheriffShotsCount ?? settings.SheriffShotsCount),
                MurdersVoteTogether = settingsVM?.MurdersVoteTogether ?? settings.MurdersVoteTogether,
                MurdersMustVoteForOnePlayer = settingsVM?.MurdersMustVoteForOnePlayer ?? settings.MurdersMustVoteForOnePlayer,
                MurdersKnowEachOther = settingsVM?.MurdersKnowEachOther ?? settings.MurdersKnowEachOther,
                CanInnocentsKillAtNight = settingsVM?.CanInnocentsKillAtNight ?? settings.CanInnocentsKillAtNight,
                InnocentsMustVoteForOnePlayer = settingsVM?.InnocentsMustVoteForOnePlayer ?? settings.InnocentsMustVoteForOnePlayer
            };


            var MurdersKnowEachOther = mafiaSettings.RolesInfoSubSettings.MurdersKnowEachOther;
            var MurdersVoteTogether = mafiaSettings.RolesInfoSubSettings.MurdersVoteTogether;

            if (!MurdersKnowEachOther && MurdersVoteTogether)
            {
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Error, $"Конфликт настроек. " +
                    $"Параметры {nameof(MurdersKnowEachOther)} ({MurdersKnowEachOther}) и {nameof(MurdersVoteTogether)} ({MurdersVoteTogether}) взаимоисключают друг друга. " +
                    $"Измените значение одного или двух параметров для устранения конфликта");

                return;
            }


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAndDeleteAsync(EmbedStyle.Successfull, "Дополнительные настройки ролей успешно сохранены");
        }

        [Command("Сервер")]
        [Alias("серв", "с")]
        public async Task SetServerSubSettingsAsync()
        {
            var serverSettingsVM = new ServerSubSettingsViewModel();

            var success = await TrySetParameters(serverSettingsVM);


            if (!success)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, "Серверные настройки не были сохранены");

                return;
            }

            var mafiaSettings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var settings = mafiaSettings.ServerSubSettings;
            mafiaSettings.ServerSubSettings = settings with
            {
                AbortGameWhenError = serverSettingsVM?.AbortGameWhenError ?? settings.AbortGameWhenError,
                SendWelcomeMessage = serverSettingsVM?.SendWelcomeMessage ?? settings.SendWelcomeMessage,
                RenameUsers = serverSettingsVM?.RenameUsers ?? settings.RenameUsers,
                RemoveRolesFromUsers = serverSettingsVM?.RemoveRolesFromUsers ?? settings.RemoveRolesFromUsers,
                ReplyMessagesOnSetupError = serverSettingsVM?.ReplyMessagesOnSetupError ?? settings.ReplyMessagesOnSetupError
            };

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Настройки сервера успешно сохранены");
        }

        [Command("Игра")]
        [Alias("и")]
        public async Task SetGameSubSettingsAsync()
        {
            var settingsVM = new GameSubSettingsViewModel();

            var success = await TrySetParameters(settingsVM);


            if (!success)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, "Настройки игры не были сохранены");

                return;
            }

            var mafiaSettings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var settings = mafiaSettings.GameSubSettings;
            mafiaSettings.GameSubSettings = settings with
            {
                IsRatingGame = settingsVM?.IsRatingGame ?? settings.IsRatingGame,
                MafiaCoefficient = (settingsVM?.MafiaCoefficient <= -1) ? 3 : (settingsVM?.MafiaCoefficient ?? settings.MafiaCoefficient),
                IsCustomGame = settingsVM?.IsCustomGame ?? settings.IsCustomGame,
                ConditionAliveAtLeast1Innocent = settingsVM?.ConditionAliveAtLeast1Innocent ?? settings.ConditionAliveAtLeast1Innocent,
                LastWordNightCount = (settingsVM?.LastWordNightCount <= -1) ? 0 : (settingsVM?.LastWordNightCount ?? settings.LastWordNightCount)
            };

            if (mafiaSettings.GameSubSettings.MafiaCoefficient <= 1)
            {
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, "Коэффиент мафии не может быть меньше 2. Установлено стандартное значение **3**");

                mafiaSettings.GameSubSettings = settings with
                {
                    MafiaCoefficient = 3
                };
            }

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Настройки сервера успешно сохранены");
        }


        [Priority(-1)]
        [Command("Каналы")]
        [Summary("Настроить используемые каналы для игры")]
        [Remarks("Для указания ссылки на голосовой канал, используйте следующий шаблон: <#VoiceChannelId>")]
        public async Task SetSettingsAsync(
            [Summary("Главный канал игры, в котором происходит дневное голосование и объявление результатов прошедшей ночи")] ITextChannel generalTextChannel,
            [Summary("Канал для убийц, в котором обсуждаются планы на следующую жертву и непосредственно само голосование")] ITextChannel murderTextChannel,
            [Summary("Текстовый канал для наблюдателей")] ITextChannel? watcherTextChannel = null,
            [Summary("Голосовой канал для всех игроков, доступен только днем во время обсуждения")] IVoiceChannel? generalVoiceChannel = null,
            [Summary("Голосовой канал для убийц, доступен только ночью")] IVoiceChannel? murderVoiceChannel = null,
            [Summary("Голосовой канал для наблюдателей")] IVoiceChannel? watcherVoiceChannel = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();


            settings.ServerSubSettings = settings.ServerSubSettings with
            {
                GeneralTextChannelId = generalTextChannel.Id,
                MurdersTextChannelId = murderTextChannel.Id,
                WatchersTextChannelId = watcherTextChannel?.Id,
                GeneralVoiceChannelId = generalVoiceChannel?.Id,
                MurdersVoiceChannelId = murderVoiceChannel?.Id,
                WatchersVoiceChannelId = watcherVoiceChannel?.Id
            };


            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Каналы успешно сохранены");
        }


        [Priority(-1)]
        [Command("Роли")]
        [Summary("Настроить параметры игры")]
        [Remarks("Чтобы указать нужную роль, введите **@** и выберите необходимую роль")]
        public async Task SetSettingsAsync(
            [Summary("Роль, выдываваемая каждому игроку мафии")] IRole mafiaRole,
            [Summary("Роль, выдываваемая игроку, убитому во время игры, чтобы он мог продолжать наблюдать за игрой")] IRole watcherRole)
        {

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.ServerSubSettings = settings.ServerSubSettings with
            {
                MafiaRoleId = mafiaRole.Id,
                WatcherRoleId = watcherRole.Id
            };

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Роли успешно сохранены");
        }



        [Command("Категория")]
        [Summary("Настроить параметры игры")]
        [Remarks("Чтобы ввести ID категории канала, нажмите по нужной категории правой кнопкой мыши (ПК), или зажмите пальцем канал (Моб.), " +
                 "а затем нажмите кнопку **Скопировать ID**")]
        public async Task SetSettingsAsync([Summary("Категория каналов, в которой будут находиться игровые каналы")] ICategoryChannel categoryChannel)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.ServerSubSettings = settings.ServerSubSettings with
            {
                CategoryChannelId = categoryChannel.Id
            };


            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync(EmbedStyle.Successfull, "Категория успешно установлена");
        }




        [Command("Текущие")]
        [Alias("тек", "т")]
        [Summary("Показать все текущие настройки мафии для этого сервера")]
        public async Task ShowAllSettingsAsync()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            await ShowSettingsAsync(settings.ServerSubSettings, "Настройки сервера");
            await ShowSettingsAsync(settings.GameSubSettings, "Настройки игры");
            await ShowSettingsAsync(settings.RoleAmountSubSettings, "Настройки количества ролей");
            await ShowSettingsAsync(settings.RolesInfoSubSettings, "Настройки действий ролей");
        }


        private async Task ShowSettingsAsync<T>(T settings, string title) where T : notnull
        {
            var parameters = settings.GetType().GetProperties().ToList();

            var fields = parameters.Select(p => new EmbedFieldBuilder()
            {
                Name = p.GetPropertyFullName(),
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


        private async Task<bool> TrySetParameters<T>(T settings) where T : notnull
        {
            var wasSetSettings = false;


            var parameters = settings.GetType().GetProperties().Where(p => p.CanWrite).ToList();

            var parameterNames = GetPropertiesName(parameters).ToList();

            var emotes = GetEmotesList(parameterNames.Count, parameterNames, out var text);


            while (true)
            {
                var message = await ReplyEmbedAsync(EmbedStyle.Information, $"Выберите интересующий вас параметр\n{text}");


                var selectedEmote = await NextReactionAsync(message, TimeSpan.FromSeconds(30), emotes);

                if (selectedEmote is null)
                {
                    await ReplyEmbedAsync(EmbedStyle.Warning, "Вы не выбрали параметр");

                    break;
                }


                var index = emotes.IndexOf(selectedEmote);

                if (index == -1 || index >= parameters.Count)
                {
                    await ReplyEmbedAsync(EmbedStyle.Error, "Параметр не найден");

                    break;
                }


                var embed = CreateEmbedBuilder(EmbedStyle.Information, $"Напишите значение выбранного параметра **{parameterNames[index]}**").Build();
                var valueMessage = await NextMessageAsync(embed: embed);

                if (valueMessage is null)
                {
                    await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, $"Вы не указали значение параметра **{parameterNames[index]}**");

                    break;
                }


                if (parameters[index].PropertyType == typeof(bool?) || parameters[index].PropertyType == typeof(bool))
                {
                    var result = await new BooleanTypeReader().ReadAsync(Context, valueMessage.Content);

                    if (result.IsSuccess)
                        parameters[index].SetValue(settings, result.Values is not null ? result.BestMatch : null);
                    else
                    {
                        await ReplyEmbedAsync(EmbedStyle.Error, $"Не удалось установить значение параметра **{parameterNames[index]}**");

                        break;
                    }
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(parameters[index].PropertyType);

                    if (converter.IsValid(valueMessage.Content))
                    {
                        var value = converter.ConvertFrom(valueMessage.Content);

                        parameters[index].SetValue(settings, value);
                    }
                    else
                    {
                        await ReplyEmbedAsync(EmbedStyle.Error, $"Не удалось установить значение параметра **{parameterNames[index]}**");

                        break;
                    }
                }

                wasSetSettings = true;

                await ReplyEmbedAndDeleteAsync(EmbedStyle.Successfull, $"Значение параметра **{parameterNames[index]}** успешно установлено");


                var isContinue = await ConfirmActionAsync("Продолжить настройку?");

                if (isContinue is not true)
                    break;
            }


            if (wasSetSettings)
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Successfull, "Настройки успешно установлены");
            else
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, "Настройки не были установлены");

            return wasSetSettings;
        }


        private static IEnumerable<string> GetPropertiesName(IEnumerable<PropertyInfo> props)
            => props.Select(p => p.GetPropertyFullName());
    }


    public class MafiaHelpModule : HelpModule
    {
        public MafiaHelpModule(IConfiguration config) : base(config)
        {
        }


        [Command("Роли")]
        public virtual async Task ShowGameRolesAsync(bool sendToServer = false)
        {
            var gameRulesSection = GetGameSection("Roles");

            if (gameRulesSection is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Список ролей не найден");

                return;
            }

            var title = gameRulesSection.GetTitle() ?? "Роли";

            var builder = new EmbedBuilder()
                .WithTitle(title)
                .WithInformationMessage(false);

            foreach (var section in gameRulesSection.GetChildren())
            {
                var roleField = section.GetEmbedFieldInfo();

                if (roleField is not null)
                    builder.AddField(roleField?.Item1, roleField?.Item2);
            }

            if (!sendToServer)
                await Context.User.SendMessageAsync(embed: builder.Build());
            else
                await ReplyAsync(embed: builder.Build());
        }
    }

    protected override GameModuleData CreateGameData(IGuildUser creator)
        => new("Мафия", 3, creator);

    protected override bool CanStart(out string? failMessage)
    {
        if (!base.CanStart(out failMessage))
            return false;

        if (_settings.GameSubSettings.IsCustomGame)
        {
            if (GameData!.Players.Count < _settings.RoleAmountSubSettings.MinimumPlayersCount)
            {
                failMessage = $"Недостаточно игроков. Минимальное количество игроков согласно пользовательским настройкам игры: {_settings.RoleAmountSubSettings.MinimumPlayersCount}";

                return false;
            }


            if (_settings.RoleAmountSubSettings.RedRolesCount == 0)
            {
                failMessage = "Для игры необходимо наличие хотя бы одной красной роли. " +
                    "Измените настройки ролей, добавив красную роль, или установите значение для роли по умолчанию";

                return false;
            }

            if (_settings.RoleAmountSubSettings.BlackRolesCount == 0)
            {
                failMessage = "Для игры необходимо наличие хотя бы одной черной роли. " +
                    "Измените настройки ролей, добавив черную роль, или установите значение для роли по умолчанию";

                return false;
            }


            if (_settings.RoleAmountSubSettings.BlackRolesCount == GameData!.Players.Count)
            {
                failMessage = "Недостаточно игроков. Все участвующие игроки являются черными ролями. Добавьте еще одного игрока, или измените настройки игры";

                return false;
            }

            if (_settings.RoleAmountSubSettings.RedRolesCount == GameData!.Players.Count)
            {
                failMessage = "Недостаточно игроков. Все участвующие игроки являются красными ролями. Добавьте еще одного игрока, или измените настройки игры";

                return false;
            }
        }

        return true;
    }


    public override async Task ResetStatAsync(IGuildUser guildUser)
        => await ResetStatAsync<MafiaStats>(guildUser);

    public override async Task ShowStatsAsync()
        => await ShowStatsAsync(Context.User);
    public override async Task ShowStatsAsync(IUser user)
    {
        var userStat = await Context.Db.MafiaStats
            .AsNoTracking()
            .Include(stat => stat.User)
            .FirstOrDefaultAsync(stat => stat.UserId == user.Id);

        if (userStat is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Warning, "Статистика отсутствует");

            return;
        }

        var embedBuilder = new EmbedBuilder()
        {
            Author = new EmbedAuthorBuilder()
            {
                Name = user.GetFullName(),
                IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl()
            },
            Title = "Статистика",
            Color = Color.Gold
        }
        .AddField("Победы за красные роли", $"{userStat.WinRate:P}", true)
        .AddField("Победы за черные роли", $"{userStat.BlacksWinRate:P}", true)
        .AddField("Суммарные победы", $"{userStat.WinRate / 2 + userStat.BlacksWinRate / 2:P}", true)
        .AddField("Эффективность доктора", $"{userStat.DoctorEfficiency:P}", true)
        .AddField("Эффективность шерифа", $"{userStat.CommissionerEfficiency:P}", true)
        .AddField("Эффективность дона", $"{userStat.DonEfficiency:P}", true)
        .AddField("Кол-во основных очков", $"{userStat.Scores:0.##}", true)
        .AddField("Кол-во доп. очков", $"{userStat.ExtraScores:0.##}", true)
        .AddField("Кол-во штрафных очков", $"{userStat.PenaltyScores:0.##}", true)
        .AddEmptyField(true)
        .AddField("Рейтинг", $"{userStat.Rating:0.##}")
        .WithCurrentTimestamp();

        await ReplyAsync(embed: embedBuilder.Build());
    }

    public override async Task ShowRating()
    {
        var allStats = await Context.Db.MafiaStats
            .AsNoTracking()
            .Where(s => s.GuildSettingsId == Context.Guild.Id && s.Rating > 0)
            .OrderByDescending(stat => stat.Rating)
            .ThenByDescending(stat => stat.WinRate + stat.BlacksWinRate)
            .ThenBy(stat => stat.GamesCount)
            .Include(stat => stat.User)
            .ToListAsync();


        if (allStats.Count == 0)
        {
            await ReplyEmbedAsync(EmbedStyle.Warning, "Рейтинг отсутствует");

            return;
        }


        var playersId = allStats
            .Select(s => s.UserId)
            .ToHashSet();

        var players = Context.Guild.Users
            .Where(u => playersId.Contains(u.Id))
            .ToDictionary(u => u.Id);

        var pages = new List<string>();
        var msg = new PaginatedMessage()
        {
            Title = "Рейтинг мафии",
            Color = Color.Gold,
            Pages = pages
        };

        var ratingsPerPage = 10;

        var page = "";
        for (int i = 0, j = 0; i < allStats.Count; i++, j++)
        {
            if (!players.TryGetValue(allStats[i].UserId, out var user))
                continue;

            page += $"{i + 1}. **{user.GetFullName()}** – {allStats[i].Rating:0.##}\n";

            if ((j + 1) % ratingsPerPage == 0)
            {
                pages.Add(page);
                page = "";
            }
        }

        if (!string.IsNullOrEmpty(page))
            pages.Add(page);

        await PagedReplyAsync(msg);
    }

    public override Task ResetRatingAsync()
        => ResetRatingAsync<MafiaStats>();


    [RequireUserPermission(GuildPermission.Administrator)]
    [Command("ОчкиДополнительные")]
    [Alias("ОчкиДоп", "Очки+")]
    public async Task AddExtraScores(IGuildUser guildUser, float scores)
    {
        var playerStat = await Context.Db.MafiaStats
            .AsTracking()
            .Include(m => m.User)
            .Where(m => m.UserId == guildUser.Id)
            .FirstOrDefaultAsync();

        if (playerStat is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игрок не найден");

            return;
        }

        var guildSettings = await Context.GetGuildSettingsAsync();

        var confirm = await ConfirmActionWithHandlingAsync($"Добавить доп. очки игроку **{guildUser.GetFullName()}**", guildSettings.LogChannelId);


        if (confirm)
        {
            playerStat.ExtraScores += scores;

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Доп. очки успешно начислены игроку **{guildUser.GetFullName()}**", withDefaultFooter: true);

            await Context.Db.SaveChangesAsync();
        }
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [Command("ОчкиШтрафные")]
    [Alias("ОчкиШтраф", "Очки-")]
    public async Task AddPenaltyScores(IGuildUser guildUser, float scores)
    {
        var playerStat = await Context.Db.MafiaStats
            .AsTracking()
            .Include(m => m.User)
            .Where(m => m.UserId == guildUser.Id)
            .FirstOrDefaultAsync();

        if (playerStat is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игрок не найден");

            return;
        }

        var guildSettings = await Context.GetGuildSettingsAsync();

        var confirm = await ConfirmActionWithHandlingAsync($"Добавить штрафные очки игроку **{guildUser.GetFullName()}**", guildSettings.LogChannelId);


        if (confirm)
        {
            playerStat.PenaltyScores += scores;

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Штрафные очки успешно начислены игроку **{guildUser.GetFullName()}**", withDefaultFooter: true);

            await Context.Db.SaveChangesAsync();
        }
    }


    [Command("к")]
    [RequireOwner]
    public async Task Test(int c)
    {
        await ReplyAsync("Ok");
    }

    public override async Task StartAsync()
    {
        GuildLogger = GetGuildLogger(Context.Guild.Id);

        GameData = GetGameData();


        _settings = await Context.GetGameSettingsAsync<MafiaSettings>();

        if (!CanStart(out var msg))
        {
            await ReplyEmbedAsync(EmbedStyle.Error, msg ?? "Невозможно начать игру");

            return;
        }

        GameData!.IsPlaying = true;


        GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game starting...");

        await ReplyEmbedAsync(EmbedStyle.Information, $"{GameData.Name} начинается!", addSmilesToDescription: false);


        _mafiaData = await PreSetupGuildAsync();

        try
        {
            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Begin setup game...");

            await SetupGuildAsync();

            await SetupPlayersAsync();

            await SetupRolesAsync();

            await NotifyPlayersAsync();


            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "End setup game");


            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game started");

            await PlayAsync();

            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game finishing...");


            await _mafiaData.GeneralTextChannel.SendMessageAsync("Игра завершена");


            var isMafiaWon = _mafiaData.AliveMurders.Count > 0 || _mafiaData.AlivePlayers.Count == 0;

            await FinishAsync(isMafiaWon);


            if (GameData.IsPlaying)
            {
                await ReplyEmbedAsync(EmbedStyle.Information, $"{(isMafiaWon ? "Мафия победила!" : "Мирные жители победили!")} Благодарим за участие!", addSmilesToDescription: false);

                await ReplyEmbedAsync(EmbedStyle.Information, $"Участники и их роли:\n{_mafiaData.PlayerGameRoles}", addSmilesToDescription: false);
            }


            if (GameData.IsPlaying && _settings.GameSubSettings.IsRatingGame)
                await SaveStatsAsync();

            await ReplyEmbedAsync(EmbedStyle.Information, "Игра завершена", addSmilesToDescription: false);

            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game finished");
        }
        catch (GameAbortedException e)
        {
            GuildLogger.Debug(e, "Game was stopped");

            await ReplyEmbedAsync(EmbedStyle.Warning, $"Игра остановлена. Причина: {e.Message}");

            await FinishAsync(false, true);
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{Context.Guild.Name} {Context.Guild.Id}] Game was aborted");
            GuildLogger.Error(e, "Game was aborted");

            await ReplyEmbedAsync(EmbedStyle.Error, "Игра аварийно прервана");

            await FinishAsync(false, true);

            throw;
        }
        finally
        {
            if (_settings.ServerSubSettings.GeneralTextChannelId != _mafiaData.GeneralTextChannel.Id)
                await SetAndSaveSettingsToDbAsync();


            DeleteGameData();

            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Gamedata deleted");
        }
    }


    private async Task<MafiaData> PreSetupGuildAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(PreSetupGuildAsync), "Begin presetup guild...");


        var messagesToDelete = Context.Guild.CurrentUser.GuildPermissions.ManageMessages ? 500 : 0;

        var serverSettings = _settings.ServerSubSettings;

        var mafiaData = new MafiaData(await Context.Guild.GetTextChannelOrCreateAsync(serverSettings.GeneralTextChannelId, "мафия-общий", messagesToDelete, SetCategoryChannel),
               await Context.Guild.GetTextChannelOrCreateAsync(serverSettings.MurdersTextChannelId, "мафия-убийцы", messagesToDelete, SetCategoryChannel),
               await Context.Guild.GetVoiceChannelOrCreateAsync(serverSettings.GeneralVoiceChannelId, "мафия-общий", SetCategoryChannel),
               await Context.Guild.GetVoiceChannelOrCreateAsync(serverSettings.MurdersVoiceChannelId, "мафия-убийцы", SetCategoryChannel),
               await Context.Guild.GetRoleOrCreateAsync(serverSettings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
               await Context.Guild.GetRoleOrCreateAsync(serverSettings.WatcherRoleId, "Наблюдатель мафии", null, Color.DarkBlue, true, true));

        GuildLogger.Debug(LogTemplate, nameof(PreSetupGuildAsync), "End presetup guild");


        return mafiaData;



        void SetCategoryChannel(GuildChannelProperties props)
            => props.CategoryId = serverSettings.CategoryChannelId;
    }


    private async Task SetupGuildAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(SetupGuildAsync), "Begin setup guild...");

        await ReplyEmbedAsync(EmbedStyle.Information, "Подготавливаем сервер...");


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Adding overwrite permissions to guild channels");

        foreach (var channel in Context.Guild.Channels)
            await channel.AddPermissionOverwriteAsync(_mafiaData!.MafiaRole, OverwritePermissions.DenyAll(channel));


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Overwrite permissions to guild channels added");

        ConfigureOverwritePermissions();


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Adding overwrite permissions to _mafiaData channels");

        await _mafiaData!.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);
        await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);

        await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new(viewChannel: PermValue.Deny));
        await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new(viewChannel: PermValue.Deny));

        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Overwrite permissions to _mafiaData channels added");

        GuildLogger.Debug(LogTemplate, nameof(SetupGuildAsync), "End setup guild");
    }

    private async Task SetupPlayersAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(SetupPlayersAsync), "Begin setup players...");

        await ReplyEmbedAsync(EmbedStyle.Information, "Собираем досье на игроков...");

        foreach (var player in GameData!.Players)
        {
            if (_settings.ServerSubSettings.SendWelcomeMessage)
            {
                try
                {
                    GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"Sending welcome DM to user {player.GetFullName()}");

                    await player.SendMessageAsync("Добро пожаловать в мафию! Скоро я вышлю вам вашу роль и вы начнете играть.");

                    GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Message sent");
                }
                catch (HttpException e)
                {
                    var msg = $"Не удалось отправить сообщение пользователю {player.GetFullMention()}";

                    await HandleHttpExceptionAsync(msg, e);
                }
                catch (Exception e)
                {
                    Log.Error(e, LogTemplate, nameof(SetupPlayersAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] Failed to send welcome DM to user {player.GetFullName()}");
                    GuildLogger.Error(e, LogTemplate, nameof(SetupPlayersAsync), $"Failed to send welcome DM to user {player.GetFullName()}");

                    await FinishAsync(false, true);

                    throw;
                }
            }

            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Removing overwrite permissions from murder text channel");

            await _mafiaData!.MurderTextChannel.RemovePermissionOverwriteAsync(player);

            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Overwrite permissions removed from murder text channel");

            _mafiaData.PlayerRoles.Add(player.Id, new List<IRole>());

            //_mafiaData.MafiaStatsHelper.AddPlayer(player.Id);


            var guildPlayer = (SocketGuildUser)player;

            if (_settings.ServerSubSettings.RenameUsers && guildPlayer.Id != Context.Guild.OwnerId && guildPlayer.Nickname is null)
            {
                try
                {
                    GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"Renaming user {guildPlayer.GetFullName()}");

                    await guildPlayer.ModifyAsync(props => props.Nickname = $"_{guildPlayer.Username}_");

                    _mafiaData!.OverwrittenNicknames.Add(guildPlayer.Id);

                    GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"User {guildPlayer.GetFullName()} renamed");
                }
                catch (HttpException e)
                {
                    var msg = $"Не удалось назначить ник пользователю {guildPlayer.GetFullMention()}";

                    await HandleHttpExceptionAsync(msg, e);
                }
                catch (Exception e)
                {
                    Log.Error(e, LogTemplate, nameof(SetupPlayersAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] Failed to rename user");
                    GuildLogger.Error(e, LogTemplate, nameof(SetupPlayersAsync), "Failed to rename user");

                    throw;
                }
            }

            if (_settings.ServerSubSettings.RemoveRolesFromUsers)
                foreach (var role in guildPlayer.Roles)
                    if (!role.IsEveryone && role.Id != _mafiaData!.MafiaRole.Id && role.Id != _mafiaData.WatcherRole.Id)
                    {
                        try
                        {
                            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"Removing role {role.Name} ({role.Id}) from user {guildPlayer.GetFullName()}");

                            await guildPlayer.RemoveRoleAsync(role);

                            _mafiaData.PlayerRoles[player.Id].Add(role);

                            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"Role {role.Name} ({role.Id}) removed from user {guildPlayer.GetFullName()}");
                        }
                        catch (HttpException e)
                        {
                            var msg = $"Не удалось убрать роль **{role}** у пользователя {guildPlayer.GetFullMention()}";

                            await HandleHttpExceptionAsync(msg, e);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, LogTemplate, nameof(SetupPlayersAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] Failed to remove role {role.Name} ({role.Id}) from user {guildPlayer.GetFullName()}");
                            GuildLogger.Error(e, LogTemplate, nameof(SetupPlayersAsync), $"Failed to remove role {role.Name} ({role.Id}) from user {guildPlayer.GetFullName()}");

                            throw;
                        }
                    }

            await player.AddRoleAsync(_mafiaData!.MafiaRole);
        }

        GuildLogger.Debug(LogTemplate, nameof(SetupPlayersAsync), "End setup players");
    }

    private async Task SetupRolesAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(SetupRolesAsync), "Begin setup roles");



        await ReplyEmbedAsync(EmbedStyle.Information, "Выдаем игрокам роли...");


        GameData!.Players.Shuffle(3);


        int offset = 0;


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Setuping black roles");

        var rolesInfo = _settings.RolesInfoSubSettings;
        var isCustomGame = _settings.GameSubSettings.IsCustomGame;
        var roleAmount = _settings.RoleAmountSubSettings;



        var doctorsCount = (isCustomGame && roleAmount.DoctorsCount is not null)
            ? roleAmount.DoctorsCount.Value
            : isCustomGame && roleAmount.InnocentCount is not null ? 0 : 1;

        var sheriffsCount = (isCustomGame && roleAmount.SheriffsCount is not null)
            ? roleAmount.SheriffsCount.Value
            : isCustomGame && roleAmount.InnocentCount is not null ? 0 : 1;


        var murdersCount = (isCustomGame && roleAmount.MurdersCount is not null)
            ? roleAmount.MurdersCount
            : (GameData.Players.Count / _settings.GameSubSettings.MafiaCoefficient);

        if (isCustomGame && roleAmount.InnocentCount is not null)
            murdersCount = GameData.Players.Count - roleAmount.InnocentCount.Value;


        var donsCount = (isCustomGame && roleAmount.DonsCount is not null)
            ? roleAmount.DonsCount
            : (murdersCount > 2 ? 1 : 0);

        murdersCount -= donsCount;

        for (int i = 0; i < murdersCount; i++, offset++)
        {
            var murder = new Murder(GameData.Players[offset], GameRoleOptions, 1);

            _mafiaData!.Murders.Add(murder.Player, murder);

            _mafiaData.AllRoles.Add(murder.Player, murder);
        }

        for (int i = 0; i < donsCount; i++, offset++)
        {
            var don = new Don(GameData.Players[offset], GameRoleOptions, 1);

            _mafiaData!.Murders.Add(don.Player, don);

            _mafiaData.Dons.Add(don.Player, don);

            _mafiaData.AllRoles.Add(don.Player, don);
        }

        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Black roles setuped");


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Setuping red roles");


        doctorsCount = Math.Min(doctorsCount, GameData.Players.Count - offset);
        for (int i = 0; i < doctorsCount; i++, offset++)
        {
            var doctor = new Doctor(GameData.Players[offset], GameRoleOptions, 1, rolesInfo.DoctorSelfHealsCount ?? 1);

            _mafiaData!.Doctors.Add(doctor.Player, doctor);

            _mafiaData.Innocents.Add(doctor.Player, doctor);

            _mafiaData.AllRoles.Add(doctor.Player, doctor);
        }


        sheriffsCount = Math.Min(sheriffsCount, GameData.Players.Count - offset);
        for (int i = 0; i < sheriffsCount; i++, offset++)
        {
            var sheriff = new Sheriff(GameData.Players[offset], GameRoleOptions, 1, rolesInfo.SheriffShotsCount ?? default);


            _mafiaData!.Sheriffs.Add(sheriff.Player, sheriff);

            _mafiaData.Innocents.Add(sheriff.Player, sheriff);

            _mafiaData.AllRoles.Add(sheriff.Player, sheriff);
        }



        for (int i = offset; i < GameData.Players.Count; i++)
        {
            var innocent = new Innocent(GameData.Players[i], GameRoleOptions, 1, _settings.RolesInfoSubSettings.CanInnocentsKillAtNight);

            _mafiaData!.AllRoles.Add(innocent.Player, innocent);

            _mafiaData.Innocents.Add(innocent.Player, innocent);
        }


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Red roles setuped");

        GuildLogger.Debug(LogTemplate, nameof(SetupRolesAsync), "End setup roles");
    }

    private async Task NotifyPlayersAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(NotifyPlayersAsync), "Begin notify players");


        foreach (var role in _mafiaData!.AllRoles.Values)
        {
            var player = role.Player;

            var text = $"Вы - {role.Name}";

            _mafiaData.PlayerGameRoles += $"**{role.Name}** - {player.GetFullName()}\n";

            try
            {
                GuildLogger.Verbose(LogTemplate, nameof(NotifyPlayersAsync), $"Sending notify DM to user {player.GetFullName()}");

                await player.SendMessageAsync(text);

                GuildLogger.Verbose(LogTemplate, nameof(NotifyPlayersAsync), $"Notify DM sent to user {player.GetFullName()}");
            }
            catch (HttpException e)
            {
                var msg = $"Не удалось отправить сообщение пользователю {player.GetFullMention()}";

                await HandleHttpExceptionAsync(msg, e);
            }
            catch (Exception e)
            {
                Log.Error(e, LogTemplate, nameof(NotifyPlayersAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] Failed to send notify DM to user {player.GetFullName()}");
                GuildLogger.Error(e, LogTemplate, nameof(NotifyPlayersAsync), $"Failed to send notify DM to user {player.GetFullName()}");

                throw;
            }
        }


        GuildLogger.Debug(LogTemplate, nameof(NotifyPlayersAsync), "End notify players");
    }


    private async Task FinishAsync(bool isMafiaWon, bool isAbort = false)
    {
        GuildLogger!.Debug(LogTemplate, nameof(FinishAsync), $"Begin {(isAbort ? "abort" : "finish")} game...");

        await Context.Guild.DownloadUsersAsync();


        foreach (var role in _mafiaData!.AllRoles.Values)
        {
            var player = role.Player;

            var playerExistInGuild = Context.Guild.GetUser(player.Id) is not null;
            if (!playerExistInGuild)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, $"Игрок {player.GetFullName()} отсутствует на сервере");

                GuildLogger.Debug(LogTemplate, nameof(EjectPlayerAsync), $"Player {player.GetFullName()} does not exists in guild");

                continue;
            }


            if (GameData!.IsPlaying && !isAbort)
            {
                GuildLogger.Verbose(LogTemplate, nameof(FinishAsync), $"Begin update game stat to {player.GetFullName()}...");

                //_mafiaData.MafiaStatsHelper.UpdateWinsStat(isMafiaWon, player);

                GuildLogger.Verbose(LogTemplate, nameof(FinishAsync), $"End update game stat to {player.GetFullName()}");
            }
        }

        foreach (var player in _mafiaData!.AlivePlayers)
        {
            try
            {
                await EjectPlayerAsync(player, false);
            }
            catch (Exception e)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, $"Не удалось вернуть данные игрока {player.GetFullMention()}");

                GuildLogger.Debug(e, LogTemplate, nameof(EjectPlayerAsync), $"Failed to revert guild data of player {player.GetFullName()}");

                continue;
            }
        }

        foreach (var player in _mafiaData!.KilledPlayers)
        {
            GuildLogger.Verbose(LogTemplate, nameof(FinishAsync), $"Removing watcher role from user {player.GetFullName()}");

            try
            {
                await player.RemoveRoleAsync(_mafiaData.WatcherRole);
            }
            catch (Exception e)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, $"Не удалось убрать роль наблюдателя у игрока {player.GetFullMention()}");

                GuildLogger.Debug(e, LogTemplate, nameof(EjectPlayerAsync), $"Failed to remove watcher role from player {player.GetFullName()}");

                continue;
            }

            GuildLogger.Verbose(LogTemplate, nameof(FinishAsync), $"Watcher role removed from user {player.GetFullName()}");
        }

        GuildLogger.Debug(LogTemplate, nameof(FinishAsync), $"End {(isAbort ? "abort" : "finish")} game");
    }

    private async Task PlayAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(PlayAsync), "Begin playing game...");

        await _mafiaData!.GeneralTextChannel.SendMessageAsync($"Добро пожаловать в мафию! Сейчас ночь, весь город спит, а мафия знакомится в отдельном чате.");

        await ChangePermissionsGenaralChannelsAsync(_denyWrite, _denySpeak);
        await _mafiaData.GeneralTextChannel.SendMessageAsync($"Количество мафиози - {_mafiaData.Murders.Count}");


        if (_mafiaData.Murders.Count > 1 && _settings.RolesInfoSubSettings.MurdersKnowEachOther)
        {
            var meetTime = _mafiaData.Murders.Count * 10;

            if (_settings.RolesInfoSubSettings.MurdersKnowEachOther)
                await ChangePermissionsMurderChannelsAsync(_allowWrite, _allowSpeak);

            await _mafiaData.MurderTextChannel.SendMessageAsync("Добро пожаловать в мафию! Сейчас ночь, весь город спит, самое время познакомиться с остальными мафиозниками");


            var murdersList = "";
            foreach (var murder in _mafiaData.Murders.Keys)
                murdersList += $"{murder.GetFullName()}\n";

            await _mafiaData.MurderTextChannel.SendMessageAsync($"Список мафиози:\n{murdersList}");


            var donsList = "";
            foreach (var don in _mafiaData.Dons.Keys)
                donsList += $"{don.GetFullName()}\n";

            await _mafiaData.MurderTextChannel.SendMessageAsync($"Список донов:\n{donsList}");


            await Task.Delay(meetTime * 1000);

            await WaitTimerAsync(meetTime, _mafiaData.GeneralTextChannel, _mafiaData.MurderTextChannel);

            await _mafiaData.MurderTextChannel.SendMessageAsync("Время вышло! Переходите в общий канал и старайтесь не подавать виду, что вы мафиозник.");
        }


        //var nightMurderVoteTime = 30;
        var nightInnocentVoteTime = 40 + _mafiaData.Murders.Count * 10;
        var nightTime = 30 + _mafiaData.Murders.Count * 5;
        var dayVoteTime = 30;
        var lastWordNightCount = _settings.GameSubSettings.LastWordNightCount;


        while (GameData!.IsPlaying && CanContinueGame)
        {
            try
            {
                if (_settings.RolesInfoSubSettings.MurdersKnowEachOther)
                    await ChangePermissionsMurderChannelsAsync(_denyWrite, _denySpeak);

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"{_mafiaData.MafiaRole.Mention} Доброе утро, жители города! Самое время пообщаться всем вместе.");


                var lastWordTasks = new List<Task<string>>();

                if (!_mafiaData.IsFirstNight)
                {
                    await _mafiaData.GeneralTextChannel.SendMessageAsync($"Но сначала новости: сегодня утром, в незаправленной постели...");

                    var delay = Task.Delay(2500);

                    if (!_settings.RolesInfoSubSettings.MurdersVoteTogether)
                    {
                        if (_settings.RolesInfoSubSettings.MurdersMustVoteForOnePlayer)
                        {
                            var killedPlayer = _mafiaData.Murders.Values.First().KilledPlayer;
                            if (!_mafiaData.Murders.Values.Where(m => m.IsAlive).All(m => m.KilledPlayer == killedPlayer))
                                foreach (var murder in _mafiaData.Murders.Values)
                                    murder.KilledPlayer = null;
                        }
                        else
                        {
                            var votes = new Dictionary<IGuildUser, int>();

                            foreach (var murder in _mafiaData.Murders.Values.Where(m => m.IsAlive && m.KilledPlayer is not null))
                                votes[murder.KilledPlayer!] = votes.TryGetValue(murder.KilledPlayer!, out var count) ? count + 1 : 1;

                            IGuildUser? selectedPlayer = null;

                            if (votes.Count == 1)
                                selectedPlayer = votes.Keys.First();
                            else if (votes.Count > 1)
                            {
                                var votesList = votes.ToList();

                                votesList.Sort((v1, v2) => v2.Value - v1.Value);

                                if (votesList[0].Value > votesList[1].Value)
                                    selectedPlayer = votesList[0].Key;
                            }

                            foreach (var murder in _mafiaData.Murders.Values)
                                murder.KilledPlayer = selectedPlayer;
                        }
                    }

                    IGuildUser? killedPlayerByInnocents = null;
                    if (_settings.RolesInfoSubSettings.CanInnocentsKillAtNight)
                    {
                        var innocents = _mafiaData.Innocents.Values.Where(i => i.GetType() == typeof(Innocent) && i.IsAlive);

                        if (innocents.Any())
                        {
                            if (_settings.RolesInfoSubSettings.InnocentsMustVoteForOnePlayer)
                            {
                                var killedPlayer = innocents.First().LastMove;
                                if (!innocents.All(i => i.LastMove == killedPlayer))
                                    foreach (var innocent in innocents)
                                        innocent.ProcessMove(null, false);
                                else
                                    killedPlayerByInnocents = killedPlayer;
                            }
                            else
                            {
                                var votes = new Dictionary<IGuildUser, int>();

                                foreach (var innocent in innocents.Where(i => i.LastMove is not null))
                                    votes[innocent.LastMove!] = votes.TryGetValue(innocent.LastMove!, out var count) ? count + 1 : 1;

                                IGuildUser? selectedPlayer = null;

                                if (votes.Count == 1)
                                    selectedPlayer = votes.Keys.First();
                                else if (votes.Count > 1)
                                {
                                    var votesList = votes.ToList();

                                    votesList.Sort((v1, v2) => v2.Value - v1.Value);

                                    if (votesList[0].Value > votesList[1].Value)
                                        selectedPlayer = votesList[0].Key;
                                }

                                foreach (var innocent in innocents)
                                    innocent.ProcessMove(selectedPlayer, false);
                            }
                        }
                    }

                    var kills = _mafiaData.Killers.Where(k => k.KilledPlayer is not null).Distinct().Select(k => k.KilledPlayer!).ToList();
                    if (killedPlayerByInnocents is not null)
                        kills.Add(killedPlayerByInnocents);


                    var heals = _mafiaData.Healers.Where(h => h.HealedPlayer is not null).Distinct().Select(k => k.HealedPlayer!);

                    var corpses = kills.Except(heals).ToList();


                    var msg = "";
                    if (corpses.Count > 0)
                    {
                        corpses.Shuffle();

                        msg += corpses.Count == 1 ? "Был обнаружен труп:\n" : "Были обнаружены трупы:\n";

                        for (int i = 0; i < corpses.Count; i++)
                            msg += $"{corpses[i].Mention}\n";
                    }
                    else
                        msg = "Никого не оказалось. Все живы.";


                    await delay;

                    await _mafiaData.GeneralTextChannel.SendMessageAsync(msg);

                    for (int i = 0; i < corpses.Count; i++)
                    {
                        await EjectPlayerAsync(corpses[i]);

                        if (lastWordNightCount > 0)
                        {
                            var task = HandleAsync(corpses[i]);

                            lastWordTasks.Add(task);
                        }


                        async Task<string> HandleAsync(IGuildUser player)
                        {
                            var dmChannel = await player.GetOrCreateDMChannelAsync();

                            var criteria = new Criteria<SocketMessage>()
                            .AddCriterion(new EnsureFromUserCriterion(player))
                            .AddCriterion(new EnsureFromChannelCriterion(dmChannel));

                            var msg = await NextMessageAsync(criteria,
                                $"{player.Username}, у вас есть 30с для последнего слова, воспользуйтесь этим временем с умом",
                                timeout: TimeSpan.FromSeconds(30),
                                messageChannel: dmChannel);

                            return msg is not null
                            ? $"{player.GetFullName()} перед смертью сказал следующее:\n{msg?.Content ?? "пустое сообщение"}"
                            : $"{player.GetFullName()} умер молча";
                        }
                    }

                    lastWordNightCount--;


                    if (!CanContinueGame)
                        break;
                }

                var dayTime = _mafiaData.AlivePlayers.Count * 20;

                if (_mafiaData.IsFirstNight)
                {
                    dayTime /= 2;

                    // TODO: Repeat rules.
                }

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"Обсуждайте. ({dayTime}с)");

                await ChangePermissionsGenaralChannelsAsync(_allowWrite, _allowSpeak);
                
                var timer = WaitTimerAsync(dayTime, _mafiaData.GeneralTextChannel);

                await Task.WhenAll(lastWordTasks);


                while (lastWordTasks.Count > 0)
                {
                    var receivedMessageTask = await Task.WhenAny(lastWordTasks);
                    lastWordTasks.Remove(receivedMessageTask);

                    var msg = await receivedMessageTask;

                    await _mafiaData.GeneralTextChannel.SendMessageAsync(embed: CreateEmbedBuilder(EmbedStyle.Information, msg).Build());
                }


                await timer;

                await ChangePermissionsGenaralChannelsAsync(_denyWrite, _denySpeak);


                if (!_mafiaData.IsFirstNight)
                {
                    await _mafiaData.GeneralTextChannel.SendMessageAsync(
                        $"{_mafiaData.MafiaRole.Mention} Время голосовать! Выбирайте жителя, который будет изгнан сегодня. ({dayVoteTime}с)");

                    await Task.Delay(1000);

                    await _mafiaData.GeneralTextChannel.RemovePermissionOverwriteAsync(_mafiaData!.WatcherRole);


                    var aliveGroup = new AliveGroup(_mafiaData.AllRoles.Values.Where(r => r.IsAlive).ToList(), GameRoleOptions, 1);


                    await DoRoleMoveAsync(_mafiaData.GeneralTextChannel, aliveGroup);


                    await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);


                    if (aliveGroup.LastMove is not null)
                        await EjectPlayerAsync(aliveGroup.LastMove);
                }
                else
                    _mafiaData.IsFirstNight = false;

                if (!GameData.IsPlaying || !CanContinueGame)
                    break;

                await Task.Delay(2000);
                await _mafiaData.GeneralTextChannel.SendMessageAsync("Город засыпает...");

                GuildLogger.Verbose(LogTemplate, nameof(PlayAsync), "Begin do night moves...");


                var tasks = new List<Task>();

                var except = new List<GameRole>();
                except.AddRange(_mafiaData.Murders.Values);
                except.AddRange(_mafiaData.Sheriffs.Values);

                foreach (var role in _mafiaData.AllRoles.Values.Except(except).Where(r => r.CanDoMove))
                    tasks.Add(DoRoleMoveAsync(await role.Player.GetOrCreateDMChannelAsync(), role));


                if (_settings.RolesInfoSubSettings.MurdersKnowEachOther)
                    await ChangePermissionsMurderChannelsAsync(_allowWrite, _allowSpeak);

                if (_settings.RolesInfoSubSettings.MurdersVoteTogether)
                {
                    var murdersGroup = new MurdersGroup(_mafiaData.Murders.Values.Where(m => m.CanDoMove).ToList(), GameRoleOptions, 1);
                    tasks.Add(DoRoleMoveAsync(_mafiaData.MurderTextChannel, murdersGroup));
                }
                else
                    foreach (var murder in _mafiaData.Murders.Values)
                        tasks.Add(DoRoleMoveAsync(await murder.Player.GetOrCreateDMChannelAsync(), murder));


                foreach (var sheriff in _mafiaData.Sheriffs.Values.Where(s => s.CanDoMove))
                {
                    var task = Task.Run(async () =>
                    {
                        var channel = await sheriff.Player.GetOrCreateDMChannelAsync();

                        var options = new List<bool>()
                        {
                            false,
                            true
                        };

                        var dispalyOptions = new List<string>()
                        {
                            "Выполнить проверку",
                            "Сделать выстрел"
                        };

                        var (shotSelected, _) = await WaitForVotingAsync(channel, 20, options, dispalyOptions);

                        sheriff.ConfigureMove(shotSelected);

                        await DoRoleMoveAsync(channel, sheriff);
                    });

                    tasks.Add(task);
                }


                try
                {
                    Task.WaitAll(tasks.ToArray());

                    if (_mafiaData.Dons.Count > 0)
                    {
                        await _mafiaData.GeneralTextChannel.SendMessageAsync("Дон выбирает к кому наведаться ночью");

                        foreach (var role in _mafiaData.Dons.Values.Where(d => d.CanDoMove))
                            tasks.Add(DoRoleMoveAsync(await role.Player.GetOrCreateDMChannelAsync(), role));


                        Task.WaitAll(tasks.ToArray());
                    }
                }
                catch (AggregateException ae)
                {
                    var flattenAe = ae.Flatten();

                    Log.Error(flattenAe, LogTemplate, nameof(PlayAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] Failed to do night moves");
                    GuildLogger.Error(flattenAe, LogTemplate, nameof(PlayAsync), "Failed to do night moves");

                    throw flattenAe;
                }


                GuildLogger.Verbose(LogTemplate, nameof(PlayAsync), "End do night moves");

            }
            catch (HttpException e)
            {
                await ReplyAsync($"HTTP: {e.Reason}\n{e.Message}");

                GuildLogger.Warning(e, LogTemplate, nameof(PlayAsync), "Handling HTTP exception");
            }
        }

        GuildLogger.Debug(LogTemplate, nameof(PlayAsync), "End playing game");
    }



    private async Task DoRoleMoveAsync(IMessageChannel channel, GameRole role)
    {
        GuildLogger!.Debug(LogTemplate, nameof(DoRoleMoveAsync), $"Begin do {role.Name} move...");

        if (role.IsAlive)
        {
            await channel.SendMessageAsync($"{role.Name}, ваш ход! ({40}с)");

            var except = role.GetExceptList();

            var options = _mafiaData!.AlivePlayers.Except(except).ToList();
            var displayOptions = options.Select(user => user.GetFullName()).ToList();


            var (selectedPlayer, isSkip) = await WaitForVotingAsync(
                channel,
                40,
                options,
                displayOptions);


            role.ProcessMove(selectedPlayer, isSkip);

            if (selectedPlayer is not null)
                await channel.SendMessageAsync($"Выбор сделан. {role.GetRandomPhrase(true)}");
            else
            {
                var msg = $"Выбор не был сделан. {(isSkip ? "Вы пропустили голосование" : role.GetRandomPhrase(false))}";

                await channel.SendMessageAsync(msg);
            }

            await channel.SendMessageAsync("Ожидайте наступления утра");
        }
        else
        {
            await WaitTimerAsync(33);
        }

        GuildLogger.Debug(LogTemplate, nameof(DoRoleMoveAsync), $"End do {role.Name} move");
    }

    private async Task ChangePermissionsMurderChannelsAsync(OverwritePermissions textPerms, OverwritePermissions voicePerms)
    {
        foreach (var murder in _mafiaData!.AliveMurders)
        {
            await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(murder, textPerms);
            await _mafiaData.MurderVoiceChannel.AddPermissionOverwriteAsync(murder, voicePerms);

            if (murder.VoiceChannel != null && voicePerms.ViewChannel == PermValue.Deny)
                await murder.ModifyAsync(props => props.Channel = null);
        }
    }

    private async Task ChangePermissionsGenaralChannelsAsync(OverwritePermissions textPerms, OverwritePermissions voicePerms)
    {
        await _mafiaData!.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, textPerms);
        await _mafiaData.GeneralVoiceChannel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, voicePerms);

        foreach (var player in _mafiaData.AlivePlayers)
        {
            if (player.VoiceChannel != null && voicePerms.ViewChannel == PermValue.Deny)
                await player.ModifyAsync(props => props.Channel = null);
        }
    }



    private async Task EjectPlayerAsync(IGuildUser player, bool isKill = true)
    {
        GuildLogger!.Debug(LogTemplate, nameof(EjectPlayerAsync), $"Begin eject player {player.GetFullName()}");


        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Removing overwrite permissions in murder channel for player {player.GetFullName()}");

        await _mafiaData!.MurderTextChannel.RemovePermissionOverwriteAsync(player);

        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Overwrite permissions removed in murder channel for player {player.GetFullName()}");


        _mafiaData.AllRoles[player].GameOver();


        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Removing _mafiaData role from player {player.GetFullName()}");

        await player.RemoveRoleAsync(_mafiaData.MafiaRole);

        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Mafia role removed from {player.GetFullName()}");


        if (_mafiaData.PlayerRoles.ContainsKey(player.Id))
        {
            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Add roles to player {player.GetFullName()}");

            await player.AddRolesAsync(_mafiaData.PlayerRoles[player.Id]);

            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Roles added to player {player.GetFullName()}");
        }

        if (_mafiaData.OverwrittenNicknames.Contains(player.Id))
        {
            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Renaming player {player.GetFullName()}");

            await player.ModifyAsync(props => props.Nickname = null);

            _mafiaData.OverwrittenNicknames.Remove(player.Id);

            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Player {player.GetFullName()} renamed");
        }


        if (isKill)
        {
            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Adding watcher role to player {player.GetFullName()}");

            await player.AddRoleAsync(_mafiaData.WatcherRole);

            _mafiaData.KilledPlayers.Add(player);

            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Watcher role added to player {player.GetFullName()}");
        }


        GuildLogger.Debug(LogTemplate, nameof(EjectPlayerAsync), $"End eject player {player.GetFullName()}");
    }



    private async Task HandleHttpExceptionAsync(string message, HttpException e)
    {
        if (_settings.ServerSubSettings.ReplyMessagesOnSetupError)
            await ReplyEmbedAsync(EmbedStyle.Error, message);

        if (_settings.ServerSubSettings.AbortGameWhenError)
            throw new GameAbortedException(message, e);
    }


    private void ConfigureOverwritePermissions()
    {
        _allowWrite = OverwritePermissions.DenyAll(_mafiaData!.GeneralTextChannel).Modify(
           viewChannel: PermValue.Allow,
           readMessageHistory: PermValue.Allow,
           sendMessages: PermValue.Allow);

        _denyWrite = OverwritePermissions.DenyAll(_mafiaData.GeneralTextChannel).Modify(
            viewChannel: PermValue.Allow,
            readMessageHistory: PermValue.Allow);

        _allowSpeak = OverwritePermissions.DenyAll(_mafiaData.GeneralVoiceChannel).Modify(
            viewChannel: PermValue.Allow,
            connect: PermValue.Allow,
            useVoiceActivation: PermValue.Allow,
            speak: PermValue.Allow
            );

        _denySpeak = OverwritePermissions.DenyAll(_mafiaData.GeneralVoiceChannel);
    }


    private async Task SaveStatsAsync()
    {
        var playersId = new HashSet<ulong>(GameData!.Players.Select(p => p.Id));

        await AddNewUsersAsync(playersId);

        var playersStat = await Context.Db.MafiaStats
                  .AsTracking()
                  .Where(stat => playersId.Contains(stat.UserId) && stat.GuildSettingsId == Context.Guild.Id)
                  .ToListAsync();

        var newPlayersId = playersId
            .Except(playersStat.Select(s => s.UserId))
            .ToList();

        if (newPlayersId.Count > 0)
        {
            var newPlayersStats = newPlayersId.Select(id => new MafiaStats
            {
                UserId = id,
                GuildSettingsId = Context.Guild.Id
            })
            .ToList();

            await Context.Db.MafiaStats.AddRangeAsync(newPlayersStats);

            playersStat.AddRange(newPlayersStats);
        }


        //foreach (var playerStat in playersStat)
        //{
        //    var gameStat = _mafiaData!.MafiaStatsHelper[playerStat.UserId];

        //    playerStat.GamesCount++;
        //    playerStat.WinsCount += gameStat.WinsCount;

        //    playerStat.BlacksGamesCount += gameStat.BlacksGamesCount;
        //    playerStat.BlacksWinsCount += gameStat.BlacksWinsCount;

        //    playerStat.DoctorMovesCount += gameStat.DoctorMovesCount;
        //    playerStat.DoctorSuccessfullMovesCount += gameStat.DoctorSuccessfullMovesCount;

        //    playerStat.CommissionerMovesCount += gameStat.CommissionerMovesCount;
        //    playerStat.CommissionerSuccessfullFoundsCount += gameStat.CommissionerSuccessfullFoundsCount;
        //    playerStat.CommissionerSuccessfullShotsCount += gameStat.CommissionerSuccessfullShotsCount;

        //    playerStat.DonMovesCount += gameStat.DonMovesCount;
        //    playerStat.DonSuccessfullMovesCount += gameStat.DonSuccessfullMovesCount;


        //    playerStat.ExtraScores += gameStat.ExtraScores;

        //    playerStat.PenaltyScores += gameStat.PenaltyScores;
        //}


        await Context.Db.SaveChangesAsync();
    }


    private async Task SetAndSaveSettingsToDbAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(SetAndSaveSettingsToDbAsync), "Settings saving...");


        _settings.ServerSubSettings = _settings.ServerSubSettings with
        {
            GeneralTextChannelId = _mafiaData!.GeneralTextChannel.Id,
            MurdersTextChannelId = _mafiaData.MurderTextChannel.Id
        };

        await Context.Db.SaveChangesAsync();

        GuildLogger.Debug(LogTemplate, nameof(SetAndSaveSettingsToDbAsync), "Settings saved");
    }


    private class MafiaData
    {
        public IReadOnlyCollection<IKiller> Killers => AllRoles.Values.Where(r => r.IsAlive && r is IKiller).Cast<IKiller>().ToList();

        public IReadOnlyCollection<IHealer> Healers => AllRoles.Values.Where(r => r.IsAlive && r is IHealer).Cast<IHealer>().ToList();

        public IReadOnlyCollection<IGuildUser> AlivePlayers => AllRoles.Values.Where(r => r.IsAlive).Select(r => r.Player).ToList();

        public IReadOnlyCollection<IGuildUser> AliveMurders => Murders.Values.Where(m => m.IsAlive).Select(m => m.Player).ToList();

        public Dictionary<IGuildUser, GameRole> AllRoles { get; }

        public Dictionary<IGuildUser, Innocent> Innocents { get; }

        public Dictionary<IGuildUser, Doctor> Doctors { get; }

        public Dictionary<IGuildUser, Sheriff> Sheriffs { get; }

        public Dictionary<IGuildUser, Murder> Murders { get; }

        public Dictionary<IGuildUser, Don> Dons { get; }




        //public MafiaStatsHelper MafiaStatsHelper { get; }

        public Dictionary<ulong, ICollection<IRole>> PlayerRoles { get; }

        public List<ulong> OverwrittenNicknames { get; }



        public List<IGuildUser> KilledPlayers { get; }


        public ITextChannel GeneralTextChannel { get; }
        public ITextChannel MurderTextChannel { get; }

        public IVoiceChannel GeneralVoiceChannel { get; }
        public IVoiceChannel MurderVoiceChannel { get; }


        public IRole MafiaRole { get; }
        public IRole WatcherRole { get; }


        public bool IsFirstNight { get; set; }

        public bool HasDoctorSelfHealed { get; set; }

        public string PlayerGameRoles { get; set; }


        public MafiaData(ITextChannel generalTextChannel,
                         ITextChannel murderTextChannel,
                         IVoiceChannel generalVoiceChannel,
                         IVoiceChannel murderVoiceChannel,
                         IRole mafiaRole,
                         IRole watcherRole)
        {
            AllRoles = new();
            Innocents = new();
            Doctors = new();
            Sheriffs = new();
            Murders = new();
            Dons = new();


            // MafiaStatsHelper = new(this);

            PlayerRoles = new();

            OverwrittenNicknames = new();


            KilledPlayers = new();


            IsFirstNight = true;

            PlayerGameRoles = "";

            GeneralTextChannel = generalTextChannel;
            MurderTextChannel = murderTextChannel;
            GeneralVoiceChannel = generalVoiceChannel;
            MurderVoiceChannel = murderVoiceChannel;
            MafiaRole = mafiaRole;
            WatcherRole = watcherRole;
        }
    }

    /*
    private class MafiaStatsHelper
    {
        private readonly MafiaData _mafiaData;

        private readonly Dictionary<ulong, MafiaStats> _mafiaStats;


        public MafiaStatsHelper(MafiaData mafiaData)
        {
            _mafiaStats = new();

            _mafiaData = mafiaData;
        }


        public MafiaStats this[ulong key] => _mafiaStats[key];


        public void AddPlayer(ulong playerId)
            => _mafiaStats[playerId] = new()
            {
                UserId = playerId
            };



        public void UpdateWinsStat(bool isMafiaWon, IGuildUser player)
        {
            if (_mafiaData.AllMurders.Contains(player))
            {
                if (isMafiaWon)
                {
                    _mafiaStats[player.Id].BlacksWinsCount++;
                    _mafiaStats[player.Id].ExtraScores += 0.5f;
                }
            }
            else if (!isMafiaWon)
                _mafiaStats[player.Id].WinsCount++;
        }

        public void UpdateDoctorStat(bool savedFromMurder, bool savedFromCommissioner)
        {
            if (_mafiaData.Doctor is null)
                return;

            var doctorStats = _mafiaStats[_mafiaData.Doctor.Id];

            switch (savedFromMurder, savedFromCommissioner)
            {
                case (true, true):
                    doctorStats.DoctorSuccessfullMovesCount++;
                    doctorStats.ExtraScores++;
                    break;

                case (true, false):
                    doctorStats.DoctorSuccessfullMovesCount++;
                    if (_mafiaData.MurdersMove == _mafiaData.Commissioner)
                        doctorStats.ExtraScores += 0.5f;
                    break;

                case (false, true):
                    if (!_mafiaData.AliveMurders.Contains(_mafiaData.DoctorMove!))
                        doctorStats.DoctorSuccessfullMovesCount++;
                    break;

                default:
                    break;
            }
        }

        public void UpdateCommissionerStat(bool wasCommissionerKill)
        {
            if (_mafiaData.Has3MurdersInGame && _mafiaData.Commissioner is not null && wasCommissionerKill)
            {
                var commissionerStats = _mafiaStats[_mafiaData.Commissioner.Id];

                if (_mafiaData.AliveMurders.Contains(_mafiaData.CommissionerMove!))
                {
                    commissionerStats.CommissionerSuccessfullShotsCount++;
                    commissionerStats.ExtraScores++;
                }
                else if (_mafiaData.Doctor == _mafiaData.CommissionerMove)
                    commissionerStats.PenaltyScores += 1.5f;
                else
                    commissionerStats.PenaltyScores++;
            }
        }
        public void UpdateCommissionerStat(bool hasSkipped, bool hasFoundMurder)
        {
            if (_mafiaData.Commissioner is null)
                return;

            var commissionerStats = _mafiaStats[_mafiaData.Commissioner.Id];

            if (!hasSkipped)
            {
                commissionerStats.CommissionerMovesCount++;

                if (hasFoundMurder)
                    commissionerStats.CommissionerSuccessfullFoundsCount++;
            }
        }


        public void UpdateDonStat(bool hasSkipped, bool hasFoundCommissioner)
        {
            if (_mafiaData.Don is null)
                return;

            var donStats = _mafiaStats[_mafiaData.Don.Id];

            if (!hasSkipped)
            {
                donStats.CommissionerMovesCount++;

                if (hasFoundCommissioner)
                {

                    donStats.DonSuccessfullMovesCount++;
                    donStats.ExtraScores++;
                }
            }
        }

        public void AddDoctorMovesCount()
        {
            if (_mafiaData.Doctor is not null)
                _mafiaStats[_mafiaData.Doctor.Id].DoctorMovesCount++;
        }
    }*/
}
