using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    protected static Dictionary<ulong, Dictionary<Type, TData>> GamesData { get; } = new();



    public GameModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }



    [Priority(-1)]
    [Command("Играть")]
    [Alias("игра")]
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

            //AutoStop();

            gameData.Players.Add(player);

            await ReplyEmbedAsync($"{gameData.Name} создана! Хост игры - {player.Mention}", EmbedStyle.Successfull, gameData.Name);

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


        gameData.Players.Add(player);

        await ReplyEmbedAsync($"{player.GetFullMention()} присоединился к игре! Количество участников: {gameData.Players.Count}", gameData.Name);
    }


    [Command("Выход")]
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


    [RequireUserPermission(GuildPermission.Administrator, Group = "Perm")]
    [RequireOwner(Group = "Perm")]
    [Command("Стоп")]
    public virtual async Task StopAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

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
    public abstract Task StartAsync();



    [Command("Упомянуть")]
    [Alias("Квот", "Пинг")]
    public virtual async Task MentionPlayers()
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }


        if (gameData.Host.Id != Context.User.Id && !Context.User.HasGuildPermission(GuildPermission.Administrator))
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
    [Remarks("Только хост может запустить игру")]
    public virtual async Task ShowCreator()
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
    public virtual async Task ShowPlayerList()
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
    [Summary("Выгнать пользователя из игры")]
    [Remarks("Попытка выгнать себя приравнивается выходу из игры")]
    public virtual async Task KickAsync([Summary("Пользователь, которого вы хотите выгнать")] IGuildUser guildUser)
    {
        if (!TryGetGameData(out var gameData))
        {
            await ReplyEmbedAsync("Игра еще не создана", EmbedStyle.Error);

            return;
        }

        if (gameData.Host.Id != Context.User.Id)
        {
            var ownerId = (await Context.Client.GetApplicationInfoAsync()).Owner.Id;

            if (ownerId != Context.User.Id)
            {
                await ReplyEmbedAsync("Вы не являетесь создателем игры", EmbedStyle.Error);

                return;
            }
        }

        if (!gameData.Players.Contains(guildUser))
        {
            await ReplyEmbedAsync($"{guildUser.GetFullName()} не участвует в игре", EmbedStyle.Error);

            return;
        }

        if (Context.User.Id == guildUser.Id)
        {
            await LeaveAsync();

            return;
        }

        if (gameData.IsPlaying)
        {
            await ReplyEmbedAsync($"{gameData.Name} уже началась, выгнать игрока невозможно", EmbedStyle.Error);

            return;
        }


        if (gameData.Players.Remove(guildUser))
            await ReplyEmbedStampAsync($"{guildUser.GetFullMention()} выгнан из игры. Количество участников: {gameData.Players.Count}", EmbedStyle.Successfull);
        else
            await ReplyEmbedStampAsync($"Не удалось выгнать {guildUser.GetFullName()}", EmbedStyle.Error);
    }


    protected abstract TData CreateGameData(IGuildUser host);


    protected virtual Task<PreconditionResult> CheckPreconditionsAsync()
    {
        if (!TryGetGameData(out var gameData))
        {
            return Task.FromResult(PreconditionResult.FromError("Игра еще не создана"));
        }

        if (gameData.Players.Count < gameData.MinPlayersCount)
        {
            return Task.FromResult(PreconditionResult.FromError($"Недостаточно игроков. Минимальное количество игроков для игры: {gameData.MinPlayersCount}"));
        }

        if (gameData.IsPlaying)
        {
            return Task.FromResult(PreconditionResult.FromError($"{gameData.Name} уже запущена, дождитесь завершения игры"));
        }

        if (gameData.Host.Id != Context.User.Id && Context.User.Id != Context.Guild.OwnerId)
        {
            return Task.FromResult(PreconditionResult.FromError("Вы не являетесь хостом игры. Запустить игру может только хост"));
        }


        return Task.FromResult(PreconditionResult.FromSuccess());
    }

    protected virtual async Task<int> AddNewStatsAsync(IEnumerable<ulong> playersIds, IDictionary<ulong, TStats> stats)
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

        return games?.Remove(GetType()) ?? false;
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




    [Group("Помощь")]
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
    public abstract class GameStatsModule : GuildModuleBase
    {
        protected GameStatsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Command("Личная")]
        [Alias("Л")]
        [Priority(-10)]
        public virtual async Task ShowStatsAsync(IUser? user = null)
        {
            user ??= Context.User;

            var userStats = await Context.Db.Set<TStats>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.GuildSettingsId == Context.Guild.Id && s.UserId == user.Id);

            if (userStats is null)
            {
                return;
            }

            var embedBuilder = GetStatsEmbedBuilder(userStats, user);

            await ReplyAsync(embed: embedBuilder.Build());
        }


        protected virtual EmbedBuilder GetStatsEmbedBuilder(TStats stats, IUser user)
            => new EmbedBuilder()
            .WithTitle($"Статистика игрока {user.GetFullName()}")
            .WithUserAuthor(user)
            .WithUserFooter(Context.Client.CurrentUser)
            .WithCurrentTimestamp()
            .WithColor(new Color(230, 151, 16))
            .AddField("Общий % побед", $"{stats.WinRate:P2} ({stats.WinsCount}/{stats.GamesCount})", true);

    }
}