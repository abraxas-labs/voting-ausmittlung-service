// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Data.Models;

public static class DomainOfInfluencePartyExtensions
{
    /// <summary>
    /// Snapshots a party for a contest. Modifies the party in place.
    /// </summary>
    /// <param name="party">The party to snapshot.</param>
    /// <param name="contestId">The contest ID for which the snapshot should be produced.</param>
    public static void SnapshotForContest(this DomainOfInfluenceParty party, Guid contestId)
    {
        // Modify the IDs. When saving this party to the database, this will create new entries.
        party.Id = AusmittlungUuidV5.BuildDomainOfInfluenceParty(contestId, party.BaseDomainOfInfluencePartyId);
        party.SnapshotContestId = contestId;

        foreach (var translation in party.Translations)
        {
            translation.DomainOfInfluencePartyId = party.Id;
            translation.Id = Guid.NewGuid();
        }
    }
}
