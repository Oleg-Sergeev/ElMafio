using System.Collections.Generic;
using System.Linq;
using Discord;
using Modules.Games.Mafia.Common.GameRoles;

namespace Modules.Games.Mafia.Common.Data;

public class VoteGroup
{
    public Vote Choice { get; }

    public IReadOnlyDictionary<IGuildUser, Vote> PlayersVote { get; }


    public VoteGroup(GameRole votedRole, IReadOnlyDictionary<IGuildUser, Vote> playersVote, bool isUnanimousVote = false)
    {
        PlayersVote = playersVote;

        Choice = CalculateChoice(votedRole, isUnanimousVote);
    }


    private Vote CalculateChoice(GameRole votedRole, bool isUnanimousVote)
    {
        if (isUnanimousVote)
        {
            var activeVotes = PlayersVote.Values.Where(v => v.IsSkip || v.Option is not null);

            if (!activeVotes.Any())
                return new Vote(votedRole, null, false);


            var allSkipped = activeVotes.All(v => v.IsSkip);

            if (allSkipped)
                return new Vote(votedRole, null, true);


            var killedPlayer = activeVotes.FirstOrDefault()?.Option;

            var allVotedForOne = activeVotes.All(v => v.Option == killedPlayer);

            if (allVotedForOne)
                return new Vote(votedRole, killedPlayer, false);
            else
                return new Vote(votedRole, null, false);
        }

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
            result = new Vote(votedRole, null, skipCount > 0);
        }
        else if (votes.Count == 1)
        {
            var vote = votes.First();

            result = vote.Value > skipCount
                ? new Vote(votedRole, vote.Key, false)
                : new Vote(votedRole, null, skipCount > vote.Value);
        }
        else
        {
            var votesList = votes.ToList();

            votesList.Sort((v1, v2) => v2.Value - v1.Value);

            if (votesList[0].Value > votesList[1].Value && votesList[0].Value > skipCount)
                result = new Vote(votedRole, votesList[0].Key, false);
            else
                result = new Vote(votedRole, null, skipCount > votesList[0].Value);
        }

        return result;
    }
}
