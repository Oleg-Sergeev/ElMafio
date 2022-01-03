﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Modules.Games;

[RequireContext(ContextType.Guild)]
public abstract class GameModule : GuildModuleBase
{
    protected static Dictionary<ulong, Dictionary<Type, GameModuleData>> GamesData { get; } = new();


    protected IConfiguration Config { get; }


    protected GameModuleData? GameData { get; set; }

    public GameModule(InteractiveService interactiveService, IConfiguration config) : base(interactiveService)
    {
        Config = config;
    }


    [Command("Упомянуть")]
    [Alias("Квот", "Пинг")]
    public virtual async Task MentionPlayers()
    {
        GameData = GetGameData();

        if (GameData is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игра еще не создана");

            return;
        }

        if (GameData.Creator.Id != Context.User.Id && !Context.User.HasGuildPermission(GuildPermission.Administrator))
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Упомянуть всех участников может только создатель или администратор");

            return;
        }


        var mentions = "Упоминание всех игроков:\n";

        foreach (var player in GameData.Players)
            mentions += $"{player.Mention}\n";


        await ReplyAsync(mentions);
    }


    [Command("Создатель")]
    [Summary("Показать создателя игры")]
    [Remarks("Только создатель может запустить игру")]
    public virtual async Task ShowCreator()
    {
        GameData = GetGameData();

        if (GameData is not null)
            await ReplyEmbedAsync(EmbedStyle.Information, $"Создатель - **{GameData.Creator.GetFullName()}**");
        else
            await ReplyEmbedAsync(EmbedStyle.Error, "Игра еще не создана");
    }


    [Command("Список")]
    [Summary("Показать список игроков")]
    public virtual async Task ShowPlayerList()
    {
        GameData = GetGameData();

        if (GameData is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игра еще не создана");

            return;
        }


        var text = "";

        for (int i = 0; i < GameData.Players.Count; i++)
        {
            var player = GameData.Players[i];

            text += $"[{i + 1}] {player.Mention} - {(GameData.Creator.Id == player.Id ? "**создатель**" : "участник")}\n";
        }

        await ReplyEmbedAsync(EmbedStyle.Information, text, "Список игроков");
    }


    [Priority(-1)]
    [Command("Играть")]
    [Alias("игра")]
    [Summary("Создать новую игру или присоединиться к текущей")]
    public async Task JoinAsync()
    {
        var guildUser = (IGuildUser)Context.User;

        await JoinAsync(guildUser);
    }

    [RequireOwner()]
    [Command("Играть")]
    [Alias("игра")]
    public virtual async Task JoinAsync(IGuildUser guildUser)
    {
        GameData = GetGameData();

        if (GameData is null)
        {
            GameData = CreateGameData(guildUser);
            AddGameDataToGamesList(Context.Guild.Id, GameData);

            AutoStop();


            GameData.Players.Add(guildUser);

            await ReplyEmbedStampAsync(EmbedStyle.Successfull, $"{GameData.Name} создана! Хост игры - {guildUser.Mention}");

            return;
        }

        if (GameData!.IsPlaying)
        {
            await ReplyEmbedAsync(EmbedStyle.Warning, $"{GameData.Name} уже запущена. Дождитесь окончания");

            return;
        }

        if (GameData.Players.Contains(guildUser))
        {
            await ReplyEmbedAsync(EmbedStyle.Warning, "Вы уже участвуете!");

            return;
        }


        GameData.Players.Add(guildUser);

        await ReplyEmbedAsync(EmbedStyle.Information, $"{guildUser.GetFullMention()} присоединился к игре! Количество участников: {GameData.Players.Count}");
    }


    [Command("Выход")]
    [Summary("Покинуть игру")]
    public virtual async Task LeaveAsync()
    {
        GameData = GetGameData();

        if (GameData is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игра еще не создана");

            return;
        }

        if (GameData.IsPlaying)
            await ReplyEmbedAsync(EmbedStyle.Warning, $"{GameData.Name} уже началась, выход невозможен");
        else
        {
            var guildUser = (IGuildUser)Context.User;

            if (GameData.Players.Remove(guildUser))
            {
                await ReplyEmbedAsync(EmbedStyle.Information, $"{guildUser.GetFullMention()} покинул игру. Количество участников: {GameData.Players.Count}");

                if (GameData.Players.Count == 0)
                    await StopAsync();
                else if (GameData.Creator.Id == guildUser.Id)
                    GameData.Creator = GameData.Players[0];
            }
            else
                await ReplyEmbedAsync(EmbedStyle.Warning, "Вы не можете выйти: вы не участник");
        }
    }


    [Command("Стоп")]
    [Summary("Остановить игру")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public virtual async Task StopAsync()
    {
        GameData = GetGameData();

        if (GameData is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игра еще не создана");

            return;
        }

        if (GameData.IsPlaying)
            GameData.IsPlaying = false;
        else
            DeleteGameData();

        await ReplyEmbedStampAsync(EmbedStyle.Successfull, $"{GameData.Name} остановлена");
    }


    [Command("Выгнать")]
    [Summary("Выгнать пользователя из игры")]
    [Remarks("Попытка выгнать себя приравнивается выходу из игры")]
    public virtual async Task KickAsync([Summary("Пользователь, которого вы хотите выгнать")] IGuildUser guildUser)
    {
        GameData = GetGameData();


        if (GameData is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Игра еще не создана");

            return;
        }

        if (GameData.Creator.Id != Context.User.Id)
        {
            var ownerId = (await Context.Client.GetApplicationInfoAsync()).Owner.Id;

            if (ownerId != Context.User.Id)
            {
                await ReplyEmbedAsync(EmbedStyle.Error, "Вы не являетесь создателем игры");

                return;
            }
        }

        if (!GameData.Players.Contains(guildUser))
        {
            await ReplyEmbedAsync(EmbedStyle.Error, $"{guildUser.GetFullName()} не участвует в игре");

            return;
        }

        if (Context.User.Id == guildUser.Id)
        {
            await LeaveAsync();

            return;
        }

        if (GameData.IsPlaying)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, $"{GameData.Name} уже началась, выгнать игрока невозможно");

            return;
        }


        if (GameData.Players.Remove(guildUser))
            await ReplyEmbedStampAsync(EmbedStyle.Successfull, $"{guildUser.GetFullMention()} выгнан из игры. Количество участников: {GameData.Players.Count}");
        else
            await ReplyEmbedStampAsync(EmbedStyle.Error, $"Не удалось выгнать {guildUser.GetFullName()}");
    }



    [Command("Старт")]
    [Summary("Запустить игру")]
    public abstract Task StartAsync();


    protected virtual async void AutoStop(TimeSpan? timeout = null, CancellationToken token = default)
    {
        if (GameData is null || GameData.IsPlaying)
            return;

        timeout ??= TimeSpan.FromMinutes(30);

        if (token != default)
        {
            try
            {
                await Task.Delay(timeout.Value, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
        else
        {
            var secs = (int)timeout.Value.TotalSeconds;

            while (secs-- > 0)
            {
                await Task.Delay(1000, CancellationToken.None);

                if (GameData is null || GameData.IsPlaying)
                    return;
            }
        }

        if (GameData is null || GameData.IsPlaying)
            return;


        await ReplyEmbedAsync(EmbedStyle.Warning, "Остановка игры из-за низкой активности...");

        await StopAsync();
    }




    // ????

    protected abstract GameModuleData CreateGameData(IGuildUser creator);

    protected async Task AddNewUsersAsync(HashSet<ulong> playersId)
    {
        var existingPlayersId = await Context.Db.Users
            .AsNoTracking()
            .Where(u => playersId.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        if (existingPlayersId.Count == playersId.Count)
            return;


        var newPlayersId = playersId.Except(existingPlayersId);

        var newUsers = GameData!.Players
            .Where(u => newPlayersId.Contains(u.Id))
            .Select(u => new User
            {
                Id = u.Id,
                JoinedAt = u.JoinedAt!.Value.DateTime
            })
            .ToList();

        await Context.Db.Users.AddRangeAsync(newUsers);

        await Context.Db.SaveChangesAsync();
    }

    protected GameModuleData? GetGameData()
    {
        var type = GetType();

        if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
            return null;

        return games.GetValueOrDefault(type);
    }

    protected virtual bool CanStart(out string? failMessage)
    {
        failMessage = null;


        if (GameData is null)
        {
            failMessage = "Игра еще не создана";

            return false;
        }

        if (GameData.Players.Count < GameData.MinPlayersCount)
        {
            failMessage = $"Недостаточно игроков. Минимальное количество игроков для игры: {GameData.MinPlayersCount}";

            return false;
        }

        if (GameData.IsPlaying)
        {
            failMessage = $"{GameData.Name} уже запущена, дождитесь завершения игры";

            return false;
        }

        if (GameData.Creator.Id != Context.User.Id && Context.User.Id != Context.Guild.OwnerId)
        {
            failMessage = "Вы не являетесь инициатором игры. Запустить игру может только ее инициатор";

            return false;
        }


        return true;
    }

    protected void AddGameDataToGamesList(ulong guildId, GameModuleData gameData)
    {
        var type = GetType();

        if (GamesData.TryGetValue(guildId, out var games))
            games.Add(type, gameData);
        else
            GamesData.Add(guildId, new()
            {
                { type, gameData }
            });
    }

    protected void DeleteGameData()
    {
        GamesData[Context.Guild.Id].Remove(GetType());
    }


    protected class GameModuleData
    {
        public IGuildUser Creator { get; set; }

        public IList<IGuildUser> Players { get; }

        public bool IsPlaying { get; set; }


        public string Name { get; }

        public int MinPlayersCount { get; }


        public GameModuleData(string name, int minPlayersCount, IGuildUser creator)
        {
            Players = new List<IGuildUser>();

            IsPlaying = false;


            Name = name;

            MinPlayersCount = minPlayersCount;

            Creator = creator;
        }
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
                await ReplyEmbedAsync(EmbedStyle.Error, "Описание игры не найдено");

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
                await ReplyEmbedAsync(EmbedStyle.Error, "Подробности не найдены");

                return;
            }

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

                if (!sendToServer)
                    await Context.User.SendMessageAsync(embed: builder.Build());
                else
                    await ReplyAsync(embed: builder.Build());
            }
        }
    }


    public abstract class RatingModule : GuildModuleBase
    {
        protected RatingModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        [Command("Рейтинг")]
        [Alias("Рейт")]
        public virtual async Task ShowRatingAsync()
        {
            await ReplyAsync("Rating");
        }
    }
}