// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Ech.Models;

public record EVotingCountingCircleResultCountOfVotersInformation(
    string BasisCountingCircleId,
    Guid PoliticalBusinessId,
    int CountOfVotersTotal);
