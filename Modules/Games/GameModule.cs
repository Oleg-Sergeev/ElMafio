using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Infrastructure.Data;
using Infrastructure.Data.Models;
using Infrastructure.Data.Models.Games.Settings;
using Infrastructure.Data.Models.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Modules.Extensions;

namespace Modules.Games
{
    [RequireContext(ContextType.Guild)]
    public abstract class GameModule : ModuleBase<SocketCommandContext>
    {
        protected static Dictionary<ulong, Dictionary<Type, GameModuleData>> GamesData { get; } = new();


        protected readonly BotContext _db;

        protected GameModuleData? GameData { get; set; }

        protected Random Random { get; }


        public GameModule(Random random, BotContext db)
        {
            Random = random;
            _db = db;
        }


        [Command("Создатель")]
        [Summary("Показать создателя игры")]
        [Remarks("Только создатель может запустить игру")]
        public virtual async Task ShowCreator()
        {
            GameData = GetGameData();

            if (GameData is not null)
                await ReplyAsync($"Создатель - **{GameData.Creator.GetFullName()}**");
            else
                await ReplyAsync("Игра еще не создана");
        }


        [Command("Список")]
        [Summary("Показать список игроков")]
        public virtual async Task ShowPlayerList()
        {
            GameData = GetGameData();

            if (GameData is null)
            {
                await ReplyAsync("Игра еще не создана");

                return;
            }

            var text = $"Кол-во игроков: {GameData.Players.Count}. Список:\n";

            foreach (var player in GameData.Players)
                text += $"**{player.GetFullName()}** - {(GameData.Creator.Id == player.Id ? "создатель" : "участник")}\n";

            await ReplyAsync(text);
        }



        [Priority(0)]
        [Command("Играть")]
        [Alias("игра")]
        [Summary("Создать новую игру или присоединиться к текущей")]
        public async Task JoinAsync()
        {
            var guildUser = (IGuildUser)Context.User;

            await JoinAsync(guildUser);
        }

        [RequireOwner()]
        [Priority(1)]
        [Command("Играть")]
        [Alias("игра")]
        public virtual async Task JoinAsync(IGuildUser guildUser)
        {
            GameData = GetGameData();

            if (GameData is null)
            {
                GameData = CreateGameData(guildUser);
                AddGameDataToGamesList(Context.Guild.Id, GameData);
            }


            if (GameData!.IsPlaying)
            {
                await ReplyAsync($"{GameData.Name} уже запущена. Дождитесь окончания");

                return;
            }


            if (!GameData.Players.Contains(guildUser))
            {
                GameData.Players.Add(guildUser);

                await ReplyAsync($"{guildUser.GetFullMention()} присоединился к игре! Количество участников: {GameData.Players.Count}");
            }
            else await ReplyAsync("Вы уже участвуете!");
        }


        [Command("Выход")]
        [Summary("Покинуть игру")]
        public virtual async Task LeaveAsync()
        {
            GameData = GetGameData();

            if (GameData is null)
            {
                await ReplyAsync("Игра еще не создана");

                return;
            }

            if (GameData.IsPlaying) await ReplyAsync($"{GameData.Name} уже началась, выход невозможен");
            else
            {
                var guildUser = (IGuildUser)Context.User;

                if (GameData.Players.Remove(guildUser))
                {
                    await ReplyAsync($"{guildUser.GetFullMention()} покинул игру. Количество участников: {GameData.Players.Count}");

                    if (GameData.Players.Count == 0) await StopAsync();
                    else if (GameData.Creator.Id == guildUser.Id) GameData.Creator = GameData.Players[0];
                }
                else await ReplyAsync("Вы не можете выйти: вы не участник");
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
                await ReplyAsync("Игра еще не создана");

                return;
            }

            if (GameData.IsPlaying) GameData.IsPlaying = false;
            else DeleteGameData();

            await ReplyAsync($"{GameData.Name} остановлена");
        }


        [Command("Выгнать")]
        [Summary("Выгнать пользователя из игры")]
        [Remarks("Попытка выгнать себя приравнивается выходу из игры")]
        public virtual async Task KickAsync([Summary("Пользователь, которого вы хотите выгнать")] IGuildUser guildUser)
        {
            GameData = GetGameData();


            if (GameData is null)
            {
                await ReplyAsync("Игра еще не создана");

                return;
            }

            if (GameData.Creator.Id != Context.User.Id)
            {
                var ownerId = (await Context.Client.GetApplicationInfoAsync()).Owner.Id;

                if (ownerId != Context.User.Id)
                {
                    await ReplyAsync("Вы не являетесь создателем игры");

                    return;
                }
            }

            if (!GameData.Players.Contains(guildUser))
            {
                await ReplyAsync($"{guildUser.GetFullName()} не участвует в игре");

                return;
            }

            if (Context.User.Id == guildUser.Id)
            {
                await LeaveAsync();

                return;
            }

            if (GameData.IsPlaying)
            {
                await ReplyAsync($"{GameData.Name} уже началась, выгнать игрока невозможно");

                return;
            }

            if (GameData.Players.Remove(guildUser))
                await ReplyAsync($"{guildUser.GetFullMention()} выгнан из игры. Количество участников: {GameData.Players.Count}");
            else
                await ReplyAsync($"Не удалось выгнать {guildUser.GetFullName()}");
        }



