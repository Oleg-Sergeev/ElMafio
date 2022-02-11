using Core.Extensions;
using Discord;

namespace Core.Common;

public static class EmbedHelper
{
    public static Embed CreateEmbed(string description, string? title = null, EmbedBuilder? innerEmbedBuilder = null)
        => CreateEmbed(description, EmbedStyle.Information, title, innerEmbedBuilder);
    public static Embed CreateEmbed(string description, EmbedStyle embedStyle, string? title = null, EmbedBuilder? innerEmbedBuilder = null)
    {
        innerEmbedBuilder ??= new EmbedBuilder();

        innerEmbedBuilder.WithDescription(description);

        innerEmbedBuilder = embedStyle switch
        {
            EmbedStyle.Error => innerEmbedBuilder.WithErrorMessage(),
            EmbedStyle.Warning => innerEmbedBuilder.WithWarningMessage(),
            EmbedStyle.Successfull => innerEmbedBuilder.WithSuccessfullyMessage(),
            EmbedStyle.Waiting => innerEmbedBuilder.WithWaitingMessage(),
            EmbedStyle.Debug => innerEmbedBuilder.WithDebugMessage(),
            _ => innerEmbedBuilder.WithInformationMessage()
        };

        if (title is not null)
            innerEmbedBuilder.WithTitle(title);

        return innerEmbedBuilder.Build();
    }

    public static Embed CreateEmbedStamp(string description, EmbedStyle embedStyle, string? title = null, IUser? userAuthor = null, IUser? userFooter = null, EmbedBuilder? innerEmbedBuilder = null)
    {
        innerEmbedBuilder ??= new EmbedBuilder()
            .WithCurrentTimestamp();

        if (userAuthor is not null)
            innerEmbedBuilder.WithUserAuthor(userAuthor);

        if (userFooter is not null)
            innerEmbedBuilder.WithUserAuthor(userFooter);

        var embed = CreateEmbed(description, embedStyle, title, innerEmbedBuilder);

        return embed;
    }
}
