using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Data;

public class Vote
{
    public GameRole VotedRole { get; }

    public IGuildUser? Option { get; }

    public bool IsSkip { get; }


    public Vote(GameRole votedRole, IGuildUser? votedPlayer, bool isSkip)
    {
        VotedRole = votedRole;
        Option = votedPlayer;
        IsSkip = isSkip;
    }
}
