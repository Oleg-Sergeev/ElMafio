using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Infrastructure.Data.Entities.Games.Settings;
using Infrastructure.Data.Entities.Games.Settings.Mafia;

namespace Infrastructure.Data.Entities.ServerInfo;

public class Server
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }


    public string Prefix { get; set; }

    public ulong? LogChannelId { get; set; }

    public DebugMode DebugMode { get; set; }

    public BlockBehaviour BlockBehaviour { get; set; }

    public string BlockMessage { get; set; }

    public int SendInterval { get; set; }


    public MafiaSettings MafiaSettings { get; set; } = null!;

    public RussianRouletteSettings RussianRouletteSettings { get; set; } = null!;

    public List<ServerUser> ServerUsers { get; set; } = null!;

    public List<AccessLevel> AccessLevels { get; set; } = null!;



    public Server()
    {
        DebugMode = DebugMode.Off;

        BlockBehaviour = BlockBehaviour.SendToDM;

        BlockMessage = "Вам заблокирован доступ к командам. Пожалуйста, обратитесь к администраторам сервера для разблокировки";

        Prefix = "/";
    }
}