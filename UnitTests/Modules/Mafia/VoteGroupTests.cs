using System.Collections.Generic;
using Discord;
using Modules.Games.Mafia.Common.Data;
using Xunit;

namespace UnitTests.Modules.Mafia;


public class VoteGroupTests
{
    [Theory(DisplayName = "Should return null Choice and false Skip")]
    [MemberData(nameof(GetData_NotDefinedChoice))]
    public void NotDefinedChoice(IReadOnlyDictionary<IGuildUser, Vote> votes, bool isUnanimousVote = false)
    {
        var gameRole = TestsHelper.GetMockedGameRole();

        var voteGroup = new VoteGroup(gameRole, votes, isUnanimousVote);


        Assert.Null(voteGroup.Choice.Option);
        Assert.False(voteGroup.Choice.IsSkip);
    }

    [Theory(DisplayName = "Should return null Choice and true Skip")]
    [MemberData(nameof(GetData_SkippedChoice))]
    public void SkippedChoice(IReadOnlyDictionary<IGuildUser, Vote> votes, bool isUnanimousVote = false)
    {
        var gameRole = TestsHelper.GetMockedGameRole();

        var voteGroup = new VoteGroup(gameRole, votes, isUnanimousVote);


        Assert.Null(voteGroup.Choice.Option);
        Assert.True(voteGroup.Choice.IsSkip);
    }


    [Theory(DisplayName = "Should return expected Choice and false Skip")]
    [MemberData(nameof(GetData_ExpectedChoice))]
    public void ExpectedChoice(IGuildUser expected, IReadOnlyDictionary<IGuildUser, Vote> votes, bool isUnanimousVote = false)
    {
        var gameRole = TestsHelper.GetMockedGameRole();

        var voteGroup = new VoteGroup(gameRole, votes, isUnanimousVote);


        Assert.Equal(expected, voteGroup.Choice.Option);
        Assert.False(voteGroup.Choice.IsSkip);
    }



    public static IEnumerable<object[]> GetData_NotDefinedChoice()
    {
        var gameRole = TestsHelper.GetMockedGameRole();

        var players = TestsHelper.GetPlayers(5);


        return new List<object[]>
        {
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {

                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, false) },
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[1], false) },
                    { players[1], new(gameRole, players[2], false) },
                    { players[2], new(gameRole, players[0], false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, true) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, players[2], false) },
                    { players[3], new(gameRole, players[2], false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[1], false) },
                    { players[1], new(gameRole, null, false) },
                    { players[2], new(gameRole, players[0], false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[1], false) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, players[0], false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[1], false) },
                    { players[1], new(gameRole, null, false) },
                    { players[2], new(gameRole, null, false) },
                    { players[3], new(gameRole, null, false) },
                    { players[4], new(gameRole, players[2], false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[0], false) },
                    { players[2], new(gameRole, players[2], false) },
                    { players[3], new(gameRole, players[2], false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[2], false) }
                },
                true
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[2], false) },
                    { players[2], new(gameRole, players[2], false) },
                    { players[3], new(gameRole, players[2], false) },
                },
                true
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, true) },
                    { players[1], new(gameRole, players[2], false) },
                    { players[2], new(gameRole, players[2], false) },
                    { players[3], new(gameRole, players[2], false) },
                },
                true
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, false) },
                    { players[1], new(gameRole, null, false) },
                    { players[2], new(gameRole, null, false) },
                    { players[3], new(gameRole, null, false) },
                    { players[4], new(gameRole, null, false) }
                },
                true
            }
        };
    }

    public static IEnumerable<object[]> GetData_SkippedChoice()
    {
        var gameRole = TestsHelper.GetMockedGameRole();

        var players = TestsHelper.GetPlayers(5);


        return new List<object[]>
        {
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, true) },
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[1], false) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, null, true) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, true) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, null, true) },
                    { players[3], new(gameRole, null, false) },
                    { players[4], new(gameRole, null, false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, null, true) },
                    { players[3], new(gameRole, null, false) },
                    { players[4], new(gameRole, null, false) }
                }
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, true) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, null, true) }
                },
                true
            },
            new object[]
            {
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, null, false) },
                    { players[1], new(gameRole, null, true) },
                    { players[2], new(gameRole, null, true) }
                },
                true
            }
        };
    }

    public static IEnumerable<object[]> GetData_ExpectedChoice()
    {
        var gameRole = TestsHelper.GetMockedGameRole();

        var players = TestsHelper.GetPlayers(5);


        return new List<object[]>
        {
            new object[]
            {
                players[0],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) }
                }
            },
            new object[]
            {
                players[1],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[1], false) },
                    { players[2], new(gameRole, players[1], false) }
                }
            },
            new object[]
            {
                players[3],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[1], false) },
                    { players[2], new(gameRole, players[2], false) },
                    { players[3], new(gameRole, players[3], false) },
                    { players[4], new(gameRole, players[3], false) }
                }
            },
            new object[]
            {
                players[0],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, null, false) },
                    { players[2], new(gameRole, null, false) },
                    { players[3], new(gameRole, null, false) },
                    { players[4], new(gameRole, null, false) }
                }
            },
            new object[]
            {
                players[0],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[0], false) },
                    { players[2], new(gameRole, players[0], false) },
                    { players[3], new(gameRole, null, true) },
                    { players[4], new(gameRole, null, true) }
                }
            },
            new object[]
            {
                players[0],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[0], false) },
                    { players[2], new(gameRole, null, false) },
                    { players[3], new(gameRole, null, false) },
                    { players[4], new(gameRole, null, true) }
                }
            },
            new object[]
            {
                players[0],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[0], false) }
                },
                true
            },
            new object[]
            {
                players[0],
                new Dictionary<IGuildUser, Vote>
                {
                    { players[0], new(gameRole, players[0], false) },
                    { players[1], new(gameRole, players[0], false) },
                    { players[2], new(gameRole, players[0], false) },
                    { players[3], new(gameRole, null, false) }
                },
                true
            },
        };
    }
}