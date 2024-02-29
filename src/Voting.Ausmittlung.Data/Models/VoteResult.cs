// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voting.Ausmittlung.Data.Models;

public class VoteResult : CountingCircleResult
{
    public Guid VoteId { get; set; }

    public Vote Vote { get; set; } = null!;

    public ICollection<BallotResult> Results { get; set; } = new HashSet<BallotResult>();

    [NotMapped]
    public override PoliticalBusiness PoliticalBusiness => Vote;

    [NotMapped]
    public override Guid PoliticalBusinessId
    {
        get => VoteId;
        set => VoteId = value;
    }

    public VoteResultEntry Entry { get; set; }

    public VoteResultEntryParams? EntryParams { get; set; }

    public void UpdateVoterParticipation()
    {
        foreach (var result in Results)
        {
            result.UpdateVoterParticipation(TotalCountOfVoters);
        }
    }

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters = false)
    {
        foreach (var result in Results)
        {
            result.ResetAllSubTotals(dataSource);

            if (includeCountOfVoters)
            {
                result.ResetCountOfVoters(dataSource, TotalCountOfVoters);
            }
        }
    }
}
