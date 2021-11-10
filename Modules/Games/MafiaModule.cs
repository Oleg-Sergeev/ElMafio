using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Infrastructure.Data;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Stats;
using Infrastructure.Data.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Extensions;
using Serilog;

namespace Modules.Games;

[Group("Мафия")]
[Alias("маф", "м")]
public class MafiaModule : GameModule
{
    protected const string LogTemplate = "({Context:l}): {Message}";


    protected ILogger? GuildLogger { get; private set; }


    private MafiaData? _mafiaData;

    private MafiaSettings _settings = null!;


    private OverwritePermissions _allowWrite;
    private OverwritePermissions _denyWrite;
    private OverwritePermissions _allowSpeak;
    private OverwritePermissions _denySpeak;



    public MafiaModule(Random random, BotContext db, IConfiguration config) : base(random, db, config)
    {

    }

    [Group("Настройки")]
    [Alias("н")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Настройки для мафии включают в себя настройки сервера(используемые роли, каналы и категорию каналов) и настройки самой игры. " +
        "Для подробностей введите команду **Мафия.Настройки.Помощь**")]
    public class Settings : GuildModuleBase
    {
        private readonly BotContext _db;

        public Settings(BotContext db)
        {
            _db = db;
        }


        [Command("Параметры")]
        [Alias("парам", "пар", "п")]
        [Summary("Настройка параметров игры")]
        [Remarks("`Чтобы настроить игру, необходимо указать все параметры." +
            "\nПараметры могут быть числами, строками, логическими значениями. Тип параметра указан в квадратных [] скобках около названия параметра." +
            "\nПример ввода типов данных: [int] - 25; [float] - 1.23; [string] - \"я строка\"; [bool] - истина: true/+/да/yes/y, ложь: false/-/нет/no/n, н/д: null/x/=" +
            "\nПример Мафия.Настройки.Параметры 3 = + + - - назначит расчетный коэфф. равным 3, оставит без изменений рейтинговую игру, включит добавление серверного ника " +
            "и отправление сообщений при ошибках, также выключит прерывание игры при ошибках и отправление приветственного сообщения`")]
        public async Task SetSettingsAsync(
            [Summary("Список параметров игры:" +
                "\n **Расчетный коэффициент K [int]** (3 ≤ X) – определяет кол-ло мафиози **M** при **N** игроках по формуле **M = N / K**" +
                "\n **Рейтинговая игра [bool]** (+/-/=) – при включенном параметре результаты игры будут влиять на рейтинг игрока, иначе результаты игры не будут влиять на статистику игрока" +
                "\n **Добавлять игрокам серверный ник [bool]** (+/-/=) – устанавливать никнейм игроку, если отсутствует серверный никнейм" +
                "\n **Отправлять сообщения при ошибках [bool]** (+/-/=) – отправлять в чат сообщения об ошибках во время игры" +
                "\n **Прервать игру при ошибке [bool]** (+/-/=) – прерывать игру при возникновении ошибки. (Примечание: критические ошибки будут прерывать игру вне зависимости от данного параметра" +
                "\n **Приветственное сообщение [bool]** (+/-/=) – отправлять всем игрокам приветственное сообщение перед началом игры")]
            [Remainder] MafiaSettingsViewModel mafiaSettings)
        {
            if (mafiaSettings.MafiaKoefficient < 3)
            {
                await ReplyAsync("Расчетный коэффициент не может быть меньше 3");

                return;
            }

            var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

            if (mafiaSettings.MafiaKoefficient is not null)
                settings.MafiaKoefficient = mafiaSettings.MafiaKoefficient.Value;
            if (mafiaSettings.IsRatingGame is not null)
                settings.IsRatingGame = mafiaSettings.IsRatingGame.Value;
            if (mafiaSettings.RenameUsers is not null)
                settings.RenameUsers = mafiaSettings.RenameUsers.Value;
            if (mafiaSettings.ReplyMessagesOnError is not null)
                settings.ReplyMessagesOnError = mafiaSettings.ReplyMessagesOnError.Value;
            if (mafiaSettings.AbortGameWhenError is not null)
                settings.AbortGameWhenError = mafiaSettings.AbortGameWhenError.Value;
            if (mafiaSettings.SendWelcomeMessage is not null)
                settings.SendWelcomeMessage = mafiaSettings.SendWelcomeMessage.Value;

            await _db.SaveChangesAsync();


            await ReplyEmbedAsync(EmbedType.Successfull, "Параметры успешно установлены");
        }


        [Priority(0)]
        [Command("Каналы")]
        [Summary("Настроить используемые каналы для игры")]
        [Remarks("Для указания ссылки на голосовой канал, используйте следующий шаблон: <#VoiceChannelId>")]
        public async Task SetSettingsAsync(
            [Summary("Главный канал игры, в котором происходит дневное голосование и объявление результатов прошедшей ночи")] ITextChannel generalTextChannel,
            [Summary("Канал для убийц, в котором обсуждаются планы на следующую жертву и непосредственно само голосование")] ITextChannel murderTextChannel,
            [Summary("Голосовой канал для всех игроков, доступен только днем во время обсуждения")] IVoiceChannel generalVoiceChannel,
            [Summary("Голосовой канал для убийц, доступен только ночью")] IVoiceChannel murderVoiceChannel)
            => await SetSettingsAsync(generalTextChannel.Id, murderTextChannel.Id, generalVoiceChannel.Id, murderVoiceChannel.Id);

        [Priority(1)]
        [Command("Каналы")]
        [Summary("Настроить используемые каналы для игры")]
        [Remarks("Чтобы ввести ID категории канала, нажмите по нужной категории правой кнопкой мыши (ПК), или зажмите пальцем канал (Моб.), " +
                 "а затем нажмите кнопку **Скопировать ID**")]
        public async Task SetSettingsAsync(
            [Summary("Главный канал игры, в котором происходит дневное голосование и объявление результатов прошедшей ночи")] ulong generalTextChannelId,
            [Summary("Канал для убийц, в котором обсуждаются планы на следующую жертву и непосредственно само голосование")] ulong murderTextChannelId,
            [Summary("Голосовой канал для всех игроков, доступен только днем во время обсуждения")] ulong generalVoiceChannelId,
            [Summary("Голосовой канал для убийц, доступен только ночью")] ulong murderVoiceChannelId)

        {
            var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

            settings.GeneralTextChannelId = generalTextChannelId;
            settings.MurdersTextChannelId = murderTextChannelId;
            settings.GeneralVoiceChannelId = generalVoiceChannelId;
            settings.MurdersVoiceChannelId = murderVoiceChannelId;

            await _db.SaveChangesAsync();

            await ReplyAsync("Каналы успешно установлены");
        }


        [Priority(0)]
        [Command("Роли")]
        [Summary("Настроить параметры игры")]
        [Remarks("Чтобы указать нужную роль, введите **@** и выберите необходимую роль")]
        public async Task SetSettingsAsync(
            [Summary("Роль, выдываваемая каждому игроку мафии")] IRole mafiaRole,
            [Summary("Роль, выдываваемая игроку, убитому во время игры, чтобы он мог продолжать наблюдать за игрой")] IRole watcherRole)
            => await SetSettingsAsync(mafiaRole.Id, watcherRole.Id);

        [Priority(1)]
        [Command("Роли")]
        [Summary("Настроить параметры игры")]
        [Remarks("Чтобы указать ID роли, выберите роль с помощью **@** и добавьте перед роль обратную косую черту \\\\; " +
                 "либо зайдите в настройки ролей, найдите нужную, нажмите кнопку **Еще** и нажмите **Скопировать ID**")]
        public async Task SetSettingsAsync(
            [Summary("Роль, выдываваемая каждому игроку мафии")] ulong mafiaRoleId,
            [Summary("Роль, выдываваемая игроку, убитому во время игры, чтобы он мог продолжать наблюдать за игрой")] ulong watcherRoleId)
        {
            var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

            settings.MafiaRoleId = mafiaRoleId;
            settings.WatcherRoleId = watcherRoleId;

            await _db.SaveChangesAsync();

            await ReplyAsync("Роли успешно установлены");
        }

        [Priority(1)]
        [Command("Категория")]
        [Summary("Настроить параметры игры")]
        [Remarks("Чтобы ввести ID категории канала, нажмите по нужной категории правой кнопкой мыши (ПК), или зажмите пальцем канал (Моб.), " +
                 "а затем нажмите кнопку **Скопировать ID**")]
        public async Task SetSettingsAsync([Summary("Категория каналов, в которой будут находиться игровые каналы")] ulong categoryChannelId)
        {
            var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

            settings.CategoryChannelId = categoryChannelId;

            await _db.SaveChangesAsync();


            await ReplyAsync("Категория успешно установлена");
        }




        [Command("Текущие")]
        [Alias("тек")]
        [Summary("Показать текущие настройки игры для этого сервера")]
        public async Task ShowSettingsAsync()
        {
            var settings = await GetSettingsAsync<MafiaSettings>(_db, Context.Guild.Id);

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Текущие настройки игры **Мафия**")
                .AddField("Расчетный коэффициент", settings.MafiaKoefficient, true)
                .AddField("Рейтинговая игра", settings.IsRatingGame, true)
                .AddField("Добавлять игрокам серверный ник", settings.RenameUsers, true)
                .AddField("Отправлять сообщения при ошибках", settings.ReplyMessagesOnError, true)
                .AddField("Прервать игру при ошибке", settings.AbortGameWhenError, true)
                .AddField("Приветственное сообщение", settings.SendWelcomeMessage, true)
                .WithInformationMessage(false)
                .WithUserInfoFooter(Context.User)
                .WithCurrentTimestamp();


            await ReplyAsync(embed: embedBuilder.Build());
        }
    }



    protected static ILogger GetGuildLogger(ulong guildId)
        => Log.ForContext("GuildName", guildId);



    protected override GameModuleData CreateGameData(IGuildUser creator)
        => new("Мафия", 3, creator);



    public override async Task ResetStatAsync(IGuildUser guildUser)
        => await ResetStatAsync<MafiaStats>(guildUser);


    public override async Task ShowStatsAsync()
        => await ShowStatsAsync(Context.User);

    public override async Task ShowStatsAsync(IUser user)
    {
        var userStat = await Db.MafiaStats
            .AsNoTracking()
            .Include(stat => stat.User)
            .FirstOrDefaultAsync(stat => stat.UserId == user.Id);

        if (userStat is null)
        {
            await ReplyAsync("Статистика отсутствует");

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
        .AddField("Победы за красные роли", $"{userStat.WinRate:F2}%", true)
        .AddField("Победы за черные роли", $"{userStat.WinRate:F2}%", true)
        .AddField("Суммарные победы", $"{(userStat.WinRate / 2 + userStat.BlacksWinRate / 2):F2}%", true)
        .AddField("Эффективность доктора", $"{userStat.DoctorEfficiency:F2}%", true)
        .AddField("Эффективность шерифа", $"{userStat.CommissionerEfficiency:F2}%", true)
        .AddField("Эффективность дона", $"{userStat.DonEfficiency:F2}%", true)
        .AddField("Кол-во основных очков", $"{userStat.Scores:F2}%", true)
        .AddField("Кол-во доп. очков", $"{userStat.ExtraScores:F2}%", true)
        .AddField("Кол-во штрафных очков", $"{userStat.PenaltyScores:F2}%", true)
        .AddEmptyField(true)
        .AddField("Рейтинг", $"{userStat.Rating:F2} баллов")
        .WithCurrentTimestamp();

        await ReplyAsync(embed: embedBuilder.Build());
    }

    public override async Task ShowRating()
    {
        var allStats = await Db.MafiaStats
            .AsNoTracking()
            .Where(s => s.GuildId == Context.Guild.Id && s.Rating > 0)
            .OrderByDescending(stat => stat.Rating)
            .ThenByDescending(stat => stat.WinRate + stat.BlacksWinRate)
            .ThenBy(stat => stat.GamesCount)
            .Include(stat => stat.User)
            .ToListAsync();


        if (allStats.Count == 0)
        {
            await ReplyAsync("Рейтинг отсутствует");

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

        var builder = new EmbedBuilder()
        {
            Title = "Рейтинг мафии",
            Color = Color.Gold
        };

        var message = "";
        for (int i = 0; i < allStats.Count; i++)
        {
            if (!players.TryGetValue(allStats[i].UserId, out var user))
                continue;

            message += $"{i + 1}. **{user.GetFullName()}** – {allStats[i].Rating:F2}\n";
        }

        builder.WithDescription(message);

        await ReplyAsync(embed: builder.Build());
    }

    public override Task ResetRatingAsync()
        => ResetRatingAsync<MafiaStats>();


    public override async Task StartAsync()
    {
        GuildLogger = GetGuildLogger(Context.Guild.Id);

        GameData = GetGameData();

        if (!CanStart(out var msg))
        {
            await ReplyAsync(msg);

            return;
        }

        GameData!.IsPlaying = true;


        GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game starting...");

        await ReplyAsync($"{GameData.Name} начинается!");


        _settings = await GetSettingsAsync<MafiaSettings>(Db, Context.Guild.Id);

        _mafiaData = await PreSetupGuildAsync();

        _mafiaData.AlivePlayers.AddRange(GameData!.Players);

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


            var isMafiaWon = _mafiaData.Murders.Count > 0;

            await FinishAsync(isMafiaWon);


            if (GameData.IsPlaying)
            {
                await ReplyAsync($"{(isMafiaWon ? "Мафия победила!" : "Мирные жители победили!")} Благодарим за участие!");

                await ReplyAsync($"Участники и их роли:\n{_mafiaData.PlayerGameRoles}");
            }


            if (GameData.IsPlaying && _settings.IsRatingGame)
                await SaveStatsAsync();

            await ReplyAsync("Игра завершена");

            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Game finished");
        }
        catch (GameAbortedException e)
        {
            GuildLogger.Debug(e, "Game was stopped");

            await ReplyAsync($"Игра остановлена. Причина: {e.Message}");

            await AbortAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{Context.Guild.Name} {Context.Guild.Id}] Game was aborted");
            GuildLogger.Error(e, "Game was aborted");

            await ReplyAsync("Игра аварийно прервана");

            await AbortAsync();

            throw;
        }
        finally
        {
            if (_settings.GeneralTextChannelId != _mafiaData.GeneralTextChannel.Id)
                await SetAndSaveSettingsToDbAsync();


            DeleteGameData();

            GuildLogger.Debug(LogTemplate, nameof(StartAsync), "Gamedata deleted");
        }
    }


    private void SetCategoryChannel(GuildChannelProperties props)
        => props.CategoryId = _settings.CategoryChannelId;

    private async Task<MafiaData> PreSetupGuildAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(PreSetupGuildAsync), "Begin presetup guild...");

        int messagesToDelete = Context.Guild.CurrentUser.GuildPermissions.ManageMessages ? 500 : 0;

        var mafiaData = new MafiaData(await Context.Guild.GetTextChannelOrCreateAsync(_settings.GeneralTextChannelId, "мафия-общий", messagesToDelete, SetCategoryChannel),
               await Context.Guild.GetTextChannelOrCreateAsync(_settings.MurdersTextChannelId, "мафия-убийцы", messagesToDelete, SetCategoryChannel),
               await Context.Guild.GetVoiceChannelOrCreateAsync(_settings.GeneralVoiceChannelId, "мафия-общий", SetCategoryChannel),
               await Context.Guild.GetVoiceChannelOrCreateAsync(_settings.MurdersVoiceChannelId, "мафия-убийцы", SetCategoryChannel),
               await Context.Guild.GetRoleOrCreateAsync(_settings.MafiaRoleId, "Игрок мафии", null, Color.Blue, true, true),
               await Context.Guild.GetRoleOrCreateAsync(_settings.WatcherRoleId, "Наблюдатель мафии", null, Color.DarkBlue, true, true));

        GuildLogger.Debug(LogTemplate, nameof(PreSetupGuildAsync), "End presetup guild");

        return mafiaData;
    }


    private async Task SetupGuildAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(SetupGuildAsync), "Begin setup guild...");

        await ReplyAsync("Подготавливаем сервер...");


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

        await ReplyAsync("Собираем досье на игроков...");

        foreach (var player in GameData!.Players)
        {
            if (_settings.SendWelcomeMessage)
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

                    await AbortAsync();

                    throw;
                }
            }

            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Removing overwrite permissions from murder text channel");

            await _mafiaData!.MurderTextChannel.RemovePermissionOverwriteAsync(player);

            GuildLogger.Verbose(LogTemplate, nameof(SetupPlayersAsync), "Overwrite permissions removed from murder text channel");

            _mafiaData.PlayerRoles.Add(player.Id, new List<IRole>());

            _mafiaData.PlayerStats.Add(player.Id, new MafiaStats
            {
                UserId = player.Id
            });


            var guildPlayer = (SocketGuildUser)player;

            if (_settings.RenameUsers && guildPlayer.Id != Context.Guild.OwnerId && guildPlayer.Nickname is null)
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

        await ReplyAsync("Выдаем игрокам роли...");

        GameData!.Players.Shuffle();

        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Setuping red roles");

        var count = GameData.Players.Count;
        var sheriffIndex = Random.Next(count);

        var doctorIndex = sheriffIndex == count - 1
            ? sheriffIndex - 1 - Random.Next(sheriffIndex)
            : sheriffIndex + 1 + Random.Next(count - sheriffIndex - 1);

        _mafiaData!.Commissioner = GameData.Players[sheriffIndex];
        _mafiaData.Doctor = GameData.Players[doctorIndex];

        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Red roles setuped");


        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Setuping black roles");

        var otherPlayers = GameData.Players.Except(new List<IGuildUser>() { _mafiaData.Commissioner, _mafiaData.Doctor }).ToList();

        for (int i = 0; i < GameData.Players.Count / _settings.MafiaKoefficient; i++)
        {
            _mafiaData.Murders.Add(otherPlayers[i]);

            _mafiaData.PlayerStats[otherPlayers[i].Id].BlacksGamesCount++;
        }

        if (_mafiaData.Murders.Count >= 3)
        {
            _mafiaData.Has3MurdersInGame = true;

            _mafiaData.Don = _mafiaData.Murders[Random.Next(_mafiaData.Murders.Count)];
        }

        GuildLogger.Verbose(LogTemplate, nameof(SetupRolesAsync), "Black roles setuped");

        GuildLogger.Debug(LogTemplate, nameof(SetupRolesAsync), "End setup roles");
    }

    private async Task NotifyPlayersAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(NotifyPlayersAsync), "Begin notify players");

        foreach (var player in GameData!.Players)
        {
            var text = "Вы - Мирный житель. Ваша цель - вычислить и изгнать убийц";
            var role = "Мирный житель";

            if (_mafiaData!.Murders.Contains(player))
            {
                if (_mafiaData.Don is null || _mafiaData.Don.Id != player.Id)
                {
                    role = "Мафия";
                    text = "Ваша роль - Мафия. Ваша цель - убить всех мирных жителей";
                }
                else
                {
                    role = "Дон";
                    text = "Ваша роль - Дон. Ваша цель - вычислить шерифа и с остальными мафиози убить всех мирных жителей";
                }
            }
            else
            {
                if (player == _mafiaData.Doctor)
                {
                    role = "Доктор";
                    text = "Ваша роль - Доктор. Ваша цель - помочь жителям победить, леча каждую ночь одного из них, или себя";
                }
                else if (player == _mafiaData.Commissioner)
                {
                    role = "Шериф";
                    text = "Ваша роль - Шериф. Ваша цель - помочь жителям победить, узнавая роль определенного жителя каждую ночь";
                }
            }


            _mafiaData.PlayerGameRoles += $"{player.GetFullName()} - {role}\n";


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


    private async Task AbortAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(AbortAsync), "Begin abort game...");


        await Context.Guild.DownloadUsersAsync();

        for (int i = _mafiaData!.AlivePlayers.Count - 1; i >= 0; i--)
        {
            var player = _mafiaData.AlivePlayers[i];

            var playerExistInGuild = Context.Guild.GetUser(player.Id) is not null;
            if (!playerExistInGuild)
            {
                GuildLogger.Debug(LogTemplate, nameof(EjectPlayerAsync), $"Player {player.GetFullName()} does not exists in guild");

                continue;
            }

            try
            {
                await EjectPlayerAsync(player, false);
            }
            catch (Exception e)
            {
                GuildLogger.Debug(e, LogTemplate, nameof(EjectPlayerAsync), $"Failed to revert guild data of player {player.GetFullName()}");

                continue;
            }
        }

        foreach (var player in _mafiaData.KilledPlayers)
        {
            var playerExistInGuild = Context.Guild.GetUser(player.Id) is not null;
            if (!playerExistInGuild)
            {
                GuildLogger.Debug(LogTemplate, nameof(EjectPlayerAsync), $"Player {player.GetFullName()} does not exists in guild");

                continue;
            }


            GuildLogger.Verbose(LogTemplate, nameof(AbortAsync), $"Removing watcher role from user {player.GetFullName()}");

            try
            {
                await player.RemoveRoleAsync(_mafiaData.WatcherRole);
            }
            catch (Exception e)
            {
                GuildLogger.Debug(e, LogTemplate, nameof(EjectPlayerAsync), $"Failed to remove watcher role from player {player.GetFullName()}");

                continue;
            }

            GuildLogger.Verbose(LogTemplate, nameof(AbortAsync), $"Watcher role removed from user {player.GetFullName()}");
        }


        GuildLogger.Debug(LogTemplate, nameof(AbortAsync), "End abort game");
    }

    private async Task FinishAsync(bool isMafiaWon)
    {
        GuildLogger!.Debug(LogTemplate, nameof(FinishAsync), "Begin finish game...");

        for (int i = _mafiaData!.AlivePlayers.Count - 1; i >= 0; i--)
        {
            var player = _mafiaData.AlivePlayers[i];


            if (GameData!.IsPlaying)
            {
                if (_mafiaData.Murders.Contains(player))
                {
                    if (isMafiaWon)
                    {
                        _mafiaData.PlayerStats[player.Id].BlacksWinsCount++;
                        _mafiaData.PlayerStats[player.Id].ExtraScores += 0.5f;
                    }
                }
                else if (!isMafiaWon)
                    _mafiaData.PlayerStats[player.Id].WinsCount++;
            }

            await EjectPlayerAsync(player, false);
        }

        foreach (var player in _mafiaData.KilledPlayers)
        {
            GuildLogger.Verbose(LogTemplate, nameof(FinishAsync), $"Removing watcher role from user {player.GetFullName()}");

            await player.RemoveRoleAsync(_mafiaData.WatcherRole);

            GuildLogger.Verbose(LogTemplate, nameof(FinishAsync), $"Watcher role removed from user {player.GetFullName()}");


            if (GameData!.IsPlaying)
            {
                if (_mafiaData.KilledMurders.Contains(player))
                {
                    if (isMafiaWon)
                    {
                        _mafiaData.PlayerStats[player.Id].BlacksWinsCount++;
                        _mafiaData.PlayerStats[player.Id].ExtraScores += 0.5f;
                    }
                }
                else if (!isMafiaWon)
                    _mafiaData.PlayerStats[player.Id].WinsCount++;
            }
        }

        GuildLogger.Debug(LogTemplate, nameof(FinishAsync), "End finish game");
    }

    private async Task PlayAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(PlayAsync), "Begin playing game...");

        await ChangePermissionsGenaralChannelsAsync(_denyWrite, _denySpeak);

        await _mafiaData!.GeneralTextChannel.SendMessageAsync($"Добро пожаловать в мафию! Сейчас ночь, весь город спит, а мафия знакомится в отдельном чате.");

        await _mafiaData.GeneralTextChannel.SendMessageAsync($"Количество мафиози - {_mafiaData.Murders.Count}");

        if (_mafiaData.Murders.Count > 1)
        {
            var meetTime = _mafiaData.Murders.Count * 10;

            await ChangePermissionsMurderChannelsAsync(_allowWrite, _allowSpeak);

            await _mafiaData.MurderTextChannel.SendMessageAsync("Добро пожаловать в мафию! Сейчас ночь, весь город спит, самое время познакомиться с остальными мафиозниками");


            var murdersList = "";
            foreach (var murder in _mafiaData.Murders)
                murdersList += $"{murder.GetFullName()}\n";

            await _mafiaData.MurderTextChannel.SendMessageAsync($"Список мафиози:\n{murdersList}");

            if (_mafiaData.Don is not null)
                await _mafiaData.MurderTextChannel.SendMessageAsync($"Ваш дон: {_mafiaData.Don.GetFullName()}");


            await Task.Delay(meetTime * 1000);

            await WaitTimerAsync(meetTime, _mafiaData.GeneralTextChannel, _mafiaData.MurderTextChannel);

            await _mafiaData.MurderTextChannel.SendMessageAsync("Время вышло! Переходите в общий канал и старайтесь не подавать виду, что вы мафиозник.");
        }


        var nightMurderVoteTime = 30;
        var nightInnocentVoteTime = 40 + _mafiaData.Murders.Count * 10;
        var nightTime = 30 + _mafiaData.Murders.Count * 5;
        var dayVoteTime = 30;

        while (GameData!.IsPlaying && _mafiaData.Murders.Count > 0 && _mafiaData.AlivePlayers.Count > 2 * _mafiaData.Murders.Count)
        {
            try
            {
                var dayTime = _mafiaData.AlivePlayers.Count * 20;

                await ChangePermissionsMurderChannelsAsync(_denyWrite, _denySpeak);

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"{_mafiaData.MafiaRole.Mention} Доброе утро, жители города! Самое время пообщаться всем вместе.");

                if (!_mafiaData.IsFirstNight)
                {
                    var wasCommissionerShot = _mafiaData.CommissionerMove is not null;
                    var wasMurderShot = _mafiaData.MurdersMove is not null;

                    var wasMurderKill = wasMurderShot && _mafiaData.MurdersMove != _mafiaData.DoctorMove;
                    var wasCommissionerKill = wasCommissionerShot && _mafiaData.CommissionerMove != _mafiaData.DoctorMove;


                    var savedFromMurder = _mafiaData.Doctor is not null && wasMurderShot && !wasMurderKill;
                    var savedFromCommissioner = _mafiaData.Doctor is not null && wasCommissionerShot && !wasCommissionerKill;

                    switch (savedFromMurder, savedFromCommissioner)
                    {
                        case (true, true):
                            _mafiaData.PlayerStats[_mafiaData.Doctor!.Id].DoctorSuccessfullMovesCount++;
                            _mafiaData.PlayerStats[_mafiaData.Doctor.Id].ExtraScores++;
                            break;

                        case (true, false):
                            _mafiaData.PlayerStats[_mafiaData.Doctor!.Id].DoctorSuccessfullMovesCount++;
                            if (_mafiaData.MurdersMove == _mafiaData.Commissioner)
                                _mafiaData.PlayerStats[_mafiaData.Doctor!.Id].ExtraScores += 0.5f;
                            break;

                        case (false, true):
                            if (!_mafiaData.Murders.Contains(_mafiaData.DoctorMove!))
                                _mafiaData.PlayerStats[_mafiaData.Doctor!.Id].DoctorSuccessfullMovesCount++;
                            break;

                        default:
                            break;
                    }

                    if (_mafiaData.Has3MurdersInGame && _mafiaData.Commissioner is not null && wasCommissionerKill)
                    {
                        if (_mafiaData.Murders.Contains(_mafiaData.CommissionerMove!))
                        {
                            _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].CommissionerSuccessfullMovesCount++;
                            _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].ExtraScores++;
                        }
                        else if (_mafiaData.Doctor == _mafiaData.CommissionerMove)
                            _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].PenaltyScores += 2;
                        else
                            _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].PenaltyScores++;
                    }


                    await _mafiaData.GeneralTextChannel.SendMessageAsync($"Но сначала новости: сегодня утром, в незаправленной постели...");

                    await Task.Delay(2500);

                    var deadBodies = new List<IGuildUser>();

                    if (wasMurderKill)
                        deadBodies.Add(_mafiaData.MurdersMove!);

                    if (wasCommissionerKill)
                        deadBodies.Add(_mafiaData.CommissionerMove!);

                    deadBodies = deadBodies.Distinct().ToList();

                    deadBodies.Shuffle();

                    var msg = deadBodies.Count switch
                    {
                        0 => "Никого не оказалось. Все живы.",
                        1 => $"Был обнаружен труп {(wasMurderKill ? _mafiaData.MurdersMove!.Mention : _mafiaData.CommissionerMove!.Mention)}",
                        2 => $"Были обнаружены трупы {deadBodies[0]!.Mention} и {deadBodies[1]!.Mention}",
                        _ => throw new GameAbortedException("Неожиданное количество трупов (> 3).")
                    };

                    await _mafiaData.GeneralTextChannel.SendMessageAsync(msg);


                    if (wasMurderKill)
                        await EjectPlayerAsync(_mafiaData.MurdersMove!);
                    if (wasCommissionerKill)
                        await EjectPlayerAsync(_mafiaData.CommissionerMove!);

                    if (wasCommissionerShot)
                    {
                        _mafiaData.HasCommissionerShot = false;
                        _mafiaData.CommissionerMove = null;
                    }

                    if (_mafiaData.Murders.Count == 0 || _mafiaData.AlivePlayers.Count <= 2 * _mafiaData.Murders.Count)
                        break;
                }

                if (!GameData.IsPlaying)
                    break;

                await _mafiaData.GeneralTextChannel.SendMessageAsync($"Обсуждайте. ({dayTime}с)");

                await ChangePermissionsGenaralChannelsAsync(_allowWrite, _allowSpeak);

                await WaitTimerAsync(dayTime, _mafiaData.GeneralTextChannel);

                await ChangePermissionsGenaralChannelsAsync(_denyWrite, _denySpeak);

                if (!GameData.IsPlaying)
                    break;

                if (!_mafiaData.IsFirstNight)
                {
                    await _mafiaData.GeneralTextChannel.SendMessageAsync(
                        $"{_mafiaData.MafiaRole.Mention} Время голосовать! Выбирайте жителя, который будет изгнан сегодня. ({dayVoteTime}с)");

                    var delay = Task.Delay(1000);
                    await _mafiaData.GeneralTextChannel.RemovePermissionOverwriteAsync(_mafiaData!.WatcherRole);
                    await delay;


                    var (kickedPlayer, isSkip) = await WaitForVotingAsync(
                        _mafiaData.GeneralTextChannel,
                        dayVoteTime,
                        _mafiaData.AlivePlayers,
                        _mafiaData.AlivePlayers.Select(user => user.GetFullName()).ToList());

                    await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData!.WatcherRole, _denyWrite);

                    if (kickedPlayer != null)
                    {
                        await _mafiaData.GeneralTextChannel.SendMessageAsync(
                            $"По результатам голосования нас покидает {kickedPlayer.Mention}. Надеемся, что жители сделали правильный выбор...");

                        await EjectPlayerAsync(kickedPlayer);

                        if (_mafiaData.Murders.Count == 0 || _mafiaData.AlivePlayers.Count <= 2 * _mafiaData.Murders.Count)
                            break;
                    }
                    else
                    {
                        var msg = (isSkip ? "Вы пропустили голосование." : "Вы не смогли прийти к единому решению.") + " Никто не будет изгнан сегодня.";
                        await _mafiaData.GeneralTextChannel.SendMessageAsync(msg);
                    }
                }
                else
                    _mafiaData.IsFirstNight = false;

                if (!GameData.IsPlaying)
                    break;

                await Task.Delay(2000);
                await _mafiaData.GeneralTextChannel.SendMessageAsync("Город засыпает...");

                var tasks = new Task[]
                {
                    DoMurdersMove(nightTime, nightMurderVoteTime),
                    DoDoctorMove(nightInnocentVoteTime),
                    DoCommissionerMove(nightInnocentVoteTime)
                };


                GuildLogger.Verbose(LogTemplate, nameof(PlayAsync), "Begin do night moves...");

                try
                {
                    Task.WaitAll(tasks);

                    if (_mafiaData.Has3MurdersInGame)
                    {
                        await _mafiaData.GeneralTextChannel.SendMessageAsync("Дон выбирает к кому наведаться ночью");

                        await DoDonMove(nightMurderVoteTime);
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
                await ReplyAsync($"Fucking HTTP Exception");

                GuildLogger.Warning(e, LogTemplate, nameof(PlayAsync), "Handling HTTP exception");
            }
        }

        GuildLogger.Debug(LogTemplate, nameof(PlayAsync), "End playing game");
    }


    private async Task DoMurdersMove(int nightTime, int nightMurderVoteTime)
    {
        GuildLogger!.Debug(LogTemplate, nameof(DoMurdersMove), "Begin do murders move...");

        await _mafiaData!.MurderTextChannel.RemovePermissionOverwriteAsync(_mafiaData.WatcherRole);


        var extraTime = 0;
        if (_mafiaData.Murders.Count > 1)
        {
            await ChangePermissionsMurderChannelsAsync(_allowWrite, _allowSpeak);
            await _mafiaData.MurderTextChannel.SendMessageAsync($"Кто же погибнет этой ночью? Обсуждайте ({nightTime}с)");

            await WaitTimerAsync(nightTime, _mafiaData.MurderTextChannel);
        }
        else
            extraTime += nightTime;


        await _mafiaData.MurderTextChannel.SendMessageAsync(
            $"{_mafiaData.MafiaRole.Mention} Время голосовать! Выбирайте жителя, который будет убит сегодня ({nightMurderVoteTime + extraTime}с)");

        await ChangePermissionsMurderChannelsAsync(_denyWrite, _denySpeak);

        (_mafiaData.MurdersMove, var isSkip) = await WaitForVotingAsync(
            _mafiaData.MurderTextChannel,
            nightMurderVoteTime + extraTime,
            _mafiaData.AlivePlayers,
            _mafiaData.AlivePlayers.Select(user => user.GetFullName()).ToList());


        await _mafiaData.GeneralTextChannel.AddPermissionOverwriteAsync(_mafiaData.WatcherRole, _denyWrite);


        if (_mafiaData.MurdersMove is not null)
            await _mafiaData.MurderTextChannel.SendMessageAsync($"Решение сделано. Вы наведаетесь этой ночью к {_mafiaData.MurdersMove.GetFullName()}");
        else
        {
            var msg = (isSkip ? "Вы пропустили голосование." : "Вы не смогли прийти к единому решению.") + " Никто не умрет сегодня";

            await _mafiaData.MurderTextChannel.SendMessageAsync(msg);
        }

        await _mafiaData.MurderTextChannel.SendMessageAsync("Ожидайте наступления утра");
        await _mafiaData.GeneralTextChannel.SendMessageAsync("Мафия зарядила 1 пулю для своей жертвы.");


        GuildLogger.Debug(LogTemplate, nameof(DoMurdersMove), "End do murders move");
    }

    private async Task DoDoctorMove(int nightInnocentVoteTime)
    {
        GuildLogger!.Debug(LogTemplate, nameof(DoDoctorMove), "Begin do doctor move...");

        if (_mafiaData!.Doctor is not null)
        {
            await _mafiaData.Doctor.SendMessageAsync($"Доктор, ваш ход! Решайте, кто сегодня получит жизненно необходимую медицинскую помощь. ({nightInnocentVoteTime}с)");

            var except = new List<IGuildUser>();
            if (_mafiaData.DoctorMove is not null)
                except.Add(_mafiaData.DoctorMove);

            if (_mafiaData.HasDoctorSelfHealed)
                except.Add(_mafiaData.Doctor);

            var options = _mafiaData.AlivePlayers.Except(except).ToList();
            var displayOptions = options.Select(user => user.GetFullName()).ToList();


            (_mafiaData.DoctorMove, var isSkip) = await WaitForVotingAsync(
                await _mafiaData.Doctor.GetOrCreateDMChannelAsync(),
                nightInnocentVoteTime,
                options,
                displayOptions);


            if (!isSkip)
                _mafiaData.PlayerStats[_mafiaData.Doctor.Id].DoctorMovesCount++;

            if (_mafiaData.DoctorMove == _mafiaData.Doctor)
                _mafiaData.HasDoctorSelfHealed = true;

            if (_mafiaData.DoctorMove is not null)
                await _mafiaData.Doctor.SendMessageAsync($"Решение сделано. Этой ночью вы вылечите {_mafiaData.DoctorMove.GetFullName()}");
            else
            {
                var msg = (isSkip ? "Вы пропустили голосование." : "Вы не смогли прийти к единому решению.") + " Никто не умрет сегодня";

                await _mafiaData.Doctor.SendMessageAsync(msg);
            }

            await _mafiaData.Doctor.SendMessageAsync("Ожидайте наступления утра");
        }
        else
        {
            _mafiaData.DoctorMove = null;

            await WaitTimerAsync(nightInnocentVoteTime);
        }

        await _mafiaData.GeneralTextChannel.SendMessageAsync("Доктор выбрал чья жизнь сегодня в безопасности");

        GuildLogger.Debug(LogTemplate, nameof(DoDoctorMove), "End do doctor move");
    }

    private async Task DoCommissionerMove(int nightInnocentVoteTime)
    {
        GuildLogger!.Debug(LogTemplate, nameof(DoDoctorMove), "Begin do comissioner move...");


        if (_mafiaData!.Commissioner is not null)
        {
            var dmChannel = await _mafiaData.Commissioner.GetOrCreateDMChannelAsync();

            string voteMessage = "Шериф, делайте выбор, к кому наведаться с проверкой сегодня.";

            if (_mafiaData.Has3MurdersInGame && _mafiaData.HasCommissionerShot is null)
            {
                const string SearchMafiaOption = "Вычислить мафию";
                const string DoShotOption = "Сделать выстрел";

                var voteTime = nightInnocentVoteTime >= 60 ? 30 : nightInnocentVoteTime / 2;


                await _mafiaData.Commissioner.SendMessageAsync($"Шериф, выбирайте ваше действие на эту ночь. ({voteTime}с)");

                var (commissionerChoice, isSkip1) = await WaitForVotingAsync(
                    dmChannel,
                    voteTime,
                    new List<string>() { SearchMafiaOption, DoShotOption });

                if (commissionerChoice is not null)
                    await _mafiaData.Commissioner.SendMessageAsync("Выбор сделан.");
                else
                    await _mafiaData.Commissioner.SendMessageAsync("Вы не сделали выбор, поэтому вы проверите какого-либо жителя на причастность к мафии");

                if (commissionerChoice == DoShotOption)
                {
                    _mafiaData.HasCommissionerShot = true;

                    voteMessage = "В кого будем стрелять этой ночью?";
                }

                nightInnocentVoteTime -= voteTime;
            }


            await _mafiaData.Commissioner.SendMessageAsync($"{voteMessage} ({nightInnocentVoteTime}с)");

            var options = _mafiaData.AlivePlayers.Except(new List<IGuildUser>() { _mafiaData.Commissioner }).ToList();
            var displayOptions = options.Select(user => user.GetFullName()).ToList();

            var (commissionerMove, isSkip2) = await WaitForVotingAsync(
                dmChannel,
                nightInnocentVoteTime,
                options,
                displayOptions);

            if (!isSkip2)
                _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].CommissionerMovesCount++;


            if (commissionerMove != null)
            {
                if (_mafiaData.HasCommissionerShot is not true)
                {
                    await _mafiaData.Commissioner.SendMessageAsync($"{commissionerMove.GetFullName()} является");

                    await Task.Delay(1500);

                    var isMurderFound = _mafiaData.Murders.Contains(commissionerMove);
                    var isDonFound = _mafiaData.Don == commissionerMove;


                    if (isMurderFound)
                        _mafiaData.PlayerStats[_mafiaData.Commissioner.Id].CommissionerSuccessfullMovesCount++;

                    string text;

                    if (isMurderFound)
                        text = isDonFound ? "Доном!" : "Мафией!";
                    else
                        text = "Мирным жителем.";

                    await _mafiaData.Commissioner.SendMessageAsync(text);
                }
                else
                {
                    _mafiaData.CommissionerMove = commissionerMove;

                    await _mafiaData.Commissioner.SendMessageAsync($"Решение сделано. Вы зарядили свой револьвер для {commissionerMove.GetFullName()}");
                }
            }
            else
            {
                if (_mafiaData.HasCommissionerShot is false)
                    _mafiaData.HasCommissionerShot = null;

                await _mafiaData.Commissioner.SendMessageAsync(!isSkip2 ? "Вы не смогли принять решение." : "Вы пропустили голосование.");
            }

            await _mafiaData.Commissioner.SendMessageAsync("Ожидайте наступления утра");
        }
        else
            await WaitTimerAsync(nightInnocentVoteTime);

        await _mafiaData.GeneralTextChannel.SendMessageAsync("Шериф направился к дому одного из жителей для выяснения правды");


        GuildLogger.Debug(LogTemplate, nameof(DoDoctorMove), "End do comissioner move");
    }

    private async Task DoDonMove(int nightMurderVoteTime)
    {
        GuildLogger!.Debug(LogTemplate, nameof(DoDoctorMove), "Begin do don move...");

        if (_mafiaData!.Don is not null)
        {
            await _mafiaData.Don.SendMessageAsync($"Ваш ход, дон! Кто, по вашему мнению, является шерифом? ({nightMurderVoteTime}с)");

            var options = _mafiaData.AlivePlayers.Except(new List<IGuildUser>() { _mafiaData.Don }).ToList();
            var displayOptions = options.Select(user => user.GetFullName()).ToList();

            var (donMove, isSkip) = await WaitForVotingAsync(
                await _mafiaData.Don.GetOrCreateDMChannelAsync(),
                nightMurderVoteTime,
                options,
                displayOptions);

            if (!isSkip)
                _mafiaData.PlayerStats[_mafiaData.Don.Id].DonMovesCount++;


            if (donMove is not null)
            {
                await _mafiaData.Don.SendMessageAsync($"{donMove.GetFullName()} является");

                await Task.Delay(1500);

                string text;
                if (_mafiaData.Commissioner == donMove)
                {
                    text = "Шерифом!";

                    _mafiaData.PlayerStats[_mafiaData.Don.Id].DonSuccessfullMovesCount++;
                    _mafiaData.PlayerStats[_mafiaData.Don.Id].ExtraScores++;
                }
                else
                    text = "Не тем, кого вы ищете.";

                await _mafiaData.Don.SendMessageAsync(text);
            }
            else
            {
                await _mafiaData.Don.SendMessageAsync(!isSkip ? "Вы не смогли принять решение." : "Вы пропустили голосование.");
            }

            await _mafiaData.Don.SendMessageAsync("Ожидайте наступления утра");
        }
        else
            await WaitTimerAsync(nightMurderVoteTime);

        await _mafiaData.GeneralTextChannel.SendMessageAsync("Дон наведался к одному из жителей с парочкой вопросов.");

        GuildLogger.Debug(LogTemplate, nameof(DoDoctorMove), "End do do move");
    }


    private async Task ChangePermissionsMurderChannelsAsync(OverwritePermissions textPerms, OverwritePermissions voicePerms)
    {
        foreach (var murder in _mafiaData!.Murders)
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



        _mafiaData.AlivePlayers.Remove(player);
        _mafiaData.Murders.Remove(player);
        _mafiaData.KilledMurders.Add(player);

        if (_mafiaData.Commissioner == player)
            _mafiaData.Commissioner = null;
        else if (_mafiaData.Doctor == player)
            _mafiaData.Doctor = null;
        else if (_mafiaData.Don == player)
            _mafiaData.Don = null;

        var playerExistInGuild = Context.Guild.GetUser(player.Id) is not null;

        if (!playerExistInGuild)
        {
            GuildLogger.Debug(LogTemplate, nameof(EjectPlayerAsync), $"Player {player.GetFullName()} does not exists in guild");

            return;
        }


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
        if (_settings.ReplyMessagesOnError)
            await ReplyAsync(message);

        if (_settings.AbortGameWhenError)
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
        if (_mafiaData is null)
            throw new NullReferenceException($"{nameof(_mafiaData)} is null");

        var playersId = new HashSet<ulong>(GameData!.Players.Select(p => p.Id));

        await AddNewUsersAsync(playersId);

        var playersStat = await Db.MafiaStats
                  .AsTracking()
                  .Where(stat => playersId.Contains(stat.UserId) && stat.GuildId == Context.Guild.Id)
                  .ToListAsync();

        var newPlayersId = playersId
            .Except(playersStat.Select(s => s.UserId))
            .ToList();

        if (newPlayersId.Count > 0)
        {
            var newPlayersStats = newPlayersId.Select(id => new MafiaStats
            {
                UserId = id,
                GuildId = Context.Guild.Id
            })
            .ToList();

            await Db.MafiaStats.AddRangeAsync(newPlayersStats);

            playersStat.AddRange(newPlayersStats);
        }

        foreach (var playerStat in playersStat)
        {
            var gameStat = _mafiaData.PlayerStats[playerStat.UserId];

            playerStat.GamesCount++;
            playerStat.WinsCount += gameStat.WinsCount;

            playerStat.BlacksGamesCount += gameStat.BlacksGamesCount;
            playerStat.BlacksWinsCount += gameStat.BlacksWinsCount;

            playerStat.DoctorMovesCount += gameStat.DoctorMovesCount;
            playerStat.DoctorSuccessfullMovesCount += gameStat.DoctorSuccessfullMovesCount;

            playerStat.CommissionerMovesCount += gameStat.CommissionerMovesCount;
            playerStat.CommissionerSuccessfullMovesCount += gameStat.CommissionerSuccessfullMovesCount;

            playerStat.DonMovesCount += gameStat.DonMovesCount;
            playerStat.DonSuccessfullMovesCount += gameStat.DonSuccessfullMovesCount;


            playerStat.ExtraScores += gameStat.ExtraScores;

            playerStat.PenaltyScores += gameStat.PenaltyScores;
        }


        await Db.SaveChangesAsync();
    }


    private async Task SetAndSaveSettingsToDbAsync()
    {
        GuildLogger!.Debug(LogTemplate, nameof(SetAndSaveSettingsToDbAsync), "Settings saving...");

        _settings.GeneralTextChannelId = _mafiaData!.GeneralTextChannel.Id;
        _settings.GeneralVoiceChannelId = _mafiaData.GeneralVoiceChannel.Id;
        _settings.MurdersTextChannelId = _mafiaData.MurderTextChannel.Id;
        _settings.MurdersVoiceChannelId = _mafiaData.MurderVoiceChannel.Id;
        _settings.MafiaRoleId = _mafiaData.MafiaRole.Id;
        _settings.WatcherRoleId = _mafiaData.WatcherRole.Id;

        await Db.SaveChangesAsync();

        GuildLogger.Debug(LogTemplate, nameof(SetAndSaveSettingsToDbAsync), "Settings saved");
    }


    private class MafiaData
    {
        public Dictionary<ulong, MafiaStats> PlayerStats { get; }

        public Dictionary<ulong, ICollection<IRole>> PlayerRoles { get; }

        public List<ulong> OverwrittenNicknames { get; }


        public List<IGuildUser> KilledPlayers { get; }

        public List<IGuildUser> AlivePlayers { get; }


        public List<IGuildUser> Murders { get; }
        public List<IGuildUser> KilledMurders { get; }

        public IGuildUser? Doctor { get; set; }
        public IGuildUser? Commissioner { get; set; }
        public IGuildUser? Don { get; set; }


        public IGuildUser? CommissionerMove { get; set; }
        public IGuildUser? DoctorMove { get; set; }
        public IGuildUser? MurdersMove { get; set; }


        public ITextChannel GeneralTextChannel { get; }
        public ITextChannel MurderTextChannel { get; }

        public IVoiceChannel GeneralVoiceChannel { get; }
        public IVoiceChannel MurderVoiceChannel { get; }


        public IRole MafiaRole { get; }
        public IRole WatcherRole { get; }


        public bool? HasCommissionerShot { get; set; }

        public bool Has3MurdersInGame { get; set; }

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
            PlayerStats = new();

            PlayerRoles = new();

            OverwrittenNicknames = new();


            KilledPlayers = new();

            AlivePlayers = new();


            Murders = new();

            KilledMurders = new();


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
}
