using Core.Common;
using Discord;
using Discord.Net;

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



    public static Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, MessageData data)
        => channel.SendMessageAsync(data.Message, data.IsTTS, data.Embed, data.RequestOptions, data.AllowedMentions, data.MessageReference, data.MessageComponent, data.Stickers, data.Embeds);


    public static Task<IUserMessage> SendEmbedAsync(this IMessageChannel channel, string description, string? title = null, EmbedBuilder? embedBuilder = null)
        => channel.SendEmbedAsync(description, EmbedStyle.Information, title, embedBuilder);

    public static async Task<IUserMessage> SendEmbedAsync(this IMessageChannel channel, string description, EmbedStyle embedStyle, string? title = null, EmbedBuilder? embedBuilder = null)
    {
        var embed = EmbedHelper.CreateEmbed(description, embedStyle, title, embedBuilder);

        return await channel.SendMessageAsync(embed: embed);
    }

    public static async Task<IUserMessage> SendEmbedStampAsync(this IMessageChannel channel, string description, EmbedStyle embedStyle, string? title = null,
         IUser? userAuthor = null, IUser? userFooter = null, EmbedBuilder? embedBuilder = null)
    {
        var embed = EmbedHelper.CreateEmbedStamp(description, embedStyle, title, userAuthor, userFooter, embedBuilder);

        return await channel.SendMessageAsync(embed: embed);
    }



    public static Task<IEnumerable<IUserMessage>> BroadcastMessagesAsync(this IMessageChannel sourceChannel, IEnumerable<IMessageChannel> channels, string? text = null, Embed? embed = null)
        => sourceChannel.BroadcastMessagesAsync(channels, new MessageData() { Message = text, Embed = embed });

    public static async Task<IEnumerable<IUserMessage>> BroadcastMessagesAsync(this IMessageChannel sourceChannel, IEnumerable<IMessageChannel> channels, MessageData data)
    {
        var tasks = new List<Task<IUserMessage>>();

        foreach (var channel in channels)
            tasks.Add(channel.SendMessageAsync(data.Message, data.IsTTS, data.Embed, data.RequestOptions, data.AllowedMentions, data.MessageReference, data.MessageComponent, data.Stickers, data.Embeds));

        var messages = new List<IUserMessage>();

        while (tasks.Count > 0)
        {
            var messageTask = await Task.WhenAny(tasks);

            tasks.Remove(messageTask);

            try
            {
                var message = await messageTask;

                messages.Add(message);
            }
            catch (HttpException e)
            {
                await sourceChannel.SendEmbedAsync($"Не удалось отправить сообщение в канал. Причина: {e.Reason}", EmbedStyle.Error);
            }
        }


        return messages;
    }

}
