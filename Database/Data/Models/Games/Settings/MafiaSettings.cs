namespace Infrastructure.Data.Models.Games.Settings;

public class MafiaSettings : GameSettings
{
    public int MafiaKoefficient { get; set; }

    public bool IsRatingGame { get; set; }

    public bool RenameUsers { get; set; }

    public bool ReplyMessagesOnError { get; set; }

    public bool AbortGameWhenError { get; set; }

    public bool SendWelcomeMessage { get; set; }


    public ulong? CategoryChannelId { get; set; }

    public ulong? GeneralTextChannelId { get; set; }
    public ulong? MurdersTextChannelId { get; set; }

    public ulong? GeneralVoiceChannelId { get; set; }
    public ulong? MurdersVoiceChannelId { get; set; }


    public ulong? MafiaRoleId { get; set; }
    public ulong? WatcherRoleId { get; set; }
}
