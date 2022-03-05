using System;

namespace Infrastructure.Data.Models.ServerInfo;

[Flags]
public enum BlockBehaviour
{
    Silent = 0,
    SendToDM = 1 << 0,
    SendToServer = 1 << 1,
    SendBoth = SendToDM | SendToServer
}
