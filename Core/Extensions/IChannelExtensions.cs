using Discord;

namespace Core.Extensions;

public static class IChannelExtensions
{
    public static async Task ClearAsync(this ITextChannel channel, int messagesCount = 500)
    {
        messagesCount = Math.Clamp(messagesCount, 0, 1000);

        var messages = (await channel.GetMessagesAsync(messagesCount).FlattenAsync())
            .Where(msg => DateTime.Now - msg.Timestamp < TimeSpan.FromDays(14));

        await channel.DeleteMessagesAsync(messages);
    }
}
