// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CantonSettingsVotingCardChannel : BaseEntity
{
    public VotingChannel VotingChannel { get; set; }

    public bool Valid { get; set; }

    public CantonSettings CantonSettings { get; set; } = null!;

    public Guid CantonSettingsId { get; set; }
}
