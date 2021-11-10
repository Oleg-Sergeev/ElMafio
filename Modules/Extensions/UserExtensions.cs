using Discord;

namespace Modules.Extensions;

public static class UserExtensions
{
    public static string GetFullName(this IGuildUser guildUser) => $"{guildUser.Nickname ?? guildUser.Username}#{guildUser.Discriminator}";
    public static string GetFullName(this IUser user) => $"{user.Username} #{user.Discriminator}";

    public static string GetFullMention(this IUser user) =>
        $"{user.Mention} #{user.Discriminator}";

    public static string GetAvatarUrlOrDefaultAvatarUrl(this IUser user, ImageFormat format = ImageFormat.Auto, ushort size = 128)
        => user.GetAvatarUrl(format, size) ?? user.GetDefaultAvatarUrl();
}
