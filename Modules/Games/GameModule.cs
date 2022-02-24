using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Data;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Infrastructure.Data.Models;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Common.Preconditions;

namespace Modules.Games;


public abstract class GameModule<TStats> : GameModule<GameData, TStats> where TStats : GameStats, new()
{
    protected GameModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }
}


[RequireContext(ContextType.Guild)]
public abstract class GameModule<TData, TStats> : GuildModuleBase
    where TData : GameData
    where TStats : GameStats, new()
{
    private const int AfkTimeout = 30 * 60 * 1000;

    protected static Dictionary<ulong, Dictionary<Type, TData>> GamesData { get; } = new();

    private static readonly Dictionary<ulong, Dictionary<Type, Timer>> _afkTimers = new();


    public GameModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }



    [Command("Играть")]
    [Alias("игра")]
    [Summary("Присоединиться к существующей игре, или создать новую")]
    [Priority(-1)]
    public virtual async Task JoinAsync()
    {
        var guildUser = (IGuildUser)Context.User;

        await JoinAsync(guildUser);
    }

    [RequireOwner]
    [Command("Играть")]
    [Alias("игра")]
    public async Task JoinAsync(IGuildUser player)
    {
        if (!TryGetGameData(out var gameData))
        {
            gameData = CreateGameData(player);

            AddGameDataToGamesList(gameData);

            gameData.Players.Add(player);

            await ReplyEmbedAsync($"{gameData.Name} создана! Хост игры - {player.Mention}", EmbedStyle.Successfull, gameData.Name);

            var isStarted = StartAfkWaiting(gameData);

            if (!isStarted)
                await ReplyEmbedAsync("Не удалось запустить автоматическую остановку игры", EmbedStyle.Warning);

            return;
        }

        if (gameData.IsPlaying)
        {
            await ReplyEmbedAsync($"{gameData.Name} уже запущена. Дождитесь окончания", EmbedStyle.Warning, gameData.Name);

            return;
        }

        if (gameData.Players.Contains(player))
        {
            await ReplyEmbedAsync("Вы уже участвуете!", EmbedStyle.Warning, gameData.Name);

            return;
        }

        UpdateAfk();

        gameData.Players.Add(player);

        await ReplyEmbedAsync($"{player.GetFullMention()} присоединился к игре! Количество участников: {gameData.Players.Count}", gameData.Name);
    }


    [Command("Выход")]
    [Summary("Покинуть игру")]
    [Remarks("Если игру покидает хост, то хостом становится игрок, следующий за ним" +
        "\nЕсли игру покидает последний участник - игра автоматически останавливается")]
    public virtual async Task LeaveAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }


        if (gameData.IsPlaying)
        {
            await ReplyEmbedAsync("Игра уже началась, выход невозможен", EmbedStyle.Warning, gameData.Name);

            return;
        }
        var guildUser = (IGuildUser)Context.User;

        if (gameData.Players.Remove(guildUser))
        {
            await ReplyEmbedAsync($"{guildUser.GetFullMention()} покинул игру. Количество участников: {gameData.Players.Count}", gameData.Name);

            if (gameData.Players.Count == 0)
                await StopAsync();
            else if (gameData.Host.Id == guildUser.Id)
                gameData.Host = gameData.Players[0];
        }
        else
            await ReplyEmbedAsync("Вы не можете выйти: вы не участник", EmbedStyle.Warning, gameData.Name);
    }


    [Command("Стоп")]
    [Summary("Остановить игру")]
    [Remarks("Только хост или администратор может остановить игру")]
    public virtual async Task StopAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }

        if (gameData.Host.Id != Context.User.Id && !Context.User.HasGuildPermission(GuildPermission.Administrator))
        {
            await ReplyEmbedAsync("Остановить игру может только хост или администратор", EmbedStyle.Error);

            return;
        }

        if (gameData.IsPlaying)
            gameData.IsPlaying = false;
        else if (!DeleteGameData())
        {
            await ReplyEmbedAsync("Возникла непредвиненная ошибка\nИгра не найдена", EmbedStyle.Error, gameData.Name);

            return;
        }


        await ReplyEmbedStampAsync($"{gameData.Name} остановлена", EmbedStyle.Successfull, gameData.Name);
    }


    [Command("Старт")]
    [Alias("Запуск")]
    [Summary("Запустить игру")]
    [Remarks("Только хост или администратор может запустить игру")]
    public abstract Task StartAsync();



    [Command("Упомянуть")]
    [Alias("Квот", "Пинг")]
    [Summary("Упомянуть всех участников игры")]
    [Remarks("Только хост или администратор может упомянуть всех")]
    public virtual async Task MentionPlayersAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }


        var res = await CheckUserPermsAsync(gameData.Host.Id);

        if (!res.IsSuccess)
        {
            await ReplyEmbedAsync("Упомянуть всех участников может только хост или администратор", EmbedStyle.Error);

            return;
        }


        var mentions = "Упоминание всех игроков:\n";

        foreach (var player in gameData.Players)
            mentions += $"{player.Mention}\n";


        await ReplyAsync(mentions);
    }


    [Command("Хост")]
    [Summary("Показать хоста игры")]
    [Remarks("Хостом игры является первый участник" +
        "\nХост может запускать/останавливать игру, выгонять игроков и упоминать всех игроков" +
        "\nЕсли хост покидает игру, то новым хостом становится следующий по порядку участник")]
    public virtual async Task ShowHostAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }

        await ReplyEmbedAsync($"Хост - **{gameData.Host.GetFullName()}**");
    }


    [Command("Список")]
    [Summary("Показать список игроков")]
    public virtual async Task ShowPlayerListAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }


        var text = "";

        for (int i = 0; i < gameData.Players.Count; i++)
        {
            var player = gameData.Players[i];

            text += $"[{i + 1}] {player.Mention} - {(gameData.Host.Id == player.Id ? "**создатель**" : "участник")}\n";
        }

        await ReplyEmbedAsync(text, "Список игроков");
    }



    [Command("Выгнать")]
    [Alias("Кик")]
    [Summary("Выгнать пользователя из игры")]
    [Remarks("Попытка выгнать себя приравнивается выходу из игры" +
        "\nТолько хост или администратор может выгнать игрока")]
    public virtual async Task KickAsync([Summary("Выгоняемый игрок")] IGuildUser player)
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }

        var res = await CheckUserPermsAsync(gameData.Host.Id);

        if (!res.IsSuccess)
        {
            await ReplyEmbedAsync("Вы не являетесь создателем игры", EmbedStyle.Error);

            return;
        }

        if (!gameData.Players.Contains(player))
        {
            await ReplyEmbedAsync($"{player.GetFullName()} не участвует в игре", EmbedStyle.Error);

            return;
        }

        if (Context.User.Id == player.Id)
        {
            await LeaveAsync();

            return;
        }

        if (gameData.IsPlaying)
        {
            await ReplyEmbedAsync($"{gameData.Name} уже началась, выгнать игрока невозможно", EmbedStyle.Error);

            return;
        }


        if (gameData.Players.Remove(player))
            await ReplyEmbedStampAsync($"{player.GetFullMention()} выгнан из игры. Количество участников: {gameData.Players.Count}", EmbedStyle.Successfull);
        else
            await ReplyEmbedStampAsync($"Не удалось выгнать {player.GetFullName()}", EmbedStyle.Error);
    }


    protected abstract TData CreateGameData(IGuildUser host);


    protected virtual async Task<PreconditionResult> CheckPreconditionsAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            return PreconditionResult.FromError("Игра еще не создана");
        }

        if (gameData.Players.Count < gameData.MinPlayersCount)
        {
            return PreconditionResult.FromError($"Недостаточно игроков. Минимальное количество игроков для игры: {gameData.MinPlayersCount}");
        }

        if (gameData.IsPlaying)
        {
            return PreconditionResult.FromError($"{gameData.Name} уже запущена, дождитесь завершения игры");
        }

        var res = await CheckUserPermsAsync(gameData.Host.Id);

        if (!res.IsSuccess)
            return res;

        return PreconditionResult.FromSuccess();
    }


    protected Task<Dictionary<ulong, TStats>> GetStatsAsync(IEnumerable<ulong> playersIds, bool isTracking = true)
        => Context.Db.Set<TStats>()
            .AsTracking(isTracking ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
            .Where(ms => ms.GuildSettingsId == Context.Guild.Id && playersIds.Any(id => id == ms.UserId))
            .ToDictionaryAsync(s => s.UserId);


    protected async Task<int> AddNewStatsAsync(IEnumerable<ulong> playersIds, IDictionary<ulong, TStats> stats)
    {
        var existingUsersIds = await Context.Db.Users
                            .AsNoTracking()
                            .Where(u => playersIds.Any(id => id == u.Id))
                            .Select(u => u.Id)
                            .ToListAsync();

        var newUsersIds = playersIds.Except(existingUsersIds);

        if (newUsersIds.Any())
            AddNewUsers(newUsersIds);

        var newUsersStats = newUsersIds
        .Concat(existingUsersIds.Except(stats.Keys))
        .Select(id => new TStats
        {
            UserId = id,
            GuildSettingsId = Context.Guild.Id
        }).ToList();

        Context.Db.AddRange(newUsersStats);

        stats.AddRange(newUsersStats.ToDictionary(s => s.UserId));

        return newUsersStats.Count;
}


    protected async Task<IDictionary<ulong, TStats>> GetStatsWithAddingNewAsync(IEnumerable<ulong> playersIds, bool isTracking = true)
    {
        var stats = await GetStatsAsync(playersIds, isTracking);

        if (stats.Count < playersIds.Count())
            await AddNewStatsAsync(playersIds, stats);

        return stats;
    }

    // Mb replace
    protected void AddNewUsers(IEnumerable<ulong> usersIds)
    {
        Context.Db.Users.AddRange(usersIds.Select(id => new User
        {
            Id = id
        }).ToList());
    }

    protected void AddGameDataToGamesList(TData gameData)
    {
        var type = GetType();

        if (GamesData.TryGetValue(Context.Guild.Id, out var games))
            games.Add(type, gameData);
        else
            GamesData.Add(Context.Guild.Id, new()
            {
                { type, gameData }
            });
    }

    protected bool DeleteGameData()
    {
        if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
            return false;

        var type = GetType();

        if (_afkTimers.TryGetValue(Context.Guild.Id, out var timers) && timers.TryGetValue(type, out var timer))
        {
            timer.Dispose();

            timers.Remove(type);
        }

        return games?.Remove(type) ?? false;
    }


    protected TData GetGameData()
    {
        if (!TryGetGameData(out var gameData))
            throw new KeyNotFoundException($"Game data was not found. Guild Id: {Context.Guild.Id}, Game type: {GetType()}");

        return gameData;
    }

    protected bool TryGetGameData([NotNullWhen(true)] out TData? gameData)
    {
        var type = GetType();

        gameData = null;

        if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
            return false;

        return games.TryGetValue(type, out gameData);
    }


    protected async Task<PreconditionResult> CheckUserPermsAsync(ulong hostId)
    {
        if (hostId != Context.User.Id && Context.User.Id != Context.Guild.OwnerId)
        {
            var botOwner = (await Context.Client.GetApplicationInfoAsync()).Owner;

            if (botOwner.Id != hostId)
                return PreconditionResult.FromError("Вы не являетесь хостом игры. Запустить игру может только хост");
        }

        return PreconditionResult.FromSuccess();
    }


    private bool StartAfkWaiting(TData data)
    {
        var type = GetType();

        if (!_afkTimers.TryGetValue(Context.Guild.Id, out var timers))
        {
            timers = new()
            {
                { type, StartTimer() }
            };

            _afkTimers[Context.Guild.Id] = timers;

            return true;
        }

        if (!_afkTimers[Context.Guild.Id].TryGetValue(type, out var timer))
        {
            _afkTimers[Context.Guild.Id][type] = StartTimer();

            return true;
        }

        return false;


        Timer StartTimer() => new(async (o) =>
        {
            if (!data.IsPlaying)
            {
                await ReplyEmbedAsync("Остановка игры из-за низкой активности...", EmbedStyle.Waiting);

                await StopAsync();
            }
        }, null, AfkTimeout, -1);
    }

    private bool UpdateAfk() => _afkTimers.TryGetValue(Context.Guild.Id, out var timers)
        && timers.TryGetValue(GetType(), out var timer)
        && timer.Change(AfkTimeout, -1);





    [Group("Помощь")]
    [Summary("Здесь вы можете получить информацию об игре, ее правилах и других подробностях")]
    public abstract class HelpModule : GuildModuleBase
    {
        protected IConfiguration Config { get; }


        protected HelpModule(InteractiveService interactiveService, IConfiguration config) : base(interactiveService)
        {
            Config = config;
        }


        protected IConfigurationSection? GetGameSection(string sectionName)
        {
            var gameName = GetType().Name.Replace("HelpModule", null);
            var gameSection = Config.GetSection($"Games:{gameName}:{sectionName}");

            return gameSection;
        }


        [Command("ОбИгре")]
        [Summary("Получить информацию об игре")]
        public virtual async Task ShowAboutGameAsync(bool sendToServer = false)
        {
            var gameAboutSection = GetGameSection("About");

            if (gameAboutSection is null)
            {
                await ReplyEmbedAsync("Описание игры не найдено", EmbedStyle.Error);

                return;
            }


            var title = gameAboutSection.GetTitle() ?? "Об игре";

            var description = gameAboutSection.GetDescription() ?? "Нет описания";

            var builder = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithInformationMessage();

            var imageUrl = gameAboutSection.GetImageUrl();

            if (imageUrl is not null)
                builder.WithImageUrl(imageUrl);

            if (!sendToServer)
                await Context.User.SendMessageAsync(embed: builder.Build());
            else
                await ReplyAsync(embed: builder.Build());
        }


        [Command("Подробности")]
        [Summary("Получить подробное описание игры")]
        public virtual async Task ShowGameDetails(bool sendToServer = false)
        {
            var gameDetailsSection = GetGameSection("Details");

            if (gameDetailsSection is null)
            {
                await ReplyEmbedAsync("Подробности не найдены", EmbedStyle.Error);

                return;
            }

            var paginatorBuilder = new StaticPaginatorBuilder()
                .WithActionOnCancellation(ActionOnStop.DeleteMessage);

            for (int i = 1; ; i++)
            {
                var part = gameDetailsSection.GetSection($"Part{i}");

                var title = part.GetTitle() ?? gameDetailsSection.GetTitle() ?? "Подробности";

                var builder = new EmbedBuilder()
                    .WithTitle(title)
                    .WithInformationMessage();

                foreach (var paragraph in part.GetChildren())
                {
                    var paragraphField = paragraph.GetEmbedFieldInfo();

                    if (paragraphField is null)
                    {
                        if (paragraph.Key == "Description")
                            builder.WithDescription(paragraph.Value);

                        continue;
                    }

                    builder.AddField(paragraphField?.Item1, paragraphField?.Item2);
                }

                if (builder.Description is null && builder.Fields.Count == 0)
                    break;

                var postParagraph = part["PostParagraph"];

                if (postParagraph is not null)
                    builder.AddFieldWithEmptyName(postParagraph);

                paginatorBuilder.AddPage(PageBuilder.FromEmbedBuilder(builder));

            }


            if (!sendToServer)
                await Interactive.SendPaginatorAsync(paginatorBuilder.Build(), await Context.User.CreateDMChannelAsync(), TimeSpan.FromMinutes(10));
            else
                await Interactive.SendPaginatorAsync(paginatorBuilder.Build(), Context.Channel, TimeSpan.FromMinutes(10));
        }
    }


    [Group("Статистика")]
    [Alias("Стат", "С")]
    [Summary("Статистика позволяет посмотреть свою эффективность в игре")]
    public abstract class GameStatsModule : GuildModuleBase
    {
        protected GameStatsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Name("Статистика")]
        [Command]
        [Summary("Просмотреть личную статистику")]
        public virtual async Task ShowStatsAsync([Summary("Игрок, статистику которого нужно просмотреть")] IUser? user = null)
        {
            user ??= Context.User;

            var userStats = await Context.Db.Set<TStats>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.GuildSettingsId == Context.Guild.Id && s.UserId == user.Id);

            if (userStats is null)
            {
                await ReplyEmbedAsync("Личная статистика отсутствует", EmbedStyle.Error);

                return;
            }

            var embedBuilder = GetStatsEmbedBuilder(userStats, user);

            await ReplyAsync(embed: embedBuilder.Build());
        }


        [Command("Рейтинг")]
        [Alias("Рейт", "Р")]
        [Summary("Просмотреть рейтинг")]
        public virtual async Task ShowRatingAsync([Summary("Кол-во игроков на одной странице\nМакс. кол-во: `30`")] int playersPerPage = 10)
        {
            var allStats = await GetRatingQuery()
                .ThenByDescending(stat => stat.UserId)
                .ToListAsync();

            if (allStats.Count == 0)
            {
                await ReplyEmbedAsync("Рейтинг отсутствует", EmbedStyle.Error);

                return;
            }


            var playersId = allStats
                .Select(s => s.UserId)
                .ToHashSet();

            if (Context.Guild.Users.Count < Context.Guild.MemberCount)
                await Context.Guild.DownloadUsersAsync();

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
                        .Select(ms => $"{n++}. **{(players.TryGetValue(ms.UserId, out var p) ? p.GetFullName() : "[Н/Д]")}** - {ms.Rating:0.##}"))
                    };

                    return pageBuilder;
                })
                .Build();

            _ = Interactive.SendPaginatorAsync(lazyPaginator, Context.Channel, timeout: TimeSpan.FromMinutes(10));
        }



        protected virtual IOrderedQueryable<TStats> GetRatingQuery()
            => Context.Db.Set<TStats>()
                .AsNoTracking()
                .Where(s => s.GuildSettingsId == Context.Guild.Id)
                .OrderByDescending(stat => stat.Rating)
                    .ThenByDescending(stat => stat.WinRate)
                        .ThenByDescending(stat => stat.GamesCount);


        protected virtual EmbedBuilder GetStatsEmbedBuilder(TStats stats, IUser user)
            => new EmbedBuilder()
            .WithTitle($"Статистика игрока {user.GetFullName()}")
            .WithUserAuthor(user)
            .WithUserFooter(Context.Client.CurrentUser)
            .WithCurrentTimestamp()
            .WithColor(new Color(230, 151, 16))
            .AddField("Общий % побед", $"{stats.WinRate:P2} ({stats.WinsCount}/{stats.GamesCount})", true);




        [Group]
        [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
        [RequireOwner(Group = "perm")]
        [Summary("Данный раздел предназначен для управления статистикой и рейтингом игроков")]
        [Remarks("**Внимание**\nБудьте аккуратны при выполнении команд из этого раздела. Действие команд **нельзя отменить**")]
        public abstract class GameAdminModule : GuildModuleBase
        {
            public GameAdminModule(InteractiveService interactiveService) : base(interactiveService)
            {
            }


            [Command("РейтСброс")]
            [Alias("РСброс")]
            [Summary("Сбросить весь рейтинг игры")]
            [Remarks("**Действие нельзя обратить!**")]
            [RequireConfirmAction]
            public virtual async Task ResetRatingAsync()
            {
                var allStats = await Context.Db.Set<TStats>()
                    .Where(s => s.GuildSettingsId == Context.Guild.Id)
                    .ToListAsync();

                foreach (var stat in allStats)
                    stat.Reset();

                await Context.Db.SaveChangesAsync();

                await ReplyEmbedStampAsync("Рейтинг успешно сброшен", EmbedStyle.Successfull);
            }


            [Command("Сброс")]
            [Summary("Сбросить свою стаститику, или статистику указанного пользователя")]
            [Remarks("**Действие нельзя обратить!**")]
            [RequireConfirmAction(false)]
            public async Task ResetStatsAsync([Summary("Игрок, статистику которого нужно сбросить")] IUser? user = null)
            {
                user ??= Context.User;

                var userStat = await Context.Db.MafiaStats
                       .FirstOrDefaultAsync(ms => ms.GuildSettingsId == Context.Guild.Id && ms.UserId == user.Id);

                if (userStat is null)
                {
                    await ReplyEmbedAsync($"Статистика игрока {user.GetFullMention()} не найдена", EmbedStyle.Error);

                    return;
                }

                userStat.Reset();

                await Context.Db.SaveChangesAsync();

                await ReplyEmbedStampAsync($"Статистика игрока {user.GetFullMention()} успешно сброшена", EmbedStyle.Successfull);
            }
        }
    }
}