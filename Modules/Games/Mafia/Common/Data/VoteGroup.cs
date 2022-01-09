using System.Collections.Generic;
using System.Linq;
using Discord;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Data;

public class VoteGroup
{
    public Vote Choice { get; }

    public IReadOnlyDictionary<IGuildUser, Vote> PlayersVote { get; }


    private readonly GameRole _votedRole;


    public VoteGroup(GameRole votedRole, IReadOnlyDictionary<IGuildUser, Vote> playersVote)
    {
        PlayersVote = playersVote;

        _votedRole = votedRole;

        Choice = CalculateChoice();
    }


    private Vote CalculateChoice()
    {
        var skipCount = 0;

        var votes = new Dictionary<IGuildUser, int>();

        foreach (var vote in PlayersVote.Values)
        {
            if (vote.IsSkip)
            {
                skipCount++;

                continue;
            }

            if (vote.Option is null)
                continue;

            votes[vote.Option] = votes.TryGetValue(vote.Option, out var count) ? count + 1 : 1;
        }

        Vote result;

        if (votes.Count == 0)
        {
            result = new Vote(_votedRole, null, skipCount > 0);
        }
        else if (votes.Count == 1)
        {
            var vote = votes.First();

            result = vote.Value > skipCount
                ? new Vote(_votedRole, vote.Key, false)
                : new Vote(_votedRole, null, skipCount > vote.Value);
        }
        else
        {
            var votesList = votes.ToList();

            votesList.Sort((v1, v2) => v2.Value - v1.Value);

            if (votesList[0].Value > votesList[1].Value && votesList[0].Value > skipCount)
                result = new Vote(_votedRole, votesList[0].Key, false);
            else
                result = new Vote(_votedRole, null, skipCount > votesList[0].Value);
        }

        return result;
    }
}
