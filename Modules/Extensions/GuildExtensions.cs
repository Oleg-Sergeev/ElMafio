using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Modules.Extensions;

public static class GuildExtensions
{
    public static async Task<ITextChannel> GetTextChannelOrCreateAsync(
        this IGuild guild,
        ulong? channelId,
        string channelName,
        int messagesCountToDelete = 500,
        Action<TextChannelProperties>? textProps = null)
    {
        var textChannel = await guild.GetTextChannelAsync(channelId ?? 0);


        if (textChannel is not null && messagesCountToDelete > 0)
        {
            var messages = await textChannel.GetMessagesAsync(messagesCountToDelete).FlattenAsync();

            if (messages.Any())
                await textChannel.DeleteMessagesAsync(messages);
        }
        else
            textChannel = await guild.CreateTextChannelAsync(channelName, textProps);


        return textChannel;
    }

    public static async Task<IVoiceChannel> GetVoiceChannelOrCreateAsync(
       this IGuild guild,
       ulong? channelId,
       string channelName,
       Action<VoiceChannelProperties>? voiceProps = null)
    {
        var channel = await guild.GetVoiceChannelAsync(channelId ?? 0) ?? await guild.CreateVoiceChannelAsync(channelName, voiceProps);

        return channel;
    }


    public static async Task<IRole> GetRoleOrCreateAsync(
        this IGuild guild,
        ulong? roleId,
        string roleName,
        GuildPermissions? guildPermissions = null,
        Color? color = null,
        bool isHoisted = false,
        bool isMentionable = false)
    {
        var role = guild.GetRole(roleId ?? 0);

        if (role is null)
            role = await guild.CreateRoleAsync(roleName, guildPermissions, color, isHoisted, isMentionable);

        return role;
    }


    public static async Task<ICategoryChannel> CreateCategoryChannelOrCreateAsync(
        this IGuild guild,
        ulong? categoryId,
        string categoryName,
        Action<GuildChannelProperties>? props = null)
    {
        var categories = await guild.GetCategoriesAsync();

        ICategoryChannel? categoryChannel = null;

        if (categoryId is not null)
            categoryChannel = categories.FirstOrDefault(c => c.Id == categoryId);

        if (categoryChannel is null)
            categoryChannel = await guild.CreateCategoryAsync(categoryName, props);


        return categoryChannel;
    }
}