﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Data;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Infrastructure.Data.Entities.Games.Settings;
using Infrastructure.Data.Entities.Games.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Games.Services;

namespace Modules.Games.RussianRoullete;

[Name("Русская рулетка")]
[Group("Рулетка")]
[Alias("р")]
public class RussianRouletteModule : GameModule<RussianRouletteStats>
{
    private readonly IGameSettingsService<RussianRouletteSettings> _settingsService;

    public RussianRouletteModule(InteractiveService interactiveService, IGameSettingsService<RussianRouletteSettings> settingsService) : base(interactiveService)
    {
        _settingsService = settingsService;
    }


    protected override GameData CreateGameData(IGuildUser creator)
        => new("Русская рулетка", 2, creator);



    [RequireBotPermission(GuildPermission.AddReactions)]
    public override async Task StartAsync()
    {
        var check = await CheckPreconditionsAsync();

        if (!check.IsSuccess)
        {
            await ReplyEmbedAsync(check.ErrorReason, EmbedStyle.Error);

            return;
        }


        var data = GetGameData();

        var playersIds = new HashSet<ulong>(data.Players.Select(p => p.Id));


        await ReplyEmbedStampAsync($"{data.Name} начинается!");

        var delay = Task.Delay(3000);

        var rrData = await CreateRussianRoulleteDataAsync();

        await delay;

        var winner = await PlayAsync(rrData);


        if (data.IsPlaying && winner is not null)
        {
            await Task.Delay(2000);


            await ReplyAsync($"И абсолютным чемпионом русской рулетки становится {winner.Mention}! Поздравляем!");


            await Task.Delay(1000);

            await ReplyEmbedAsync("Игра завершена!", EmbedStyle.Successfull);

            await UpdateStatsAsync(playersIds, winner.Id);
        }


        DeleteGameData();
    }


    private async Task<IGuildUser?> PlayAsync(RussianRoulleteData rrData)
    {
        await ReplyEmbedAsync("Крутим барабан...", EmbedStyle.Waiting);


        var delay = Task.Delay(3000);


        var data = GetGameData();
        data.Players.Shuffle();

        data.IsPlaying = true;

        await delay;


        var alivePlayers = new List<IGuildUser>(data.Players);

        var hasCustomSmileKilled = rrData.CustomEmoteKilled is not null;
        var hasCustomSmileSurvived = rrData.CustomEmoteSurvived is not null;

        while (alivePlayers.Count > 1 && data.IsPlaying)
        {
            for (int drum = 1, i = alivePlayers.Count; drum <= 6 && data.IsPlaying; drum++)
            {
                if (--i < 0)
                    i = alivePlayers.Count - 1;


                await ReplyEmbedAsync($"На очереди {alivePlayers[i].Mention}");

                await Task.Delay(3000);

                if (!data.IsPlaying)
                    return null;


                bool hasKilled = Random.NextDouble() < (double)drum / 6;

                if (hasKilled)
                {
                    var messages = new string[4]
                    {
                            "ловит пулю своим стальным лбом",
                            "получает маслину",
                            "просто сдыхает",
                            "героически останавливает пулю ценой своей никчемной жизни"
                    };


                    var msg = await ReplyAsync($"{alivePlayers[i].Mention} жмет на курок и {messages[Random.Next(messages.Length)]}");


                    if (hasCustomSmileKilled && Context.Guild.Emotes.Contains(rrData.CustomEmoteKilled))
                        await msg.AddReactionAsync(rrData.CustomEmoteKilled);
                    else
                        await msg.AddReactionAsync(rrData.UnicodeEmojiKilled);




                    alivePlayers.RemoveAt(i);

                    if (alivePlayers.Count < 2)
                        break;

                    alivePlayers.Shuffle();


                    await ReplyEmbedAsync($"Не будем унывать, в нашей беспроигрышной лотерее осталось еще {alivePlayers.Count} счастливчиков!");

                    await Task.Delay(2000);

                    await ReplyEmbedAsync("Раскручиваем барабан...", EmbedStyle.Waiting);

                    await Task.Delay(4000);

                    break;
                }
                else
                {
                    var messages = new string[4]
                    {
                            "посылает нахер смерть",
                            "получает шанс пожить еще пару минут",
                            "спокойно передает револьвер следующему участнику",
                            "жалеет, что остался в живых"
                    };

                    var msg = await ReplyAsync($"{alivePlayers[i].Mention} жмет на курок и {messages[Random.Next(messages.Length)]}");


                    if (hasCustomSmileSurvived && Context.Guild.Emotes.Contains(rrData.CustomEmoteSurvived))
                        await msg.AddReactionAsync(rrData.CustomEmoteSurvived);
                    else
                        await msg.AddReactionAsync(rrData.UnicodeEmojiSurvived);


                    await ReplyEmbedAsync("А мы идем дальше");
                }

                await Task.Delay(4000);
            }
        }


        return alivePlayers[0];
    }


