using Infrastructure.Data.Models.ServerInfo;

namespace Infrastructure.Data.Models;

public class ServerUser
{
    public ulong UserId { get; set; }

    public ulong ServerId { get; set; }


    public bool IsBlocked { get; set; }


    public User User { get; set; } = null!;

    public Server Server { get; set; } = null!;
}
