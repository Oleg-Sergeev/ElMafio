namespace Infrastructure.Data.ViewModels;

public class MafiaSettingsViewModel
{
    public int? MafiaKoefficient { get; init; }

    public bool? IsRatingGame { get; init; }

    public bool? RenameUsers { get; init; }

    public bool? ReplyMessagesOnSetupError { get; init; }

    public bool? AbortGameWhenError { get; init; }

    public bool? SendWelcomeMessage { get; init; }
}