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
            _ => innerEmbedBuilder.WithInformationMessage()
        };

        if (title is not null)
            innerEmbedBuilder.WithTitle(title);

        return innerEmbedBuilder.Build();
    }
}
