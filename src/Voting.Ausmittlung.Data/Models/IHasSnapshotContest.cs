// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IHasSnapshotContest
{
    Contest? SnapshotContest { get; }
}
