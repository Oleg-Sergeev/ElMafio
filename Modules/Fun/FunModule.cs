﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Infrastructure.Data.Entities.ServerInfo;
using Modules.Common.Preconditions.Commands;

namespace Modules.Fun;

[Name("Фан")]
public class FunModule : CommandGuildModuleBase
{
    public FunModule(InteractiveService interactiveService) : base(interactiveService) { }


    [Command("Шанс")]
    [Summary("С каким шансом случится указанное событие")]
    public async Task CalculateChanceAsync([Remainder][Summary("Текст события")] string? text = null)
    {
        int num = Random.Next(101);

        var msg = $"Шанс того, что {text} - ";

        await ReplyAsync(embed: EmbedHelper.CreateEmbed($"{(!string.IsNullOrEmpty(text) ? msg : string.Empty)}**{num}%**"),
            messageReference: new(Context.Message.Id));
    }


    [Command("Данет")]
    [Summary("Да, или нет")]
    public async Task SayYesOrNoAsync()
    {
        bool answer = Random.Next(2) > 0;

        if (answer)
            await ReplyEmbedAsync("`Да`", EmbedStyle.Successfull);
        else
            await ReplyEmbedAsync("`Нет`", EmbedStyle.Error);
    }


    [Command("Кто")]
    [Summary("Узнайте, кто сделал это")]
    public async Task SayWhoIsAsync([Remainder][Summary("Событие, которое кто-то сделал")] string? message = null)
    {
        int num = Random.Next(0, Context.Guild.Users.Count);

        var user = Context.Guild.Users.ToList()[num];
        var nickname = user.Nickname ?? user.Username;

        await ReplyAsync(embed: EmbedHelper.CreateEmbed($"**{nickname}** {message}"),
            messageReference: new(Context.Message.Id));
    }


    [Command("Колесо")]
    [Summary("Укажите набор слов, из которых бот выберет одно" +
        "\nПример: `Рулетка Арбуз, Дыня, Груша и яблоко, Что-нибудь еще вкусное`")]
    public async Task RouletteAsync([Remainder][Summary("Набор слов, участвующих в рулетке")] string text)
    {
        var arr = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (arr.Length == 0)
        {
            await ReplyEmbedAsync("Пожалуйста, укажите набор слов через запятую");

            return;
        }

        int num = Random.Next(0, arr.Length);

        await ReplyEmbedAsync($"На рулетке выпадает ЭТО - **`{arr[num]}`**");
    }


    [Command("Рулетка")]
    [Alias("Колесо")]
    [Priority(1)]
    public async Task RouletteAsync(int min, int max)
    {
        if (min > max)
            (min, max) = (max, min);

        int num = Random.Next(min, max + 1);

        await ReplyEmbedAsync($"На рулетке выпадает ЭТО - **`{num}`**");
    }


    [Command("Голосование")]
    [Alias("голос", "гс")]
    [Summary("Устройте голосование, чтобы узнать мнение людей")]
    [Remarks("Чтобы добавить в голосование собственные варианты ответа, " +
        "после заголовка голосования вставьте символ `/` и после него, через запятую, укажите свои варианты ответа" +
        "\nПример: `Голосование Что лучше и вкуснее / Арбуз, Конечно арбуз, Ну а что еще кроме арбуза, дыня`")]
    [RequireBotPermission(GuildPermission.AddReactions)]
    public async Task MakeVotingAsync([Remainder][Summary("Заголовок голосования и варианты ответа (опционально)")] string text)
    {
        var content = text.Split("/");

        string? title = null;

        if (!string.IsNullOrEmpty(content[0]))
            title = content[0];

        var options = "";

        var points = new List<string>();
        var emotes = new List<IEmote>();

        if (content.Length == 1 || string.IsNullOrWhiteSpace(content[1]))
        {
            emotes.Add(new Emoji("✅"));
            emotes.Add(new Emoji("❌"));
        }
        else
        {
            content[1] = content[1].Trim();

            points.AddRange(content[1].Split(','));

            for (int i = 0; i < Math.Min(points.Count, 20); i++)
            {
                options += $"{(char)(i + 65)} - {points[i]}\n";

                emotes.Add(new Emoji(((char)(i + 65)).ConvertToSmile()));
            }
        }

        var response = await ReplyEmbedStampAsync(options, EmbedStyle.Information, title);

        await response.AddReactionsAsync(emotes.ToArray());
    }