        [Command("Старт")]
        [Summary("Запустить игру")]
        public abstract Task StartAsync();





        [Command("Рейтинг")]
        [Alias("топ", "рейт")]
        public abstract Task ShowRating();

        [Command("рейтингсброс")]
        [Alias("топсброс", "рейтсброс")]
        [RequireOwner]
        public abstract Task ResetRatingAsync();

        protected async Task ResetRatingAsync<T>() where T : GameStats
        {
            var userStats = await _db.Set<T>()
                .AsTracking()
                .Where(s => s.GuildId == Context.Guild.Id)
                .ToListAsync();

            if (userStats is null || userStats.Count == 0)
            {
                await ReplyAsync("Рейтинг отсутствует");

                return;
            }

            foreach (var userStat in userStats)
                userStat.Reset();

            await _db.SaveChangesAsync();

            await ReplyAsync("Рейтинг успешно сброшен");
        }


        [Command("Статистика")]
        [Alias("стат", "стата")]
        [Priority(0)]
        public abstract Task ShowStatsAsync();

        [Command("Статистика")]
        [Alias("стат")]
        [Priority(1)]
        public abstract Task ShowStatsAsync(IUser user);


        [Command("статасброс")]
        public Task ResetStatAsync()
            => ResetStatAsync((IGuildUser)Context.User);

        [Command("статасброс")]
        [RequireOwner()]
        public abstract Task ResetStatAsync(IGuildUser guildUser);

        protected async Task ResetStatAsync<T>(IGuildUser guildUser) where T : GameStats
        {
            var userStat = await _db.Set<T>().FindAsync(guildUser.Id, Context.Guild.Id);

            if (userStat is null)
            {
                await ReplyAsync($"Статистика игрока {guildUser.GetFullName()} не найдена");

                return;
            }

            userStat.Reset();


            await _db.SaveChangesAsync();

            await ReplyAsync($"Статистика игрока {guildUser.GetFullName()} успешно сброшена");
        }





        protected abstract GameModuleData CreateGameData(IGuildUser creator);

        protected async Task AddNewUsersAsync(HashSet<ulong> playersId)
        {
            var existingPlayersId = await _db.Users
                .AsNoTracking()
                .Where(u => playersId.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            if (existingPlayersId.Count == playersId.Count) return;


            var newPlayersId = playersId.Except(existingPlayersId);

            var newUsers = GameData!.Players
                .Where(u => newPlayersId.Contains(u.Id))
                .Select(u => new User
                {
                    Id = u.Id,
                    JoinedAt = u.JoinedAt!.Value.DateTime
                })
                .ToList();

            await _db.Users.AddRangeAsync(newUsers);

            await _db.SaveChangesAsync();
        }

        protected GameModuleData? GetGameData()
        {
            var type = GetType();

            if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
                return null;

            return games.GetValueOrDefault(type);
        }

        protected bool CanStart(out string? failMessage)
        {
            failMessage = null;


            if (GameData is null)
            {
                failMessage = "Игра еще не создана";

                return false;
            }

            if (GameData.Players.Count < GameData.MinPlayersCount)
            {
                failMessage = "Недостаточно игроков";

                return false;
            }

            if (GameData.IsPlaying)
            {
                failMessage = $"{GameData.Name} уже запущена";

                return false;
            }

            if (GameData.Creator.Id != Context.User.Id && Context.User.Id != Context.Guild.OwnerId)
            {
                failMessage = "Вы не являетесь создателем";

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
            GamesData.Remove(Context.Guild.Id);
        }


        protected static async Task<T> GetSettingsAsync<T>(BotContext db, ulong guildId, bool isTracking = true) where T : GameSettings, new()
        {
            T? rrSettings;

            if (isTracking)
                rrSettings = await db.Set<T>()
                   .AsTracking()
                   .FirstOrDefaultAsync(s => s.GuildSettingsId == guildId);
            else
                rrSettings = await db.Set<T>()
                   .AsNoTracking()
                   .FirstOrDefaultAsync(s => s.GuildSettingsId == guildId);


            if (rrSettings is null)
                rrSettings = await AddSettingsToDb<T>(db, guildId);


            return rrSettings;
        }

        protected static async Task<T> AddSettingsToDb<T>(BotContext db, ulong guildId) where T : GameSettings, new()
        {
            var settings = new T()
            {
                GuildSettingsId = guildId
            };

            await db.Set<T>().AddAsync(settings);


            await db.SaveChangesAsync();

            return settings;
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
    }
}
