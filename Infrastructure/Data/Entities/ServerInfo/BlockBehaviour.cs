using System;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Data.Entities.ServerInfo;

[Flags]
public enum BlockBehaviour
{
    [Display(Name = "Не отправлять сообщения")]
    DoNotSend = 0,

    [Display(Name = "Отправлять в ЛС")]
    SendToDM = 1 << 0,

    [Display(Name = "Отправлять на Сервер")]
    SendToServer = 1 << 1,

    [Display(Name = "Отправлять везде")]
    SendEverywhere = SendToDM | SendToServer
}
