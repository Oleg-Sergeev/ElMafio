using Infrastructure.Data.Entities.ServerInfo;

namespace Infrastructure.Data.Entities;

public class ServerUser
{
    public ulong UserId { get; set; }

    public ulong ServerId { get; set; }

    public int? AccessLevelId { get; set; }


    public bool IsBlocked { get; set; }

    public StandartAccessLevel? StandartAccessLevel { get; set; }


    public User User { get; set; } = null!;

    public Server Server { get; set; } = null!;

    public AccessLevel? AccessLevel { get; set; }
}
