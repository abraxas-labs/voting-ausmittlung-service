// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionCandidateResultBase : BaseEntity, IHasNullableConventionalVoteCounts
{
    public Guid CandidateId { get; set; }

    public abstract int CandidatePosition { get; }

    public abstract string CandidateNumber { get; }

    public int? ConventionalVoteCount { get; set; }

    public int EVotingExclWriteInsVoteCount { get; set; }

    public int ECountingExclWriteInsVoteCount { get; set; }

    public int EVotingWriteInsVoteCount { get; set; }

    public int ECountingWriteInsVoteCount { get; set; }

    public int EVotingInclWriteInsVoteCount => EVotingExclWriteInsVoteCount + EVotingWriteInsVoteCount;

    public int ECountingInclWriteInsVoteCount => ECountingExclWriteInsVoteCount + ECountingWriteInsVoteCount;

    public int VoteCount
    {
        get => ConventionalVoteCount.GetValueOrDefault() + ECountingInclWriteInsVoteCount + EVotingInclWriteInsVoteCount;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public void SetVoteCountExclWriteIns(VotingDataSource dataSource, int count)
    {
        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                EVotingExclWriteInsVoteCount = count;
                break;
            case VotingDataSource.ECounting:
                ECountingExclWriteInsVoteCount = count;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, "Unknown data source");
        }
    }

    public void AddVoteCountExclWriteIns(VotingDataSource dataSource, int deltaVoteCount)
    {
        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                EVotingExclWriteInsVoteCount += deltaVoteCount;
                break;
            case VotingDataSource.ECounting:
                ECountingExclWriteInsVoteCount += deltaVoteCount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, "Unknown data source");
        }
    }

    public void MoveECountingToConventional()
    {
        ConventionalVoteCount += ECountingInclWriteInsVoteCount;
        ECountingWriteInsVoteCount = 0;
        ECountingExclWriteInsVoteCount = 0;
    }
}
