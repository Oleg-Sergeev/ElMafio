using Discord;

namespace Core.Common;

public class MessageData
{
    public string? Message { get; init; }

    public bool IsTTS { get; init; }

    public Embed? Embed { get; init; }

    public AllowedMentions? AllowedMentions { get; init; }

    public RequestOptions? RequestOptions { get; init; }

    public MessageReference? MessageReference { get; init; }

    public MessageComponent? MessageComponent { get; init; }

    public ISticker[]? Stickers { get; init; }

    public Embed[]? Embeds { get; init; }
}
