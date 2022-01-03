using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Chronology;
using Core.Exceptions;
using Core.Extensions;
using Core.Interfaces;
using Core.TypeReaders;
using Core.ViewModels;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
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
using static System.Collections.Specialized.BitVector32;

namespace Modules.Games.Mafia;

[Group("Мафия")]
[Alias("маф", "м")]
public class MafiaModule : GameModule
{
    protected IOptionsSnapshot<GameRoleData> GameRoleOptions { get; }

    private MafiaData? _mafiaData;

    private MafiaSettings _settings;

    private readonly MafiaChronology _chronology;


    private OverwritePermissions _denyView;
    private OverwritePermissions _allowWrite;
    private OverwritePermissions _denyWrite;
    private OverwritePermissions? _allowSpeak;



    public MafiaModule(InteractiveService interactiveService,
                       IConfiguration config,
                       IOptionsSnapshot<GameRoleData> gameRoleOptions) : base(interactiveService, config)
    {
        GameRoleOptions = gameRoleOptions;

        _chronology = new();
        _settings = MafiaSettings.Empty;
    }





    // Name from config
    protected override GameModuleData CreateGameData(IGuildUser creator)
        => new("Мафия", 3, creator);

    protected override bool CanStart(out string? failMessage)
    {
        if (!base.CanStart(out failMessage))
            return false;

        
        ArgumentNullException.ThrowIfNull(GameData);


        if (_settings.Current.GameSubSettings.IsCustomGame)
        {
            var roleAmountSettings = _settings.Current.RoleAmountSubSettings;

            if (GameData.Players.Count < roleAmountSettings.MinimumPlayersCount)
            {
                failMessage = $"Недостаточно игроков. Минимальное количество игроков согласно пользовательским настройкам игры: {roleAmountSettings.MinimumPlayersCount}";

                return false;
            }


            if (roleAmountSettings.RedRolesCount == 0 && roleAmountSettings.AllRedRolesSetted)
            {
                failMessage = "Для игры необходимо наличие хотя бы одной красной роли. " +
                    "Измените настройки ролей, добавив красную роль, или установите значение для роли по умолчанию";

                return false;
            }

            if (roleAmountSettings.BlackRolesCount == 0 && roleAmountSettings.AllBlackRolesSetted)
            {
                failMessage = "Для игры необходимо наличие хотя бы одной черной роли. " +
                    "Измените настройки ролей, добавив черную роль, или установите значение для роли по умолчанию";

                return false;
            }


            if (roleAmountSettings.BlackRolesCount == GameData.Players.Count)
            {
                failMessage = "Недостаточно игроков. Все участвующие игроки являются черными ролями. Добавьте еще одного игрока, или измените настройки игры";

                return false;
            }

            if (roleAmountSettings.RedRolesCount == GameData.Players.Count)
            {
                failMessage = "Недостаточно игроков. Все участвующие игроки являются красными ролями. Добавьте еще одного игрока, или измените настройки игры";

                return false;
            }
        }

        return true;
    }



    [RequireOwner]
    [Command("тест")]
    public async Task DebugStartAsync(int count)
    {
        GameData = GetGameData();

        ArgumentNullException.ThrowIfNull(GameData);


        var players = Context.Guild.Users.Take(count);

        foreach (var p in players)
            if (!GameData.Players.Contains(p))
                GameData.Players.Add(p);

        _settings = await Context.GetGameSettingsAsync<MafiaSettings>();

        _settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == _settings.Id && s.Name == _settings.CurrentTemplateName);

        if (!CanStart(out var msg))
        {
            await ReplyEmbedAsync(EmbedStyle.Error, msg ?? "Невозможно начать игру");

            return;
        }

        _mafiaData = await PreSetupGuildAsync();


        await SetupPlayersAsync();

        await SetupRolesAsync();

        await FinishAsync(false);

        DeleteGameData();

        await ReplyEmbedStampAsync(EmbedStyle.Information, "Игра завершена");

