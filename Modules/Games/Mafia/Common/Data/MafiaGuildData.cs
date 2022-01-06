using System.Collections.Generic;
using Discord;

namespace Modules.Games.Mafia.Common.Data;

public class MafiaGuildData
{
    public Dictionary<ulong, List<ulong>> PlayerRoleIds { get; }

    public List<ulong> OverwrittenNicknames { get; }

    public List<IGuildUser> KilledPlayers { get; }


    public ITextChannel GeneralTextChannel { get; }
    public ITextChannel MurderTextChannel { get; }
    public ITextChannel? SpectatorTextChannel { get; }

    public IVoiceChannel? GeneralVoiceChannel { get; }
    public IVoiceChannel? MurderVoiceChannel { get; }
    public IVoiceChannel? SpectatorVoiceChannel { get; }

    public IRole MafiaRole { get; }
    public IRole? SpectatorRole { get; }

    public MafiaGuildData(ITextChannel generalTextChannel, ITextChannel murderTextChannel,
                          ITextChannel? spectatorTextChannel, IVoiceChannel? generalVoiceChannel,
                          IVoiceChannel? murderVoiceChannel, IVoiceChannel? spectatorVoiceChannel,
                          IRole mafiaRole, IRole? spectatorRole)
    {
        PlayerRoleIds = new();
        OverwrittenNicknames = new();
        KilledPlayers = new();


        GeneralTextChannel = generalTextChannel;
        MurderTextChannel = murderTextChannel;
        SpectatorTextChannel = spectatorTextChannel;

        GeneralVoiceChannel = generalVoiceChannel;
        MurderVoiceChannel = murderVoiceChannel;
        SpectatorVoiceChannel = spectatorVoiceChannel;


        MafiaRole = mafiaRole;
        SpectatorRole = spectatorRole;
    }
}