    [Command("Эхо")]
    [Summary("Напишите от лица бота любую вещь")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public async Task EchoAsync([Remainder][Summary("Текст")] string text)
    {
        await Context.Message.DeleteAsync();

        await ReplyAsync(text, allowedMentions: AllowedMentions.None, messageReference: Context.Message.Reference);
    }


    [Command("Буквы")]
    [Summary("Выведите сообщение огромными буквами, чтобы его точно заметили")]
    [Remarks("Команда поддерживает перевод кириллицы в латиницу" +
        "\nСпец. символы, а также некоторые символы кириллицы не переводятся в буквы и заменяются пробелом" +
        "\nМаксимальная длина сообщения - **30 символов**")]
    public async Task TransferToLetterSmilesAsync([Remainder][Summary("Текст, который нужно перевести в большие буквы")] string text)
    {
        var letters = "";

        foreach (var letter in text[..Math.Min(30, text.Length)])
        {
            if (letter.TryConvertToSmile(out var smile))
                letters += smile;
            else
                letters += ' ';

            letters += '\u202F';
        }

        if (!string.IsNullOrWhiteSpace(letters))
            await ReplyAsync(letters, messageReference: Context.Message.Reference);
    }


    [Command("Эхобуквы")]
    [Summary("Выведите сообщение огромными буквами от лица бота, чтобы его точно заметили")]
    [Remarks("Команда поддерживает перевод кириллицы в латиницу" +
        "\nСпец. символы, а также некоторые символы кириллицы не переводятся в буквы и заменяются пробелом" +
        "\nМаксимальная длина сообщения - **30 символов**")]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    public async Task TransferToLetterSmilesAsyncAndEchoAsync([Remainder][Summary("Текст, который нужно перевести в большие буквы")] string text)
    {
        await TransferToLetterSmilesAsync(text);

        await Context.Message.DeleteAsync();
    }


    [Command("Аватар")]
    [Summary("Получить свой аватар, или аватар указанного пользователя")]
    [RequireBotPermission(GuildPermission.AttachFiles)]
    public async Task GetAvatarAsync([Summary("Пользователь, аватар которого нужно получить")] IUser? user = null)
    {
        user ??= Context.User;

        var request = user.GetAvatarUrl(size: 2048);

        var resp = await new HttpClient().GetAsync(request);
        var stream = await resp.Content.ReadAsStreamAsync();

        if (resp is null)
        {
            await ReplyEmbedAsync("Не удалось загрузить аватар", EmbedStyle.Error);

            return;
        }

        var avatarExtension = resp.Content.Headers.ContentType?.MediaType?.Split('/')?[1];

        if (avatarExtension is null)
        {
            await ReplyEmbedAsync("Не удалось загрузить аватар: не найдено расширение картинки", EmbedStyle.Error);

            return;
        }

        await Context.Channel.SendFileAsync(stream, $"avatar.{avatarExtension}");
    }



    [Command("Смайл")]
    [Alias("Эмодзи", "Эмоция")]
    [Summary("Получить картинку указанного смайла")]
    [Remarks("Если смайл имеет анимацию, то будет получена анимированная версия. При отсутствии анимированной версии, будет получена картинка")]
    [RequireBotPermission(GuildPermission.AttachFiles)]
    public Task GetSmileAsync([Summary("Пользовательский смайл")] Emote emote)
        => GetSmileByIdAsync(emote.Id);


    [Command("СмайлАйди")]
    [Alias("ЭмодзиАйди", "ЭмоцияАйди")]
    [Summary("Получить смайл по его ID")]
    [Remarks("Для получения ID смайла, поставьте `\\` и затем укажите сам смайл. Смайл будет преобразован в вид `<:smileName:smileId>`. " +
        "Для команды необходим ID, т.е. часть `smileId`")]
    [RequireBotPermission(GuildPermission.AttachFiles)]
    public async Task GetSmileByIdAsync([Summary("ID пользовательского смайла")] ulong smileId)
    {
        var hhtpClient = new HttpClient();

        var resp = await hhtpClient.GetAsync($"https://cdn.discordapp.com/emojis/{smileId}.gif");

        if (!resp.IsSuccessStatusCode)
            resp = await hhtpClient.GetAsync($"https://cdn.discordapp.com/emojis/{smileId}.png");


        if (resp.IsSuccessStatusCode)
        {
            var stream = await resp.Content.ReadAsStreamAsync();

            var smileExtension = resp.Content.Headers.ContentType!.MediaType!.Split('/')[1];

            await Context.Channel.SendFileAsync(stream, $"smile.{smileExtension}", messageReference: new(Context.Message.Id));

            return;
        }

        await ReplyEmbedAsync("Не удалось найти пользовательский смайлик", EmbedStyle.Error);
    }


    [Command("СмайлАйди")]
    [Alias("ЭмодзиАйди", "ЭмоцияАйди")]
    [Summary("Получить смайл по его ID")]
    [Remarks("Для получения ID смайла, поставьте `\\` и затем укажите сам смайл. Смайл будет преобразован в вид `<:smileName:smileId>`")]
    [RequireBotPermission(GuildPermission.AttachFiles)]
    public async Task GetSmileByIdAsync([Summary("ID пользовательского смайла")] string smileRawId)
    {
        var trimmedRawId = smileRawId.Trim('<', '>', ':');

        var smileStrId = trimmedRawId.Split(':')[^1];

        if (ulong.TryParse(smileStrId, out var smileId))
        {
            var msg = "Не удалось найти ID пользовательского смайла." +
                "\nФормат вводимого ID смайла:" +
                "\n`<:smileName:smileId>` (`*смайлайди <:mySmile:123456789012345678>`)" +
                "\n`smileId` (`*смайлайди 123456789012345678`)";

            await ReplyEmbedAsync(msg, EmbedStyle.Error);

            return;
        }

        await GetSmileByIdAsync(smileId);
    }

}