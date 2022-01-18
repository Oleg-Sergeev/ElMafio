using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Data;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;

namespace Modules.Games;

[RequireContext(ContextType.Guild)]
public abstract class GameModule<T> : GuildModuleBase where T : GameData
{
    protected static Dictionary<ulong, Dictionary<Type, T>> GamesData { get; } = new();



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
    protected void AddGameDataToGamesList(T gameData)
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



    protected abstract T CreateGameData(IGuildUser host);


    protected virtual bool CheckPreconditions([NotNullWhen(false)] out string? failMessage)
    {
        failMessage = null;


        if (!TryGetGameData(out var gameData))
        {
            failMessage = "Игра еще не создана";

            return false;
        }

        if (gameData.Players.Count < gameData.MinPlayersCount)
        {
            failMessage = $"Недостаточно игроков. Минимальное количество игроков для игры: {gameData.MinPlayersCount}";

            return false;
        }

        if (gameData.IsPlaying)
        {
            failMessage = $"{gameData.Name} уже запущена, дождитесь завершения игры";

            return false;
        }

        if (gameData.Host.Id != Context.User.Id && Context.User.Id != Context.Guild.OwnerId)
        {
            failMessage = "Вы не являетесь хостом игры. Запустить игру может только хост";

            return false;
        }


        return true;
    }



    protected bool DeleteGameData()
    {
        if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
            return false;

        return games?.Remove(GetType()) ?? false;
    }


    protected T GetGameData()
    {
        if (!TryGetGameData(out var gameData))
            throw new KeyNotFoundException($"Game data was not found. Guild Id: {Context.Guild.Id}, Game type: {GetType()}");

        return gameData;
    }

    protected bool TryGetGameData([NotNullWhen(true)] out T? gameData)
    {
        var type = GetType();

        gameData = null;

        if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
            return false;

        return games.TryGetValue(type, out gameData);
    }
}