    private async Task<RussianRoulleteData> CreateRussianRoulleteDataAsync()
    {
        var rrSettings = await _settingsService.GetSettingsOrCreateAsync(Context, false);

        var rrdata = new RussianRoulleteData(new Emoji(rrSettings.UnicodeSmileKilled), new Emoji(rrSettings.UnicodeSmileSurvived))
        {
            CustomEmoteKilled = rrSettings.CustomSmileKilled is not null ? Emote.Parse(rrSettings.CustomSmileKilled) : null,
            CustomEmoteSurvived = rrSettings.CustomSmileSurvived is not null ? Emote.Parse(rrSettings.CustomSmileSurvived) : null,
        };

        return rrdata;
    }


    private async Task<int> UpdateStatsAsync(IEnumerable<ulong> playersIds, ulong winnerId)
    {
        var stats = await GetStatsWithAddingNewAsync(playersIds);

        stats[winnerId].WinsCount++;

        foreach (var stat in stats.Values)
            stat.GamesCount++;

        return await Context.Db.SaveChangesAsync();
    }



    private class RussianRoulleteData
    {
        public Emoji UnicodeEmojiKilled { get; }
        public Emoji UnicodeEmojiSurvived { get; }

        public Emote? CustomEmoteKilled { get; init; }
        public Emote? CustomEmoteSurvived { get; init; }


        public RussianRoulleteData(Emoji unicodeEmojiKilled, Emoji unicodeEmojiSurvived)
        {
            UnicodeEmojiKilled = unicodeEmojiKilled;
            UnicodeEmojiSurvived = unicodeEmojiSurvived;
        }
    }




    public class RussianRoulleteStatsModule : GameStatsModule
    {
        public RussianRoulleteStatsModule(InteractiveService interactiveService) : base(interactiveService)
        {
        }


        public class RussianRoulleteAdminModule : GameAdminModule
        {
            public RussianRoulleteAdminModule(InteractiveService interactiveService) : base(interactiveService)
            {
            }
        }
    }


    [Group("Смайлы")]
    [Summary("Раздел для настройки смайлов, добавляющихся после каждого нажатия курка")]
    [RequireUserPermission(GuildPermission.Administrator, Group = "perm")]
    [RequireOwner(Group = "perm")]
    [RequireBotPermission(GuildPermission.AddReactions)]
    public class SetSmileModule : CommandGuildModuleBase
    {
        private readonly IGameSettingsService<RussianRouletteSettings> _settingsService;

        public SetSmileModule(InteractiveService interactiveService, IGameSettingsService<RussianRouletteSettings> settingsService) : base(interactiveService)
        {
            _settingsService = settingsService;
        }


        [Command("удалить")]
        public async Task RemoveCustomSmilesAsync()
        {
            await RemoveCustomSmileKilled();

            await RemoveCustomSmileSurvived();
        }

        [Command("удалитьубил")]
        public async Task RemoveCustomSmileKilled()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            settings.CustomSmileKilled = null;

            await Context.Db.SaveChangesAsync();
        }

        [Command("удалитьвыжил")]
        public async Task RemoveCustomSmileSurvived()
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            settings.CustomSmileSurvived = null;

            await Context.Db.SaveChangesAsync();
        }



        [Priority(-1)]
        [Command("убил")]
        public async Task SetSmileKilledAsync(Emoji emoji)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            settings.UnicodeSmileKilled = emoji.ToString();

            await Context.Db.SaveChangesAsync();


            var msg = await ReplyEmbedAsync($"Смайл {emoji} успешно настроен", EmbedStyle.Successfull);

            await msg.AddReactionAsync(emoji);
        }

        [Command("убил")]
        public async Task SetSmileKilledAsync(Emote emote)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            settings.CustomSmileKilled = emote.ToString();

            await Context.Db.SaveChangesAsync();


            var msg = await ReplyEmbedAsync($"Смайл {emote} успешно настроен", EmbedStyle.Successfull);

            await msg.AddReactionAsync(emote);
        }



        [Priority(-1)]
        [Command("выжил")]
        public async Task SetSmileSuvivedAsync(Emoji emoji)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            settings.UnicodeSmileSurvived = emoji.ToString();

            await Context.Db.SaveChangesAsync();


            var msg = await ReplyEmbedAsync($"Смайл {emoji} успешно настроен", EmbedStyle.Successfull);

            await msg.AddReactionAsync(emoji);
        }

        [Command("выжил")]
        public async Task SetSmileSuvivedAsync(Emote emote)
        {
            var settings = await _settingsService.GetSettingsOrCreateAsync(Context);

            settings.CustomSmileSurvived = emote.ToString();

            await Context.Db.SaveChangesAsync();


            var msg = await ReplyEmbedAsync($"Смайл {emote} успешно настроен", EmbedStyle.Successfull);

            await msg.AddReactionAsync(emote);
        }
    }



    public class RussianRouletteHelpModule : HelpModule
    {
        public RussianRouletteHelpModule(InteractiveService interactiveService, IConfiguration configuration)
            : base(interactiveService, configuration)
        {
        }
    }
}