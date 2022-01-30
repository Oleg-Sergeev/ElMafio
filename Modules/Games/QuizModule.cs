﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.Common.Data;
using Core.Extensions;
using Core.Interfaces;
using Discord;
using Discord.Commands;
using Discord.Net;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Trivia4NET;
using Trivia4NET.Entities;
using Trivia4NET.Payloads;

namespace Modules.Games;

[RequireOwner]
public class QuizModule : GameModule
{
    public QuizModule(InteractiveService interactiveService) : base(interactiveService)
    {
    }


    protected override GameData CreateGameData(IGuildUser creator)
        => new("Викторина", 1, creator);


    public override async Task StartAsync()
    {
        if (!CheckPreconditions(out var msg))
        {
            await ReplyEmbedAsync(msg, EmbedStyle.Error);

            return;
        }


        await PlayAsync();
    }

    private async Task PlayAsync()
    {
        var data = GetGameData();

        var questionsCountMessage = await NextMessageAsync("Введите кол-во вопросов (1-50)");
        var questionsCount = 20;

        if (!questionsCountMessage.IsSuccess)
            await ReplyEmbedAsync($"Кол-во вопросов: {questionsCount}", EmbedStyle.Warning);

        if (int.TryParse(questionsCountMessage.Value?.Content, out var count))
        {
            questionsCount = Math.Clamp(count, 1, 50);

            await ReplyEmbedAsync($"Кол-во вопросов: {questionsCount}", EmbedStyle.Successfull);
        }

        var triviaService = new TriviaService();

        var response = await triviaService.RequestTokenAsync();
        var token = response.SessionToken;

        var questionResponse = await triviaService.GetQuestionsAsync(token, questionsCount);

        var num = 1;

        foreach (var question in questionResponse.Questions)
        {
            var component = new ComponentBuilder()
                .WithButton("Ответить", "answer")
                .Build();
            
            var msg = await ReplyAsync(embed: GenerateQuestionPageEmbed(num, question), components: component);

            var f = await Interactive.NextMessageComponentAsync(m =>
            {
                return m.Message.Id == msg.Id && data.Players.Any(p => p.Id == m.User.Id);
            }, timeout: TimeSpan.FromSeconds(15));

            if (!f.IsSuccess)
                continue;

            await f.Value!.DeferAsync();

            var selection = GenerateSelection(question, num++, f.Value!.User);

            var res = await Interactive.SendSelectionAsync(selection, msg, TimeSpan.FromSeconds(15));

            if (res.IsSuccess)
            {
                if (res.Value == question.Answer)
                    await ReplyEmbedAsync($"{f.Value!.User.Mention} ответил верно", EmbedStyle.Successfull);
            }
        }
    }


    private static Embed GenerateQuestionPageEmbed(int num, Question question)
        => new EmbedBuilder()
        .WithTitle($"Вопрос #{num}")
        .WithColor(question.Difficulty switch { Difficulty.Easy => Color.Green, Difficulty.Medium => Color.LightOrange, _ => Color.Red })
        .WithDescription(question.Content)
        .AddField("Категория", question.Category, true)
        .AddField("Сложность", question.Difficulty, true)
        .Build();

    private static PageBuilder GenerateQuestionPage(int num, Question question)
        => new PageBuilder()
        .WithTitle($"Вопрос #{num}")
        .WithColor(question.Difficulty switch { Difficulty.Easy => Color.Green, Difficulty.Medium => Color.LightOrange, _ => Color.Red })
        .WithDescription(question.Content)
        .AddField("Категория", question.Category, true)
        .AddField("Сложность", question.Difficulty, true);

    private Selection<string> GenerateSelection(Question question, int num, IUser user)
    {
        var options = ConcatQuestions(question);

        var pageBuilder = GenerateQuestionPage(num, question);

        var selection = new SelectionBuilder<string>()
            .AddUser(user)
            .WithOptions(options)
            .WithInputType(InputType.Buttons)
            .WithSelectionPage(pageBuilder)
            .WithAllowCancel(false)
            .Build();

        return selection;
    }

    private IList<string> ConcatQuestions(Question question)
    {
        var list = new List<string>(question.IncorrectAnswers)
        {
            question.Answer
        };

        return list.Shuffle();
    }
}