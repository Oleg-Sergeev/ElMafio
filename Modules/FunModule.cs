using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Common;
using Core.Extensions;
using Core.Interfaces;
using Discord;
using Discord.Commands;

namespace Modules;

[Name("Фан")]
public class FunModule : GuildModuleBase
{
    private readonly IRandomService _random;


    public FunModule(IRandomService random)
    {
        _random = random;
    }


    [Command("Шанс")]
    public async Task CalculateChanceAsync([Remainder] string text)
    {
        int num = _random.Next(101);

        await ReplyAsync(embed: CreateEmbed(EmbedStyle.Information, $"Шанс того, что {text} - **{num}%**"),
            messageReference: new(Context.Message.Id));
    }


    [Command("Эхо")]
    public async Task Echo([Remainder] string message)
    {
        await Context.Message.DeleteAsync();

        await ReplyAsync(message, messageReference: Context.Message.Reference);
    }


    [Command("Данет")]
    public async Task SayYesOrNo()
    {
        bool answer = _random.Next(2) > 0;

        if (answer)
            await ReplyAsync("**Да**");
        else
            await ReplyAsync("**Нет**");
    }


    [Command("Кто")]
    public async Task SayWhoIs([Remainder] string? message = null)
    {
        int num = _random.Next(0, Context.Guild.Users.Count);

        var user = Context.Guild.Users.ToList()[num];
        var nickname = user.Nickname ?? user.Username;

        await ReplyAsync(embed: CreateEmbed(EmbedStyle.Information, $"**{nickname}** {message}"),
            messageReference: new(Context.Message.Id));
    }

    [Command("Голосование")]
    [Alias("голос", "гс")]
    public async Task MakeVotingAsync([Remainder] string text)
    {
        var content = text.Split("|");

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

            points.AddRange(content[1].Split());

            for (int i = 0; i < Math.Min(points.Count, 20); i++)
            {
                options += $"{(char)(i + 65)} - {points[i]}\n";

                emotes.Add(new Emoji(((char)(i + 65)).ConvertToSmile()));
            }
        }

        var response = await ReplyEmbedStampAsync(EmbedStyle.Information, options, title);

        await response.AddReactionsAsync(emotes.ToArray());
    }

    [Command("Буквы")]
    public async Task TransferToLetterSmilesAsync([Remainder] string text)
    {
        var letters = "";

        foreach (var letter in text[..Math.Min(25, text.Length)])
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
    public async Task TransferToLetterSmilesAsyncAndEcho([Remainder] string text)
    {
        await TransferToLetterSmilesAsync(text);

        await Context.Message.DeleteAsync();
    }


    [Command("Аватар")]
    [Priority(-1)]
    public async Task GetAvatarAsync()
        => await GetAvatarAsync(Context.User);

    [Command("Аватар")]
    public async Task GetAvatarAsync(IUser user)
    {
        var request = user.GetAvatarUrl(size: 2048);

        var resp = await new HttpClient().GetAsync(request);
        var stream = await resp.Content.ReadAsStreamAsync();

        if (resp is null)
        {
            await ReplyEmbedAsync(EmbedStyle.Error, "Не удалось загрузить аватар");

            return;
        }

        var avatarExtension = resp.Content.Headers.ContentType!.MediaType!.Split('/')[1];

        await Context.Channel.SendFileAsync(stream, $"avatar.{avatarExtension}");
    }



    [Command("Смайл")]
    public async Task GetSmileAsync(string emoteId)
    {
        if (Emote.TryParse(emoteId, out var emote))
            await GetSmileByIdAsync(emote.Id);
        else
            await ReplyEmbedAsync(EmbedStyle.Error, "Не удалось распознать пользовательский смайлик");
    }


    [Command("СмайлАйди")]
    public async Task GetSmileByIdAsync(ulong smileId)
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

        await ReplyEmbedAsync(EmbedStyle.Error, "Не удалось найти пользовательский смайлик");
    }

}