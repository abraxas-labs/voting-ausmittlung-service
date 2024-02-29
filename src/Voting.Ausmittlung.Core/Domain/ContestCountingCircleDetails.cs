// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class ContestCountingCircleDetails
{
    public Guid ContestId { get; set; }

    public Guid CountingCircleId { get; set; }

    public CountOfVotersInformation CountOfVotersInformation { get; set; } = new();

    public List<VotingCardResultDetail> VotingCards { get; set; } = new();

    public bool EVoting { get; set; }

    public CountingMachine CountingMachine { get; set; }
}
