using Discord;

namespace Modules.Extensions
{
    public static class UserExtensions
    {
        public static string GetFullName(this IGuildUser guildUser) => $"{guildUser.Nickname ?? guildUser.Username}#{guildUser.Discriminator}";
        public static string GetFullName(this IUser user) => $"{user.Username}#{user.Discriminator}";

        public static string GetFullMention(this IUser user) =>
            $"{user.Mention}#{user.Discriminator}";
    }
}
