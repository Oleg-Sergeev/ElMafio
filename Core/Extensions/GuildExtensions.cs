using Discord;
using Discord.WebSocket;

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



    public static async Task<string> GetMentionFromIdAsync(this IGuild guild, ulong id)
    {
        if (guild.GetRole(id) is IMentionable role)
            return role.Mention;

        if ((await guild.GetChannelAsync(id)) is IChannel channel)
            return $"<#{channel.Id}>";

        if (await guild.GetUserAsync(id) is IMentionable user)
            return user.Mention;

        return id.ToString();
    }

    public static string GetMentionFromId(this SocketGuild guild, ulong id)
    {
        if (guild.GetChannel(id) is IChannel channel)
            return $"<#{channel.Id}>";

        if (guild.GetRole(id) is IMentionable role)
            return role.Mention;

        if (guild.GetUser(id) is IMentionable user)
            return user.Mention;


        return id.ToString();
    }
}