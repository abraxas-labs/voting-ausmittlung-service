// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Data.Models;

public static class CountingCircleExtensions
{
    /// <summary>
    /// Snapshots a counting circle for a contest. Modifies the counting circle in place.
    /// </summary>
    /// <param name="cc">The counting circle to snapshot.</param>
    /// <param name="contestId">The contest ID for which the snapshot should be produced.</param>
    public static void SnapshotForContest(this CountingCircle cc, Guid contestId)
    {
        var basisCcId = cc.Id;
        var id = AusmittlungUuidV5.BuildCountingCircleSnapshot(contestId, basisCcId);

        // Modify the IDs. When saving this counting circle to the database, this will create new entries.
        cc.Id = id;
        cc.SnapshotContestId = contestId;

        cc.ResponsibleAuthority.Id = Guid.NewGuid();
        cc.ResponsibleAuthority.CountingCircleId = id;

        cc.ContactPersonDuringEvent.Id = Guid.NewGuid();
        cc.ContactPersonDuringEvent.CountingCircleDuringEventId = id;

        if (cc.ContactPersonAfterEvent != null)
        {
            cc.ContactPersonAfterEvent.Id = Guid.NewGuid();
            cc.ContactPersonAfterEvent.CountingCircleAfterEventId = id;
        }

        if (cc.Electorates == null)
        {
            return;
        }

        foreach (var electorate in cc.Electorates)
        {
            electorate.Id = AusmittlungUuidV5.BuildCountingCircleElectorateSnapshot(contestId, basisCcId, electorate.Id);
            electorate.CountingCircleId = id;
        }
    }
}
