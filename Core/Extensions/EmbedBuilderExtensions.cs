﻿using Discord;

namespace Core.Extensions;

public static class EmbedBuilderExtensions
{
    private const char EmptySpace = '⠀';

    public static EmbedBuilder AddEmptyField(this EmbedBuilder builder, bool inline = false)
        => builder.AddField(EmptySpace.ToString(), EmptySpace, inline);

    public static EmbedBuilder AddFieldWithEmptyName(this EmbedBuilder builder, string description, bool inline = false)
        => builder.AddField(EmptySpace.ToString(), description, inline);


    public static EmbedBuilder WithErrorMessage(this EmbedBuilder builder, bool addSmiles = true)
        => builder
        .WithDescription(addSmiles ? $"{builder.Description} ⛔" : builder.Description)
        .WithColor(214, 15, 15);

    public static EmbedBuilder WithWarningMessage(this EmbedBuilder builder, bool addSmiles = true)
        => builder
        .WithDescription(addSmiles ? $"{builder.Description} ⚠" : builder.Description)
        .WithColor(232, 120, 0);

    public static EmbedBuilder WithSuccessfullyMessage(this EmbedBuilder builder, bool addSmiles = true)
        => builder
        .WithDescription(addSmiles ? $"{builder.Description} ✅" : builder.Description)
        .WithColor(35, 115, 13);

    public static EmbedBuilder WithInformationMessage(this EmbedBuilder builder, bool addSmiles = true)
        => builder
        .WithDescription(addSmiles ? $"ℹ️{EmptySpace}{builder.Description}" : builder.Description)
        .WithColor(52, 104, 194);


    public static EmbedBuilder WithUserFooter(this EmbedBuilder builder, IUser user)
        => builder.WithFooter(user.GetFullName(), user.GetAvatarUrlOrDefaultAvatarUrl());
}