using Discord;

namespace Core.Extensions;

public static class EmbedBuilderExtensions
{
    private const char EmptySpace = '⠀';

    private static readonly Color ColorInformation = new(52, 104, 194);
    private static readonly Color ColorSuccessfully = new(35, 115, 13);
    private static readonly Color ColorWarning = new(232, 120, 0);
    private static readonly Color ColorError = new(214, 15, 15);

    public static EmbedBuilder AddEmptyField(this EmbedBuilder builder, bool inline = false)
        => builder.AddField(EmptySpace.ToString(), EmptySpace, inline);

    public static EmbedBuilder AddFieldWithEmptyName(this EmbedBuilder builder, string description, bool inline = false)
        => builder.AddField(EmptySpace.ToString(), description, inline);


    public static EmbedBuilder WithErrorMessage(this EmbedBuilder builder)
        => builder
        .WithDescription($"{builder.Description} ⛔")
        .WithColor(ColorError);

    public static EmbedBuilder WithWarningMessage(this EmbedBuilder builder)
        => builder
        .WithDescription($"{builder.Description} ⚠")
        .WithColor(ColorWarning);

    public static EmbedBuilder WithSuccessfullyMessage(this EmbedBuilder builder)
        => builder
        .WithDescription($"{builder.Description} ✅")
        .WithColor(ColorSuccessfully);

    public static EmbedBuilder WithInformationMessage(this EmbedBuilder builder, bool addSmile = false)
        => builder
        .WithDescription(addSmile ? $"ℹ️{EmptySpace}{builder.Description}" : builder.Description)
        .WithColor(ColorInformation);


    public static EmbedBuilder WithUserFooter(this EmbedBuilder builder, IUser user)
        => builder.WithFooter(user.GetFullName(), user.GetAvatarUrlOrDefaultAvatarUrl());

    public static EmbedBuilder WithUserAuthor(this EmbedBuilder builder, IUser user)
        => builder.WithAuthor(user.GetFullName(), user.GetAvatarUrlOrDefaultAvatarUrl());
}