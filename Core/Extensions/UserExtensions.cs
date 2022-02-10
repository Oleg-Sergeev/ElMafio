using Discord;

namespace Core.Extensions;

public static class UserExtensions
{
    public static string GetFullName(this IGuildUser guildUser) => $"{guildUser.Nickname ?? guildUser.Username} #{guildUser.Discriminator}";
    public static string GetFullName(this IUser user) => $"{user.Username} #{user.Discriminator}";

    public static string GetFullMention(this IUser user) =>
        $"{user.Mention} #{user.Discriminator}";

    public static string GetAvatarUrlOrDefaultAvatarUrl(this IUser user, ImageFormat format = ImageFormat.Auto, ushort size = 128)
        => user.GetAvatarUrl(format, size) ?? user.GetDefaultAvatarUrl();


    public static bool HasGuildPermission(this IUser user, GuildPermission guildPermission)
        => user is IGuildUser guildUser && guildUser.HasGuildPermission(guildPermission);

    public static bool HasGuildPermission(this IGuildUser guildUser, GuildPermission guildPermission)
        => guildUser.GuildPermissions.Has(guildPermission);


    public static int? CompareHierarchy(this IUser user1, IUser user2)
        => user1 is IGuildUser guildUser1 && user2 is IGuildUser guildUser2
        ? guildUser1.CompareHierarchy(guildUser2)
        : null;

    public static int CompareHierarchy(this IGuildUser guildUser1, IGuildUser guildUser2)
        => guildUser1.Hierarchy.CompareTo(guildUser2.Hierarchy);
}