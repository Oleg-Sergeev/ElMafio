using Discord;

namespace Core.Extensions;

public static class GuildExtensions
{
    public static async Task<ITextChannel> GetTextChannelOrCreateAsync(
        this IGuild guild,
        ulong? channelId,
        string channelName,
        Action<TextChannelProperties>? textProps = null,
        CacheMode mode = CacheMode.AllowDownload,
        RequestOptions? options = null)
    {
        var textChannel = await guild.GetTextChannelAsync(channelId ?? 0, mode, options) ?? await guild.CreateTextChannelAsync(channelName, textProps, options);

        return textChannel;
    }

    public static async Task<IVoiceChannel> GetVoiceChannelOrCreateAsync(
       this IGuild guild,
       ulong? channelId,
       string channelName,
       Action<VoiceChannelProperties>? voiceProps = null,
       CacheMode mode = CacheMode.AllowDownload,
       RequestOptions? options = null)
    {
        var channel = await guild.GetVoiceChannelAsync(channelId ?? 0, mode, options) ?? await guild.CreateVoiceChannelAsync(channelName, voiceProps, options);

        return channel;
    }


    public static async Task<IRole> GetRoleOrCreateAsync(
        this IGuild guild,
        ulong? roleId,
        string roleName,
        GuildPermissions? guildPermissions = null,
        Color? color = null,
        bool isHoisted = false,
        bool isMentionable = false,
        RequestOptions? options = null)
    {
        var role = guild.GetRole(roleId ?? 0) ?? await guild.CreateRoleAsync(roleName, guildPermissions, color, isHoisted, isMentionable, options);

        return role;
    }


    public static async Task<ICategoryChannel> GetCategoryChannelOrCreateAsync(
        this IGuild guild,
        ulong? categoryId,
        string categoryName,
        Action<GuildChannelProperties>? props = null,
        CacheMode mode = CacheMode.AllowDownload,
        RequestOptions? options = null)
    {
        var channel = await guild.GetChannelAsync(categoryId ?? 0, mode, options);

        if (channel is ICategoryChannel categoryChannel)
            return categoryChannel;
        else
            return await guild.CreateCategoryAsync(categoryName, props, options);
    }
}