        await ReplyAsync(string.Join('\n', _mafiaData.AllRoles.Values.Select(r => $"{r.Player} - {r.Name}")));
    }


    public override async Task StartAsync()
    {
        GameData = GetGameData();


        _settings = await Context.GetGameSettingsAsync<MafiaSettings>();

        _settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == _settings.Id && s.Name == _settings.CurrentTemplateName);


        if (!CanStart(out var msg))
        {
            await ReplyEmbedAsync(EmbedStyle.Error, msg ?? "Невозможно начать игру");

            return;
        }

        ArgumentNullException.ThrowIfNull(GameData);

        GameData.IsPlaying = true;


        GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game starting...");

        await ReplyEmbedStampAsync(EmbedStyle.Information, $"{GameData.Name} начинается!");

        if (_settings.Current.ServerSubSettings.MentionPlayersOnGameStart)
            await MentionPlayers();


        _mafiaData = await PreSetupGuildAsync();

        GameData.Players.Shuffle(3);

        try
        {
            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Begin setup game...");


            var setupGuild = SetupGuildAsync();

            var setupPlayers = SetupPlayersAsync();

            var sendWelcomeMessages = SendWelcomeMessagesAsync();


            await Task.WhenAll(setupGuild, setupPlayers, sendWelcomeMessages);

            await SetupRolesAndNotifyPlayersAsync();


            if (_mafiaData.WatcherTextChannel is not null)
                await SendPlayerRolesToSpectatorsAsync();


            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "End setup game");


            await PlayAsync();

            await FinishAsync();
        }
        catch (GameAbortedException e)
        {
            GuildLogger.Debug(e, "Game was stopped");

            await ReplyEmbedAsync(EmbedStyle.Warning, $"Игра остановлена\n{e.Message}");

            await FinishAsync(true);
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{Context.Guild.Name} {Context.Guild.Id}] Game was aborted");
            GuildLogger.Error(e, "Game was aborted");

            await ReplyEmbedAsync(EmbedStyle.Error, "Игра аварийно прервана");

            await FinishAsync(true);

            throw;
        }
        finally
        {
            DeleteGameData();

            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Gamedata deleted");
        }
    }


    private async Task FinishAsync(bool isAbort = false)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        
        ArgumentNullException.ThrowIfNull(GameData);

        GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game finishing...");


        await _mafiaData.GeneralTextChannel.SendMessageAsync("Игра завершена");


        bool? isMurdersWon = IsMurdersWon();


        if (GameData.IsPlaying)
        {
            var winnerMessage = isMurdersWon switch
            {
                true => "Мафия победила!",
                false => "Мирные жители победили!",
                _ => "Никто не победил. Город опустел..."
            };

            await ReplyEmbedAsync(EmbedStyle.Information, $"{winnerMessage}\nБлагодарим за участие!");


            var playerRoles = string.Join("\n", _mafiaData.AllRoles.Values.Select(r => $"{r.Name} - {r.Player.GetFullName()}"));

            await ReplyEmbedAsync(EmbedStyle.Information, playerRoles, "Участники и их роли");


            var chonologyDays = _chronology.GetActionsHistory();

            var paginator = new LazyPaginatorBuilder()
                .WithPageFactory(GeneratePageBuilderAsync)
                .WithMaxPageIndex(chonologyDays.Count - 1)
                .WithCacheLoadedPages(true)
                .WithUsers(_mafiaData.AllRoles.Keys)
                .Build();

            _ = Interactive.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(1));


            Task<PageBuilder> GeneratePageBuilderAsync(int index)
            {
                var pageBuilder = new PageBuilder()
                    .WithDescription(chonologyDays[index].FlattenActionsHistory())
                    .WithTitle($"День {index + 1}");

                return Task.FromResult(pageBuilder);
            }
        }


        await ReplyEmbedStampAsync(EmbedStyle.Information, "Игра завершена");


        GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game finished");

        await ReturnPlayersDataAsync();
    }

    private async Task ReturnPlayersDataAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);


        GuildLogger.Debug(LogTemplate, nameof(FinishAsync), $"Returning players data...");

        await Context.Guild.DownloadUsersAsync();


        foreach (var role in _mafiaData.AllRoles.Values)
        {
            var player = role.Player;

            var playerExistInGuild = Context.Guild.GetUser(player.Id) is not null;

            if (!playerExistInGuild)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, $"Игрок **{player.GetFullName()}** отсутствует на сервере");

                continue;
            }


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

        foreach (var player in _mafiaData.KilledPlayers)
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

        GuildLogger.Debug(LogTemplate, nameof(FinishAsync), $"Players data returned");
    }



    private async Task<MafiaData> PreSetupGuildAsync()
    {
        GuildLogger.Debug(LogTemplate, nameof(PreSetupGuildAsync), "Begin presetup guild...");


        _settings.CategoryChannelId ??= (await Context.Guild.CreateCategoryChannelAsync("Мафия")).Id;



        var mafiaData = new MafiaData(
               await Context.Guild.GetTextChannelOrCreateAsync(_settings.GeneralTextChannelId, "мафия-общий", SetCategoryChannel),
               await Context.Guild.GetTextChannelOrCreateAsync(_settings.MurdersTextChannelId, "мафия-убийцы", SetCategoryChannel),
               Context.Guild.GetTextChannel(_settings.WatchersTextChannelId ?? 0),
               Context.Guild.GetVoiceChannel(_settings.GeneralVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(_settings.MurdersVoiceChannelId ?? 0),
               Context.Guild.GetVoiceChannel(_settings.WatchersVoiceChannelId ?? 0),
               await Context.Guild.GetRoleOrCreateAsync(_settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
               Context.Guild.GetRole(_settings.WatcherRoleId ?? 0),
               _settings.Current.GameSubSettings.VoteTime);


        if (_settings.ClearChannelsOnStart)
        {
            await mafiaData.GeneralTextChannel.ClearAsync();

            await mafiaData.MurderTextChannel.ClearAsync();

            if (mafiaData.WatcherTextChannel is not null)
                await mafiaData.WatcherTextChannel.ClearAsync();
        }


        GuildLogger.Debug(LogTemplate, nameof(PreSetupGuildAsync), "End presetup guild");


        return mafiaData;


        void SetCategoryChannel(GuildChannelProperties props)
        {
            props.CategoryId = _settings.CategoryChannelId;

            var overwrites = new List<Overwrite>
                {
                    new(Context.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny))
                };

            props.PermissionOverwrites = overwrites;
        }
    }


    private async Task SetupGuildAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);

        GuildLogger.Debug(LogTemplate, nameof(SetupGuildAsync), "Begin setup guild...");

        await ReplyEmbedAsync(EmbedStyle.Information, "Подготавливаем сервер...");


        ConfigureOverwritePermissions();


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Adding overwrite permissions to guild channels");

        var tasks = new List<Task>();

        foreach (var channel in Context.Guild.Channels)
        {
            if (channel.Id == _mafiaData.GeneralTextChannel.Id)
                continue;

            var perms = channel.GetPermissionOverwrite(_mafiaData.MafiaRole);

            if (perms?.ViewChannel == PermValue.Deny)
                continue;

            tasks.Add(channel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, _denyView));
        }

        await Task.WhenAll(tasks);


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Overwrite permissions to guild channels added");


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Adding overwrite permissions to _mafiaData channels");

        if (_mafiaData.WatcherRole is not null)
        {
            var generalTextPerms = _mafiaData.GeneralTextChannel.GetPermissionOverwrite(_mafiaData.WatcherRole);
            var murderTextPerms = _mafiaData.MurderTextChannel.GetPermissionOverwrite(_mafiaData.WatcherRole);

            if (!Equals(generalTextPerms, _denyWrite))
                await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);

            if (!Equals(murderTextPerms, _denyWrite))
                await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);


            if (_mafiaData.WatcherTextChannel is not null)
            {
                var specPerms = _mafiaData.WatcherTextChannel.GetPermissionOverwrite(_mafiaData.WatcherRole);

                if (!Equals(specPerms, _allowWrite))
                    await _mafiaData.WatcherTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _allowWrite);
            }

            if (_mafiaData.WatcherVoiceChannel is not null && _allowSpeak is not null)
            {
                var specPerms = _mafiaData.WatcherVoiceChannel.GetPermissionOverwrite(_mafiaData.WatcherRole);

                if (!Equals(specPerms, _allowSpeak))
                    await _mafiaData.WatcherVoiceChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _allowSpeak.Value);
            }
        }


        if (!Equals(_mafiaData.GeneralTextChannel.GetPermissionOverwrite(Context.Guild.EveryoneRole), _denyView))
            await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, _denyView);

        if (!Equals(_mafiaData.MurderTextChannel.GetPermissionOverwrite(Context.Guild.EveryoneRole), _denyView))
            await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, _denyView);


        GuildLogger.Verbose(LogTemplate, nameof(SetupGuildAsync), "Overwrite permissions to _mafiaData channels added");

        GuildLogger.Debug(LogTemplate, nameof(SetupGuildAsync), "End setup guild");



        static bool Equals(OverwritePermissions? o1, OverwritePermissions? o2)
            => (o1?.AllowValue == o2?.AllowValue) && (o1?.DenyValue == o2?.DenyValue);
    }

    private async Task SetupPlayersAsync()
    {
        ArgumentNullException.ThrowIfNull(GameData);

        GuildLogger.Debug(LogTemplate, nameof(SetupPlayersAsync), "Begin setup players...");

        await ReplyEmbedAsync(EmbedStyle.Information, "Собираем досье на игроков...");

        var tasks = new List<Task>();

        foreach (var player in GameData.Players)
            tasks.Add(Task.Run(() => HandlePlayerAsync(player)));

        await Task.WhenAll(tasks);


        GuildLogger.Debug(LogTemplate, nameof(SetupPlayersAsync), "End setup players");


        async Task HandlePlayerAsync(IGuildUser player)
        {
            
            ArgumentNullException.ThrowIfNull(_mafiaData);

            var serverSettings = _settings.Current.ServerSubSettings;


            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Removing overwrite permissions from murder text channel");

            await _mafiaData.MurderTextChannel.RemovePermissionOverwriteAsync(player);

            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Overwrite permissions removed from murder text channel");

            _mafiaData.PlayerRoleIds.Add(player.Id, new List<ulong>());

            //_mafiaData.MafiaStatsHelper.AddPlayer(player.Id);


            var guildPlayer = (SocketGuildUser)player;

            if (serverSettings.RenameUsers && guildPlayer.Nickname is null && guildPlayer.Id != Context.Guild.OwnerId)
            {
                try
                {
                    GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"Renaming user {guildPlayer.GetFullName()}");

                    await guildPlayer.ModifyAsync(props => props.Nickname = $"_{guildPlayer.Username}_");

                    _mafiaData.OverwrittenNicknames.Add(guildPlayer.Id);

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


            if (serverSettings.RemoveRolesFromUsers)
            {
                var playerRoles = guildPlayer.Roles
                    .Where(role => !role.IsEveryone && role.Id != _mafiaData.MafiaRole.Id && role.Id != (_mafiaData.WatcherRole?.Id ?? 0));

                foreach (var role in playerRoles)
                    try
                    {
                        GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), $"Removing role {role.Name} ({role.Id}) from user {guildPlayer.GetFullName()}");

                        await guildPlayer.RemoveRoleAsync(role);

                        _mafiaData.PlayerRoleIds[player.Id].Add(role.Id);

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

            await player.AddRoleAsync(_mafiaData.MafiaRole);
        }
    }

    private async Task SendWelcomeMessagesAsync()
    {
        var serverSettings = _settings.Current.ServerSubSettings;

        if (!serverSettings.SendWelcomeMessage)
            return;


        ArgumentNullException.ThrowIfNull(GameData);

        var tasks = new List<Task>();

        foreach (var player in GameData.Players)
        {
            GuildLogger.Verbose(LogTemplate, nameof(SendWelcomeMessagesAsync), $"Sending welcome DM to user {player.GetFullName()}...");

            var welcomeMessage = "Добро пожаловать в мафию! Скоро я вышлю вам вашу роль и вы начнете играть.";
            var errorMsg = $"Не удалось отправить приветственное сообщение пользователю {player.GetFullMention()}";
            var errorLogMsg = $"Failed to send welcome DM to user {player.GetFullName()}";

            tasks.Add(SendMessageAsync(player, welcomeMessage, errorMsg, errorLogMsg));
        }

        await Task.WhenAll(tasks);
    }


    private async Task SetupRolesAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        
        ArgumentNullException.ThrowIfNull(GameData);

        GuildLogger.Debug(LogTemplate, nameof(SetupRolesAsync), "Begin setup roles");



        await ReplyEmbedAsync(EmbedStyle.Information, "Выдаем игрокам роли...");


        int offset = 0;


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Setuping black roles");

        var rolesInfo = _settings.Current.RolesInfoSubSettings;
        var isCustomGame = _settings.Current.GameSubSettings.IsCustomGame;
        var mafiaCoefficient = _settings.Current.GameSubSettings.MafiaCoefficient;
        var roleAmount = _settings.Current.RoleAmountSubSettings;

        var blackRolesCount = Math.Max(GameData.Players.Count / mafiaCoefficient, 1);
        var redRolesCount = Math.Max((int)(blackRolesCount / 2.5f), 1);

        var doctorsCount = 0;
        var sheriffsCount = 0;
        var murdersCount = 0;
        var donsCount = 0;


        if (isCustomGame)
        {
            var neutralsCount = roleAmount.NeutralRolesCount ?? 0;

            var exceptInnocentsCount = GameData.Players.Count - roleAmount.InnocentCount;

            var redRolesRemainsCount = exceptInnocentsCount - (roleAmount.BlackRolesCount ?? blackRolesCount) - neutralsCount;


            doctorsCount = roleAmount.DoctorsCount ?? Math.Min(redRolesRemainsCount ?? redRolesCount, redRolesCount);

            sheriffsCount = roleAmount.SheriffsCount ?? Math.Min((redRolesRemainsCount - doctorsCount) ?? redRolesCount, redRolesCount);


            murdersCount = roleAmount.MurdersCount ?? (exceptInnocentsCount - doctorsCount - sheriffsCount - neutralsCount) ?? blackRolesCount;

            donsCount = roleAmount.DonsCount ?? (murdersCount > 2 ? 1 : 0);

            if (roleAmount.DonsCount is null)
                murdersCount -= donsCount;

        }
        else
        {
            doctorsCount = redRolesCount;

            sheriffsCount = redRolesCount;

            murdersCount = blackRolesCount;

            donsCount = murdersCount > 2 ? 1 : 0;
        }


        for (int i = 0; i < murdersCount; i++, offset++)
        {
            var murder = new Murder(GameData.Players[offset], GameRoleOptions);

            _mafiaData.Murders.Add(murder.Player, murder);

            _mafiaData.AllRoles.Add(murder.Player, murder);
        }

        for (int i = 0; i < donsCount; i++, offset++)
        {
            var don = new Don(GameData.Players[offset], GameRoleOptions, _mafiaData.Sheriffs.Values);

            _mafiaData.Murders.Add(don.Player, don);

            _mafiaData.Dons.Add(don.Player, don);

            _mafiaData.AllRoles.Add(don.Player, don);
        }


        if (isCustomGame)
        {
            for (int i = 0; i < roleAmount.ManiacsCount; i++, offset++)
            {
                var maniac = new Maniac(GameData.Players[offset], GameRoleOptions);

                _mafiaData.Maniacs.Add(maniac.Player, maniac);

                _mafiaData.Neutrals.Add(maniac.Player, maniac);

                _mafiaData.AllRoles.Add(maniac.Player, maniac);
            }


            for (int i = 0; i < roleAmount.HookersCount; i++, offset++)
            {
                var hooker = new Hooker(GameData.Players[offset], GameRoleOptions);

                _mafiaData.Hookers.Add(hooker.Player, hooker);

                _mafiaData.Neutrals.Add(hooker.Player, hooker);

                _mafiaData.AllRoles.Add(hooker.Player, hooker);
            }
        }


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Black roles setuped");


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Setuping red roles");


        for (int i = 0; i < doctorsCount; i++, offset++)
        {
            var doctor = new Doctor(GameData.Players[offset], GameRoleOptions, rolesInfo.DoctorSelfHealsCount ?? 1);

            _mafiaData.Doctors.Add(doctor.Player, doctor);

            _mafiaData.Innocents.Add(doctor.Player, doctor);

            _mafiaData.AllRoles.Add(doctor.Player, doctor);
        }


        for (int i = 0; i < sheriffsCount; i++, offset++)
        {
            var sheriff = new Sheriff(GameData.Players[offset], GameRoleOptions, rolesInfo.SheriffShotsCount ?? 0, _mafiaData.Murders.Values);


            _mafiaData.Sheriffs.Add(sheriff.Player, sheriff);

            _mafiaData.Innocents.Add(sheriff.Player, sheriff);

            _mafiaData.AllRoles.Add(sheriff.Player, sheriff);
        }



        for (int i = offset; i < GameData.Players.Count; i++)
        {
            var innocent = new Innocent(GameData.Players[i], GameRoleOptions);

            _mafiaData.AllRoles.Add(innocent.Player, innocent);

            _mafiaData.Innocents.Add(innocent.Player, innocent);
        }


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Red roles setuped");

        GuildLogger.Debug(LogTemplate, nameof(SetupRolesAsync), "End setup roles");
    }

    private async Task NotifyPlayersAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);

        GuildLogger.Debug(LogTemplate, nameof(NotifyPlayersAsync), "Begin notify players");


        var tasks = new List<Task>();

        foreach (var role in _mafiaData.AllRoles.Values)
        {
            var player = role.Player;

            var text = $"Ваша роль - {role.Name}";


            GuildLogger.Verbose(LogTemplate, nameof(NotifyPlayersAsync), $"Sending notify DM to user {player.GetFullName()}...");


            var errorMsg = $"Не удалось отправить игровое сообщение пользователю {player.GetFullMention()}";
            var errorLogMsg = $"Failed to send notify message to DM user {player.GetFullName()}";

            tasks.Add(SendMessageAsync(player, text, errorMsg, errorLogMsg));
        }

        await Task.WhenAll(tasks);

        // Send roles list

        GuildLogger.Debug(LogTemplate, nameof(NotifyPlayersAsync), "End notify players");
    }

    private async Task SetupRolesAndNotifyPlayersAsync()
    {
        await SetupRolesAsync();

        await NotifyPlayersAsync();
    }

    private async Task SendPlayerRolesToSpectatorsAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);

        if (_mafiaData.WatcherTextChannel is null)
            return;


        var playerRoles = string.Join("\n", _mafiaData.AllRoles.Values.Select(r => $"{r.Name} - {r.Player.GetFullName()}"));

        await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, playerRoles, "Роли игроков"));
    }


    private async Task SendMessageAsync(IGuildUser user, string message, string errorMsg, string errorLogMsg)
    {
        try
        {
            await user.SendMessageAsync(message);
        }
        catch (HttpException e)
        {
            await HandleHttpExceptionAsync(errorMsg, e);
        }
        catch (Exception e)
        {
            Log.Error(e, LogTemplate, nameof(SendWelcomeMessagesAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] {errorLogMsg}");
            GuildLogger.Error(e, LogTemplate, nameof(SendWelcomeMessagesAsync), errorLogMsg);

            await FinishAsync(true);

            throw;
        }
    }

    private async Task HandleTaskAsync(Action action, string errorMsg, string errorLogMsg)
    {
        try
        {
            await Task.Run(action);
        }
        catch (HttpException e)
        {
            await HandleHttpExceptionAsync(errorMsg, e);
        }
        catch (Exception e)
        {
            Log.Error(e, LogTemplate, nameof(SendWelcomeMessagesAsync), $"[{Context.Guild.Name} {Context.Guild.Id}] {errorLogMsg}");
            GuildLogger.Error(e, LogTemplate, nameof(SendWelcomeMessagesAsync), errorLogMsg);

            await FinishAsync(true);

            throw;
        }
    }



    private async Task PlayAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        
        ArgumentNullException.ThrowIfNull(GameData);

        GuildLogger.Debug(LogTemplate, nameof(PlayAsync), "Begin playing game...");

        await ChangeGenaralChannelPermissionsAsync(_denyWrite, _denyView);

        if (_settings.Current.GameSubSettings.IsCustomGame && _settings.Current.PreGameMessage is not null)
            await _mafiaData.GeneralTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, _settings.Current.PreGameMessage, "Сообщение перед игрой"));

        await _mafiaData.GeneralTextChannel.SendMessageAsync($"Добро пожаловать в мафию! Сейчас ночь, весь город спит, а мафия знакомится в отдельном чате.");

        await _mafiaData.GeneralTextChannel.SendMessageAsync($"Количество мафиози - {_mafiaData.Murders.Count}");


        if (_mafiaData.Murders.Count > 1 && _settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
            await IntroduceMurdersAsync();


        var nightTime = 30 + _mafiaData.Murders.Count * 5;
        var lastWordNightCount = _settings.Current.GameSubSettings.LastWordNightCount;


        while (GameData.IsPlaying && CanContinueGame())
        {
            try
            {
                _chronology.NextDay();

                if (_mafiaData.WatcherTextChannel is not null)
                    await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, "Новый день"));

                if (_settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
                    await ChangeMurdersChannelPermissionsAsync(_denyWrite, _denyView);

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"{_mafiaData.MafiaRole.Mention} Доброе утро, жители города! Самое время пообщаться всем вместе.");


                var lastWordTasks = new List<Task<string>>();


                if (!_mafiaData.IsFirstNight)
                {
                    await _mafiaData.GeneralTextChannel.SendMessageAsync($"Но сначала новости: сегодня утром, в незаправленной постели...");

                    var delay = Task.Delay(3000);


                    var corpses = GetCorpses(out var revealedManiacs);


                    var msg = "";
                    if (corpses.Count > 0)
                    {
                        msg += corpses.Count == 1 ? "Был обнаружен труп:\n" : "Были обнаружены трупы:\n";

                        for (int i = 0; i < corpses.Count; i++)
                            msg += $"{corpses[i].Mention}\n";
                    }
                    else
                        msg = "Никого не оказалось. Все живы.";


                    await delay;

                    await _mafiaData.GeneralTextChannel.SendMessageAsync(msg);


                    if (revealedManiacs.Count > 0)
                    {
                        var str = "Слухи доносят, что следующие игроки являются маньяками:";

                        for (int i = 0; i < revealedManiacs.Count; i++)
                        {
                            string action = _chronology.AddAction("Роль раскрыта", _mafiaData.Maniacs[revealedManiacs[i]]);

                            if (_mafiaData.WatcherTextChannel is not null)
                                await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, action));


                            str += $"\n{revealedManiacs[i].GetFullMention()}";
                        }

                        if (_settings.Current.GameSubSettings.IsCustomGame && _settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
                            await _mafiaData.GeneralTextChannel.SendMessageAsync(str);
                        else
                            foreach (var murder in _mafiaData.Murders.Values)
                                if (murder.IsAlive)
                                    await murder.Player.SendMessageAsync(str);
                    }


                    for (int i = 0; i < corpses.Count; i++)
                    {
                        string action = _chronology.AddAction("Труп", _mafiaData.AllRoles[corpses[i]]);

                        if (_mafiaData.WatcherTextChannel is not null)
                            await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, action));


                        await EjectPlayerAsync(corpses[i]);

                        if (lastWordNightCount > 0)
                        {
                            var task = HandleAsync(corpses[i]);

                            lastWordTasks.Add(task);
                        }


                        async Task<string> HandleAsync(IGuildUser player)
                        {
                            var dmChannel = await player.CreateDMChannelAsync();

                            var result = await NextMessageAsync(
                                embed: CreateEmbed(EmbedStyle.Information, $"{player.Username}, у вас есть 30с для последнего слова, воспользуйтесь этим временем с умом. Напишите здесь сообщение, которое увидят все игроки Мафии"),
                                timeout: TimeSpan.FromSeconds(30),
                                messageChannel: dmChannel);

                            if (result.IsSuccess && result.Value is not null)
                            {
                                await dmChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Successfull, "Сообщение успешно отправлено"));

                                return $"{player.GetFullName()} перед смертью сказал следующее:\n{result.Value.Content ?? "*пустое сообщение*"}";
                            }
                            else
                            {
                                return $"{player.GetFullName()} умер молча";
                            }
                        }
                    }


                    lastWordNightCount--;


                    if (!CanContinueGame())
                        break;
                }


                var dayTime = _mafiaData.AllRoles.Values.Count(r => r.IsAlive) * 20;

                if (_mafiaData.IsFirstNight)
                {
                    dayTime /= 2;

                    // TODO: Repeat rules.
                }

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"Обсуждайте. ({dayTime}с)");

                await ChangeGenaralChannelPermissionsAsync(_allowWrite, _allowSpeak);

                var timer = WaitForTimerAsync(dayTime, _mafiaData.GeneralTextChannel);


                var fooledPlayers = new List<IGuildUser>();

                foreach (var role in _mafiaData.AllRoles.Values)
                {
                    if (role.BlockedByHooker)
                        fooledPlayers.Add(role.Player);

                    role.SetPhase(false);
                    role.UnblockAll();
                }


                await Task.WhenAll(lastWordTasks);


                while (lastWordTasks.Count > 0)
                {
                    var receivedMessageTask = await Task.WhenAny(lastWordTasks);

                    lastWordTasks.Remove(receivedMessageTask);

                    var msg = await receivedMessageTask;


                    await _mafiaData.GeneralTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, msg));
                }


                await timer;

                await ChangeGenaralChannelPermissionsAsync(_denyWrite, _denyView);


                if (!_mafiaData.IsFirstNight)
                {
                    var move = await DoDayVotingAsync(_mafiaData.VoteTime);

                    if (move is not null)
                    {
                        var role = _mafiaData.AllRoles[move];

                        string action = "";

                        if (!fooledPlayers.Contains(role.Player))
                        {
                            action = _chronology.AddAction("Труп", _mafiaData.AllRoles[move]);


                            await EjectPlayerAsync(move);
                        }
                        else
                        {
                            action = _chronology.AddAction("Использование алиби", _mafiaData.AllRoles[move]);


                            await _mafiaData.GeneralTextChannel.SendMessageAsync(
                                embed: CreateEmbed(EmbedStyle.Warning, $"{move.GetFullMention()} не покидает игру, так как у него есть алиби"));
                        }

                        if (_mafiaData.WatcherTextChannel is not null)
                            await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, action));
                    }
                }
                else
                    _mafiaData.IsFirstNight = false;


                if (!GameData.IsPlaying || !CanContinueGame())
                    break;


                await Task.Delay(2000);

                await _mafiaData.GeneralTextChannel.SendMessageAsync("Город засыпает...");

                GuildLogger.Verbose(LogTemplate, nameof(PlayAsync), "Begin do night moves...");


                foreach (var role in _mafiaData.AllRoles.Values)
                    role.SetPhase(true);


                await DoNightMovesAsync();


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


    private async Task IntroduceMurdersAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        

        var meetTime = _mafiaData.Murders.Count * 10;

        if (_settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
            await ChangeMurdersChannelPermissionsAsync(_allowWrite, _allowSpeak);

        await _mafiaData.MurderTextChannel.SendMessageAsync("Добро пожаловать в мафию! Сейчас ночь, весь город спит, самое время познакомиться с остальными мафиозниками");


        var murdersList = string.Join("\n", _mafiaData.Murders.Keys.Select(m => m.GetFullName()));

        await _mafiaData.MurderTextChannel.SendMessageAsync($"Список мафиози:\n{murdersList}");


        var donsList = string.Join("\n", _mafiaData.Dons.Keys.Select(d => d.GetFullName()));

        await _mafiaData.MurderTextChannel.SendMessageAsync($"Список донов:\n{donsList}");


        await WaitForTimerAsync(meetTime, _mafiaData.GeneralTextChannel, _mafiaData.MurderTextChannel);

        await _mafiaData.MurderTextChannel.SendMessageAsync("Время вышло! Переходите в общий канал и старайтесь не подавать виду, что вы мафиозник.");
    }

    private async Task<IGuildUser?> DoDayVotingAsync(int dayVoteTime)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        


        await _mafiaData.GeneralTextChannel.SendMessageAsync(
            $"{_mafiaData.MafiaRole.Mention} Время голосовать! Выбирайте жителя, который будет изгнан сегодня. ({dayVoteTime}с)");

        await Task.Delay(1000);

        IGuildUser? move = null;

        if (!_settings.Current.GameSubSettings.IsTurnByTurnVote)
        {
            await _mafiaData.GeneralTextChannel.RemovePermissionOverwriteAsync(_mafiaData.WatcherRole);

            var aliveGroup = new AliveGroup(_mafiaData.AllRoles.Values.Where(r => r.IsAlive).ToList(), GameRoleOptions);


            await DoRoleMoveAsync(_mafiaData.GeneralTextChannel, aliveGroup, false);

            move = aliveGroup.LastMove;


            await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);
        }
        else
        {
            var votes = new Dictionary<IGuildUser, int>();
            var skipCount = 0;

            foreach (var role in _mafiaData.AllRoles.Values.Where(r => r.IsAlive))
            {
                var channel = await role.Player.CreateDMChannelAsync();

                if (votes.Count > 0)
                {
                    var str = string.Join("\n", votes.Select(v => $"**{v.Key.GetFullName()}** - {v.Value}")) + $"\nПропуск голосования- {skipCount}";

                    var embed = CreateEmbed(EmbedStyle.Information, str, "Распределение голосов игроков");

                    await channel.SendMessageAsync(embed: embed);
                }
                else
                    await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, "Голосов еще нет", "Распределение голосов игроков"));

                var innocent = new Innocent(role.Player, GameRoleOptions);

                await DoRoleMoveAsync(channel, innocent, false);

                if (innocent.LastMove is not null)
                    votes[innocent.LastMove] = votes.TryGetValue(innocent.LastMove, out var count) ? count + 1 : 1;
                else if (innocent.IsSkip)
                    skipCount++;
            }

            IGuildUser? selectedPlayer = null;
            var isSkip = true;

            if (votes.Count == 1)
            {
                var player = votes.Keys.First();

                if (skipCount < votes[player])
                {
                    selectedPlayer = player;

                    isSkip = false;
                }
            }
            else if (votes.Count > 1)
            {
                var votesList = votes.ToList();

                votesList.Sort((v1, v2) => v2.Value - v1.Value);

                if (votesList[0].Value > votesList[1].Value && votesList[0].Value > skipCount)
                {
                    selectedPlayer = votesList[0].Key;

                    isSkip = false;
                }
            }

            foreach (var role in _mafiaData.AllRoles.Values.Where(r => r.IsAlive))
                role.ProcessMove(selectedPlayer, isSkip);

            move = selectedPlayer;

            var msg = (isSkip, move is null) switch
            {
                (false, false) => $"Город изгнал {move!.GetFullName()}",
                (false, true) => "Сегодня никто не был изгнан",
                _ => "Город пропустил голосование"
            };

            await _mafiaData.GeneralTextChannel.SendMessageAsync(msg);
        }


        return move;
    }

    private async Task DoNightMovesAsync()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        


        var tasks = new List<Task>();

        var exceptRoles = new List<GameRole>();
        exceptRoles.AddRange(_mafiaData.Murders.Values);
        exceptRoles.AddRange(_mafiaData.Sheriffs.Values);
        exceptRoles.AddRange(_mafiaData.Hookers.Values);


        foreach (var hooker in _mafiaData.Hookers.Values)
        {
            tasks.Add(DoRoleMoveAsync(await hooker.Player.CreateDMChannelAsync(), hooker));
        }

        await _mafiaData.GeneralTextChannel.SendMessageAsync("Путана выбирает клиента");

        await Task.WhenAll(tasks);

        await _mafiaData.GeneralTextChannel.SendMessageAsync("Путана выбрала клиента");

        foreach (var hooker in _mafiaData.Hookers.Values)
        {
            if (!hooker.IsAlive || hooker.BlockedPlayer is null)
                continue;

            _mafiaData.AllRoles[hooker.BlockedPlayer].Block(hooker);
        }


        foreach (var role in _mafiaData.AllRoles.Values.Except(exceptRoles))
            tasks.Add(DoRoleMoveAsync(await role.Player.CreateDMChannelAsync(), role));


        if (_settings.Current.RolesInfoSubSettings.MurdersKnowEachOther)
            await ChangeMurdersChannelPermissionsAsync(_allowWrite, _allowSpeak);


        if (_settings.Current.RolesInfoSubSettings.MurdersVoteTogether)
        {
            var exceptBlockedMurders = new List<Murder>();

            await ChangeMurdersChannelPermissionsAsync(_denyWrite, _denyView);

            foreach (var murder in _mafiaData.Murders.Values)
                if (murder.BlockedByHooker)
                {
                    exceptBlockedMurders.Add(murder);

                    await murder.Player.SendMessageAsync("Вас охмурила путана. Развлекайтесь с ней");

                    var player = murder.Player;

                    await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(player, _denyView);

                    if (_mafiaData.MurderVoiceChannel is not null)
                        await _mafiaData.MurderVoiceChannel.AddPermissionOverwriteAsync(player, _denyView);

                    await player.ModifyAsync(props => props.Channel = null);


                    murder.ProcessMove(null, true);
                }



            await _mafiaData.MurderTextChannel.SendMessageAsync("Время обсуждить кто станет жертвой (20с)");

            await WaitForTimerAsync(20, _mafiaData.MurderTextChannel);


            var murders = _mafiaData.Murders.Values.Except(exceptBlockedMurders).ToList();

            if (murders.Count > 0)
            {
                var murdersGroup = new MurdersGroup(_mafiaData.Murders.Values.Except(exceptBlockedMurders).ToList(), GameRoleOptions);
                tasks.Add(DoRoleMoveAsync(_mafiaData.MurderTextChannel, murdersGroup));
            }
        }
        else
            foreach (var murder in _mafiaData.Murders.Values)
                tasks.Add(DoRoleMoveAsync(await murder.Player.CreateDMChannelAsync(), murder));


        foreach (var sheriff in _mafiaData.Sheriffs.Values)
        {
            var task = Task.Run(async () =>
            {
                var channel = await sheriff.Player.CreateDMChannelAsync();

                var choiceVoteTime = Math.Max(_mafiaData.VoteTime / 3, 20);

                if (sheriff.IsAvailableToShot && !sheriff.BlockedByHooker)
                {
                    await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, $"{sheriff.Name}, выбирайте ваше действие ({choiceVoteTime}с)"));

                    var options = new List<bool>()
                    {
                        true,
                        false
                    };

                    var displayOptions = new List<string>()
                    {
                        "Сделать выстрел",
                        "Выполнить проверку"
                    };

                    var (shotSelected, _) = await WaitForVotingAsync(channel, choiceVoteTime, options, displayOptions);

                    sheriff.ConfigureMove(shotSelected);


                    if (_mafiaData.WatcherTextChannel is not null)
                    {
                        string action = _chronology.AddAction(shotSelected ? "Выбран выстрел" : "Выбрана проверка на мафиози", sheriff);

                        await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, action));
                    }
                }
                else
                    choiceVoteTime = 0;


                await DoRoleMoveAsync(channel, sheriff, true, _mafiaData.VoteTime - choiceVoteTime);
            });

            tasks.Add(task);
        }


        await Task.WhenAll(tasks);

        if (_mafiaData.Dons.Count > 0)
        {
            await _mafiaData.GeneralTextChannel.SendMessageAsync("Дон выбирает к кому наведаться ночью");

            foreach (var don in _mafiaData.Dons.Values)
            {
                don.SetChecking(true);

                tasks.Add(DoRoleMoveAsync(await don.Player.CreateDMChannelAsync(), don));
            }


            await Task.WhenAll(tasks);
        }
    }

    private IGuildUser? GetInnocentKill(bool innocentsMustVoteForOnePlayer)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);


        IGuildUser? killedPlayerByInnocents = null;

        var innocents = _mafiaData.Innocents.Values.Where(i => i.GetType() == typeof(Innocent) && i.IsAlive);

        if (innocents.Any())
        {
            if (innocentsMustVoteForOnePlayer)
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

                killedPlayerByInnocents = selectedPlayer;
            }
        }

        return killedPlayerByInnocents;
    }

    private IReadOnlyList<IGuildUser> GetCorpses(out IList<IGuildUser> revealedManiacs)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);
        

        if (!_settings.Current.RolesInfoSubSettings.MurdersVoteTogether)
        {
            if (_settings.Current.RolesInfoSubSettings.MurdersMustVoteForOnePlayer)
            {
                var killedPlayer = _mafiaData.Murders.Values.First().KilledPlayer;
                if (!_mafiaData.Murders.Values.Where(m => m.IsAlive).All(m => m.KilledPlayer == killedPlayer))
                    foreach (var murder in _mafiaData.Murders.Values)
                        murder.ProcessMove(null, false);
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
                    murder.ProcessMove(selectedPlayer, false);
            }
        }

        IGuildUser? killedPlayerByInnocents = null;
        if (_settings.Current.RolesInfoSubSettings.CanInnocentsKillAtNight)
            killedPlayerByInnocents = GetInnocentKill(_settings.Current.RolesInfoSubSettings.InnocentsMustVoteForOnePlayer);


        var killers = _mafiaData.AllRoles.Values
            .Where(r => r.IsAlive && r is IKiller)
            .Cast<IKiller>();

        var kills = killers
            .Where(k => k.KilledPlayer is not null)
            .Select(k => k.KilledPlayer!)
            .ToList();

        if (killedPlayerByInnocents is not null)
            kills.Add(killedPlayerByInnocents);


        var healers = _mafiaData.AllRoles.Values
            .Where(r => r.IsAlive && r is IHealer)
            .Cast<IHealer>();

        var heals = healers
            .Where(h => h.HealedPlayer is not null)
            .Select(k => k.HealedPlayer!);


        var corpses = kills
            .Except(heals)
            .ToList();

        var maniacKills = _mafiaData.Maniacs.Values
            .Where(m => m.KilledPlayer is not null)
            .Select(m => m.KilledPlayer!)
            .Except(_mafiaData.Hookers.Values
                .Where(m => m.HealedPlayer is not null)
                .Select(m => m.HealedPlayer!));

        corpses.AddRange(maniacKills);


        revealedManiacs = new List<IGuildUser>();

        foreach (var hooker in _mafiaData.Hookers.Values)
        {
            if (!hooker.IsAlive || hooker.HealedPlayer is null)
                continue;


            if (corpses.Contains(hooker.Player))
                corpses.Add(hooker.HealedPlayer);

            if (_mafiaData.Maniacs.ContainsKey(hooker.HealedPlayer))
                revealedManiacs.Add(hooker.HealedPlayer);
        }


        return corpses
            .Distinct()
            .Shuffle()
            .ToList();
    }


    private bool CanContinueGame()
    {
        
        ArgumentNullException.ThrowIfNull(_mafiaData);


        var aliveMurdersCount = _mafiaData.Murders.Values.Count(m => m.IsAlive);

        var alivePlayersCount = _mafiaData.AllRoles.Values.Count(r => r.IsAlive);

        var defaultCondition = alivePlayersCount > aliveMurdersCount * 2;


        var gameSettings = _settings.Current.GameSubSettings;

        if (gameSettings.IsCustomGame)
        {
            var canContinue = aliveMurdersCount > 0;

            if (gameSettings.ConditionContinueGameWithNeutrals)
            {
                var hasAnyNeutral = _mafiaData.Neutrals.Values.Any(n => n.IsAlive && n is IKiller);

                canContinue |= hasAnyNeutral;
            }

            if (gameSettings.ConditionAliveAtLeast1Innocent)
            {
                var hasAnyInnocent = _mafiaData.Innocents.Values.Any(i => i.IsAlive);

                canContinue &= hasAnyInnocent;
            }
            else
                canContinue &= defaultCondition;


            return canContinue;
        }
        else
        {
            var canContinue = aliveMurdersCount > 0 && defaultCondition;

            return canContinue;
        }
    }

    private bool? IsMurdersWon()
    {
        
        ArgumentNullException.ThrowIfNull(_mafiaData);


        var hasAnyMurder = _mafiaData.Murders.Values.Any(m => m.IsAlive);

        if (hasAnyMurder)
            return true;


        var hasAnyALiveInnocent = _mafiaData.Innocents.Values.Any(i => i.IsAlive);

        if (hasAnyALiveInnocent)
            return false;


        return null;
    }


    private async Task DoRoleMoveAsync(IMessageChannel channel, GameRole role, bool isNightMove = true, int? overrideVoteTime = null)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);

        GuildLogger.Debug(LogTemplate, nameof(DoRoleMoveAsync), $"Begin do {role.Name} move...");

        overrideVoteTime ??= _mafiaData.VoteTime;

        if (role.IsAlive && !role.BlockedByHooker)
        {
            await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, $"{role.Name}, ваш ход!"));

            await Task.Delay(1500);

            await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, $"{role.GetRandomYourMovePhrase()} ({overrideVoteTime}с)"));


            var except = role.GetExceptList();

            var options = _mafiaData.AllRoles.Values.Where(r => r.IsAlive).Select(r => r.Player).Except(except).ToList();
            var displayOptions = options.Select(user => user.GetFullName()).ToList();


            var (selectedPlayer, isSkip) = await WaitForVotingAsync(
                channel,
                overrideVoteTime.Value,
                options,
                displayOptions);


            role.ProcessMove(selectedPlayer, isSkip);

            var messages = role.GetMoveResultPhasesSequence();

            foreach (var (embedStyle, message) in messages)
                await channel.SendMessageAsync(embed: CreateEmbed(embedStyle, message));

            if (isNightMove)
                await channel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, "Ожидайте наступления утра"));

            if (_mafiaData.WatcherTextChannel is not null)
            {
                var str = (isSkip, selectedPlayer is null) switch
                {
                    (false, false) => $"На голосовании был выбран {selectedPlayer!.GetFullName()}",
                    (false, true) => "Никто не был выбран на голосовании",
                    _ => "Пропуск голосования"
                };

                string action = _chronology.AddAction(str, role);

                await _mafiaData.WatcherTextChannel.SendMessageAsync(embed: CreateEmbed(EmbedStyle.Information, action));
            }
        }
        else
        {
            if (role.IsAlive)
                await role.Player.SendMessageAsync("Вас охмурила путана. Развлекайтесь с ней");

            await WaitForTimerAsync(overrideVoteTime.Value);
        }

        GuildLogger.Debug(LogTemplate, nameof(DoRoleMoveAsync), $"End do {role.Name} move");
    }


    private async Task ChangeMurdersChannelPermissionsAsync(OverwritePermissions textPerms, OverwritePermissions? voicePerms)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);


        foreach (var murder in _mafiaData.Murders.Values)
        {
            if (!murder.IsAlive)
                continue;

            var player = murder.Player;

            await _mafiaData.MurderTextChannel.AddPermissionOverwriteAsync(player, textPerms);


            if (voicePerms is not OverwritePermissions perms)
                continue;

            if (_mafiaData.MurderVoiceChannel is not null)
                await _mafiaData.MurderVoiceChannel.AddPermissionOverwriteAsync(player, perms);

            if (player.VoiceChannel != null && perms.ViewChannel == PermValue.Deny)
                await player.ModifyAsync(props => props.Channel = null);
        }
    }

    private async Task ChangeGenaralChannelPermissionsAsync(OverwritePermissions textPerms, OverwritePermissions? voicePerms)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);

        await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, textPerms);


        if (_mafiaData.GeneralVoiceChannel is null || voicePerms is not OverwritePermissions perms)
            return;

        await _mafiaData.GeneralVoiceChannel.AddPermissionOverwriteAsync(_mafiaData.MafiaRole, perms);

        if (perms.ViewChannel == PermValue.Deny)
            foreach (var role in _mafiaData.AllRoles.Values)
            {
                if (!role.IsAlive)
                    continue;

                var player = role.Player;

                if (player.VoiceChannel != null)
                    await player.ModifyAsync(props => props.Channel = null);
            }
    }


    private async Task EjectPlayerAsync(IGuildUser player, bool isKill = true)
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);


        GuildLogger.Debug(LogTemplate, nameof(EjectPlayerAsync), $"Begin eject player {player.GetFullName()}");


        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Removing overwrite permissions in murder channel for player {player.GetFullName()}");

        await _mafiaData.MurderTextChannel.RemovePermissionOverwriteAsync(player);

        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Overwrite permissions removed in murder channel for player {player.GetFullName()}");


        _mafiaData.AllRoles[player].GameOver();


        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Removing _mafiaData role from player {player.GetFullName()}");

        await player.RemoveRoleAsync(_mafiaData.MafiaRole);

        GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Mafia role removed from {player.GetFullName()}");


        if (_mafiaData.PlayerRoleIds.ContainsKey(player.Id))
        {
            GuildLogger.Verbose(LogTemplate, nameof(EjectPlayerAsync), $"Add roles to player {player.GetFullName()}");

            await player.AddRolesAsync(_mafiaData.PlayerRoleIds[player.Id]);

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
        

        if (_settings.Current.ServerSubSettings.ReplyMessagesOnSetupError)
            await ReplyEmbedAsync(EmbedStyle.Error, message);

        if (_settings.Current.ServerSubSettings.AbortGameWhenError)
            throw new GameAbortedException(message, e);
    }


    private void ConfigureOverwritePermissions()
    {
        ArgumentNullException.ThrowIfNull(_mafiaData);

        _denyView = new OverwritePermissions(viewChannel: PermValue.Deny);

        _allowWrite = OverwritePermissions.DenyAll(_mafiaData.GeneralTextChannel).Modify(
           viewChannel: PermValue.Allow,
           readMessageHistory: PermValue.Allow,
           sendMessages: PermValue.Allow);

        _denyWrite = OverwritePermissions.DenyAll(_mafiaData.GeneralTextChannel).Modify(
            viewChannel: PermValue.Allow,
            readMessageHistory: PermValue.Allow);

        if (_mafiaData.GeneralVoiceChannel is not null)
            _allowSpeak = OverwritePermissions.DenyAll(_mafiaData.GeneralVoiceChannel).Modify(
                viewChannel: PermValue.Allow,
                connect: PermValue.Allow,
                useVoiceActivation: PermValue.Allow,
                speak: PermValue.Allow
                );

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
                await ReplyEmbedAsync(EmbedStyle.Error, $"Шаблон с именем **{originalTemplateName}** уже существует");

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
                await ReplyEmbedAsync(EmbedStyle.Error, "Шаблон-образец не найден");

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



            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Шаблон **{newTemplateName}** успешно клонирован из шаблона **{originalTemplateName}**");
        }


        [Command("Загрузить")]
        [Alias("згр", "з")]
        public async Task LoadTemplate(string name = MafiaSettings.DefaultTemplateName)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            var template = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Шаблон не найден");

                return;
            }

            settings.CurrentTemplateName = name;

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Шаблон **{name}** успешно загружен");
        }


        [Command("Текущий")]
        [Alias("тек", "т")]
        public async Task ShowCurrentTemplate()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            await ReplyEmbedAsync(EmbedStyle.Information, $"Текущий шаблон - **{settings.CurrentTemplateName}**");
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

            await ReplyEmbedAsync(EmbedStyle.Information, str, "Список шаблонов");
        }



        [Command("Сообщение")]
        [Alias("сбщ")]
        [Priority(-1)]
        public async Task ShowPreGameMessage()
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            await ReplyEmbedAsync(EmbedStyle.Information, settings.Current.PreGameMessage ?? "*Сообщение отсутствует*", "Сообщение перед игрой");
        }

        [Command("Сообщение")]
        [Alias("сбщ")]
        public async Task UpdatePreGameMessage([Remainder] string message)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>(false);

            var template = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            template.PreGameMessage = message;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync(EmbedStyle.Successfull, "Сообщение успешно изменено");
        }


        [Command("Имя")]
        public async Task UpdateTemplateName(string newName, string? templateName = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            templateName ??= settings.CurrentTemplateName;

            var templateToUpdate = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == templateName);


            if (templateToUpdate is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, $"Шаблон для замены имени **{templateName}** не найден");

                return;
            }

            var templateNames = await Context.Db.MafiaSettingsTemplates
                .AsNoTracking()
                .Where(t => t.MafiaSettingsId == settings.Id)
                .Select(t => t.Name)
                .ToListAsync();

            if (templateNames.Contains(newName))
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Имя шаблона уже используется");

                return;
            }


            var oldName = templateToUpdate.Name;

            templateToUpdate.Name = newName;
            settings.CurrentTemplateName = newName;

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Имя шаблона успешно изменено: **{oldName}** -> **{newName}**");
        }


        [Command("Сброс")]
        public async Task ResetTemplate(string? name = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            name ??= settings.CurrentTemplateName;

            var template = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Шаблон не найден");

                return;
            }

            template.GameSubSettings = new();
            template.ServerSubSettings = new();
            template.RolesInfoSubSettings = new();
            template.RoleAmountSubSettings = new();

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Шаблон **{name}** успешно сброшен");
        }


        [Command("Удалить")]
        public async Task DeleteTemplate(string? name = null)
        {
            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            name ??= settings.CurrentTemplateName;

            if (name == MafiaSettings.DefaultTemplateName)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Невозможно удалить шаблон по умолчанию");

                return;
            }

            if (name == settings.CurrentTemplateName)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Невозможно удалить активный шаблон");

                return;
            }


            var template = Context.Db.MafiaSettingsTemplates.FirstOrDefault(s => s.MafiaSettingsId == settings.Id && s.Name == name);

            if (template is null)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Шаблон не найден");

                return;
            }


            Context.Db.MafiaSettingsTemplates.Remove(template);

            await Context.Db.SaveChangesAsync();


            await ReplyEmbedAsync(EmbedStyle.Successfull, $"Шаблон **{name}** успешно удален");
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


        [Command("Общие")]
        [Alias("о")]
        public async Task SetGeneralSettingsAsync()
        {
            var settingsVM = new MafiaSettingsViewModel();

            var success = await TrySetParameters(settingsVM);


            if (!success)
            {
                await ReplyEmbedAsync(EmbedStyle.Warning, "Общие настройки мафии не были сохранены");

                return;
            }

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            SetParameters(settings, settingsVM);

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Общие настройки мафии успешно сохранены");
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


            await ReplyEmbedStampAsync(EmbedStyle.Successfull, "Автонастройка успешно завершена");


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


            await ReplyEmbedStampAsync(EmbedStyle.Successfull, "Общие настройки успешно сброшены");
        }


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

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();
            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);

            var roleAmountSettings = settings.Current.RoleAmountSubSettings;
            SetParameters(roleAmountSettings, settingsVM);
            settings.Current.RoleAmountSubSettings = roleAmountSettings;


            if (settings.Current.RoleAmountSubSettings.MurdersCount == 0 && settings.Current.RoleAmountSubSettings.DonsCount == 0)
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

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current ??= Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var rolesSettings = settings.Current.RolesInfoSubSettings;
            SetParameters(rolesSettings, settingsVM);
            settings.Current.RolesInfoSubSettings = rolesSettings;


            var MurdersKnowEachOther = settings.Current.RolesInfoSubSettings.MurdersKnowEachOther;
            var MurdersVoteTogether = settings.Current.RolesInfoSubSettings.MurdersVoteTogether;

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

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current ??= Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var serverSettings = settings.Current.ServerSubSettings;
            SetParameters(serverSettings, serverSettingsVM);
            settings.Current.ServerSubSettings = serverSettings;


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

            var settings = await Context.GetGameSettingsAsync<MafiaSettings>();

            settings.Current = Context.Db.MafiaSettingsTemplates.First(s => s.MafiaSettingsId == settings.Id && s.Name == settings.CurrentTemplateName);


            var gameSettings = settings.Current.GameSubSettings;
            SetParameters(gameSettings, settingsVM);
            settings.Current.GameSubSettings = gameSettings;


            if (settings.Current.GameSubSettings.MafiaCoefficient <= 1)
            {
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, "Коэффиент мафии не может быть меньше 2. Установлено стандартное значение **3**");

                settings.Current.GameSubSettings = gameSettings with
                {
                    MafiaCoefficient = 3
                };
            }

            if (settings.Current.GameSubSettings.VoteTime <= 0)
            {
                await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, "Установлено стандартное значение времени голосования: **40**");

                settings.Current.GameSubSettings = gameSettings with
                {
                    VoteTime = 40
                };
            }

            await Context.Db.SaveChangesAsync();

            await ReplyEmbedAsync(EmbedStyle.Successfull, "Настройки сервера успешно сохранены");
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


            await ReplyEmbedStampAsync(EmbedStyle.Information, message, "Проверка настроек");


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


        private async Task<bool> TrySetParameters<T>(T settings) where T : notnull
        {
            var wasSetSettings = false;


            var parameters = settings.GetType().GetProperties().Where(p => p.CanWrite).ToList();

            var parameterNames = GetPropertiesName(parameters).ToList();

            var emotes = GetEmotesList(parameterNames.Count, parameterNames, out var text);


            while (true)
            {
                var message = await ReplyEmbedAsync(EmbedStyle.Information, $"Выберите интересующий вас параметр\n{text}");


                var reactionResult = await NextReactionAsync(message, TimeSpan.FromSeconds(30), emotes, true);


                if (!reactionResult.IsSuccess)
                {
                    await ReplyEmbedAsync(EmbedStyle.Warning, "Вы не выбрали параметр");

                    break;
                }

                if (reactionResult?.Value.Emote is not IEmote selectedEmote)
                {
                    await ReplyEmbedAsync(EmbedStyle.Warning, "Неверный параметр");

                    break;
                }

                if (selectedEmote.Name == CancelEmote.Name)
                {
                    await ReplyEmbedAsync(EmbedStyle.Information, "Вы отменили выбор");

                    break;
                }

                var index = emotes.IndexOf(selectedEmote);


                if (index == -1 || index > parameters.Count)
                {
                    await ReplyEmbedAsync(EmbedStyle.Error, "Параметр не найден");

                    break;
                }


                var embed = CreateEmbed(EmbedStyle.Information, $"Напишите значение выбранного параметра **{parameterNames[index]}**");
                var valueMessageResult = await NextMessageAsync(embed: embed);

                if (!valueMessageResult.IsSuccess)
                {
                    await ReplyEmbedAndDeleteAsync(EmbedStyle.Warning, $"Вы не указали значение параметра **{parameterNames[index]}**");

                    break;
                }

                if (valueMessageResult?.Value is not SocketMessage valueMessage)
                {
                    await ReplyEmbedAsync(EmbedStyle.Warning, "Неверное значение параметра");

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


    public class MafiaHelpModule : HelpModule
    {
        public MafiaHelpModule(InteractiveService interactiveService, IConfiguration config) : base(interactiveService, config)
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
                .WithInformationMessage();

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





    private class MafiaData
    {
        public Dictionary<IGuildUser, GameRole> AllRoles { get; }

        public Dictionary<IGuildUser, Innocent> Innocents { get; }

        public Dictionary<IGuildUser, Doctor> Doctors { get; }

        public Dictionary<IGuildUser, Sheriff> Sheriffs { get; }

        public Dictionary<IGuildUser, Murder> Murders { get; }

        public Dictionary<IGuildUser, Don> Dons { get; }

        public Dictionary<IGuildUser, Neutral> Neutrals { get; }

        public Dictionary<IGuildUser, Maniac> Maniacs { get; }

        public Dictionary<IGuildUser, Hooker> Hookers { get; }




        //public MafiaStatsHelper MafiaStatsHelper { get; }

        public Dictionary<ulong, List<ulong>> PlayerRoleIds { get; }

        public List<ulong> OverwrittenNicknames { get; }



        public List<IGuildUser> KilledPlayers { get; }


        public ITextChannel GeneralTextChannel { get; }
        public ITextChannel MurderTextChannel { get; }
        public ITextChannel? WatcherTextChannel { get; }

        public IVoiceChannel? GeneralVoiceChannel { get; }
        public IVoiceChannel? MurderVoiceChannel { get; }
        public IVoiceChannel? WatcherVoiceChannel { get; }


        public IRole MafiaRole { get; }
        public IRole? WatcherRole { get; }


        public bool IsFirstNight { get; set; }

        public int VoteTime { get; }


        public MafiaData(ITextChannel generalTextChannel,
                         ITextChannel murderTextChannel,
                         ITextChannel? watcherTextChannel,
                         IVoiceChannel? generalVoiceChannel,
                         IVoiceChannel? murderVoiceChannel,
                         IVoiceChannel? watcherVoiceChannel,
                         IRole mafiaRole,
                         IRole? watcherRole,
                         int voteTime)
        {
            VoteTime = voteTime;

            AllRoles = new();

            Innocents = new();
            Doctors = new();
            Sheriffs = new();

            Murders = new();
            Dons = new();

            Neutrals = new();
            Maniacs = new();
            Hookers = new();

            // MafiaStatsHelper = new(this);

            PlayerRoleIds = new();

            OverwrittenNicknames = new();


            KilledPlayers = new();


            IsFirstNight = true;

            GeneralTextChannel = generalTextChannel;
            MurderTextChannel = murderTextChannel;
            WatcherTextChannel = watcherTextChannel;

            GeneralVoiceChannel = generalVoiceChannel;
            MurderVoiceChannel = murderVoiceChannel;
            WatcherVoiceChannel = watcherVoiceChannel;

            MafiaRole = mafiaRole;
            WatcherRole = watcherRole;
        }
    }


    private class MafiaChronology : Chronology<StringChronology>
    {
        private int _currentDay;


        public MafiaChronology()
        {
            _currentDay = -1;
        }


        public string AddAction(string action, GameRole role)
        {
            var str = role is not RolesGroup
                ? $"[{role.Name}] {role.Player.GetFullMention()}: **{action}**"
                : $"{role.Name}: **{action}**";

            Actions[_currentDay].AddAction(str);

            return str;
        }

        public void NextDay()
        {
            AddAction(new());
            _currentDay++;
        }
    }
}