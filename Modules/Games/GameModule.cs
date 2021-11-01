using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Modules.Extensions;

namespace Modules.Games
{
    public abstract class GameModule : ModuleBase<SocketCommandContext>
    {
        protected Random Random { get; }


        protected static Dictionary<ulong, Dictionary<Type, GameModuleData>> GamesData { get; }

        protected GameModuleData? GameData { get; set; }


        static GameModule()
        {
            GamesData = new();
        }

        public GameModule(Random random)
        {
            Random = random;
        }


        [Command("создатель")]
        public virtual async Task ShowCreator()
        {
            GameData = GetGameData();

            if (GameData is not null)
            {
                await ReplyAsync($"Создатель - {GameData.Creator.GetFullName()}");
            }
            else
            {
                await ReplyAsync("{GameData.Name} еще не создана");
            }
        }


        [Command("список")]
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
                text += $"{player.GetFullName()} - {(GameData.Creator.Id == player.Id ? "создатель" : "участник")}\n";

            await ReplyAsync(text);
        }


        [Priority(1)]
        [Command("игра")]
        public async Task JoinAsync()
        {
            var guildUser = (IGuildUser)Context.User;

            await JoinAsync(guildUser);
        }

        [Priority(0)]
        [Command("игра")]
        [RequireOwner()]
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


        [Command("выход")]
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

                    if (GameData.Players.Count == 0) { /*Stop(message);*/}
                    else if (GameData.Creator.Id == guildUser.Id) GameData.Creator = GameData.Players[0];
                }
                else await ReplyAsync("Вы не можете выйти: вы не участник");
            }
        }


        [Command("стоп")]
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

            await ReplyAsync($"{GameData.Name} остановлена администратором");
        }



        [Command("старт")]
        public abstract Task StartAsync();


        [Command("статистика")]
        [Alias("стат")]
        public abstract Task ShowStatsAsync();


        [Command("статистика")]
        [Alias("стат")]
        public abstract Task ShowStatsAsync(IUser user);


        [Command("рейтинг")]
        [Alias("топ", "рейт")]
        public abstract Task ShowRating();


        protected abstract GameModuleData CreateGameData(IGuildUser creator);


        protected bool CanStart()
        {
            if (GameData is null) return false;

            if (GameData.Players.Count < GameData.MinPlayersCount) return false;

            if (GameData.IsPlaying) return false;

            if (GameData.Creator.Id != Context.User.Id && Context.User.Id != Context.Guild.OwnerId) return false;


            return true;
        }

        protected GameModuleData? GetGameData()
        {
            var type = GetType();

            if (!GamesData.TryGetValue(Context.Guild.Id, out var games))
                return null;

            return games.GetValueOrDefault(type);
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
