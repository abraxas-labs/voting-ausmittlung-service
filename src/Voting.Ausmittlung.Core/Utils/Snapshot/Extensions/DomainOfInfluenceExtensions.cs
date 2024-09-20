// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Data.Models;

public static class DomainOfInfluenceExtensions
{
    /// <summary>
    /// Snapshots a domain of influence for a contest. Modifies the domain of influence in place.
    /// </summary>
    /// <param name="doi">The domain of influence to snapshot.</param>
    /// <param name="contestId">The contest ID for which the snapshot should be produced.</param>
    public static void SnapshotForContest(this DomainOfInfluence doi, Guid contestId)
    {
        // Modify the IDs. When saving this domain of influence to the database, this will create new entries.
        doi.Id = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(contestId, doi.Id);
        doi.SnapshotContestId = contestId;

        foreach (var party in doi.Parties)
        {
            party.SnapshotForContest(contestId);
        }

        if (doi.PlausibilisationConfiguration == null)
        {
            return;
        }

        doi.PlausibilisationConfiguration.DomainOfInfluenceId = doi.Id;
        doi.PlausibilisationConfiguration.SnapshotForContest();
    }
}
