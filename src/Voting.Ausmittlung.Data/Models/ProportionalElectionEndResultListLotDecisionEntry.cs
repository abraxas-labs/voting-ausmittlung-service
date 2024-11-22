// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionEndResultListLotDecisionEntry : BaseEntity
{
    public Guid ProportionalElectionEndResultListLotDecisionId { get; set; }

    public ProportionalElectionEndResultListLotDecision ProportionalElectionEndResultListLotDecision { get; set; } = null!;

    public Guid? ListId { get; set; }

    public ProportionalElectionList? List { get; set; }

    public Guid? ListUnionId { get; set; }

    public ProportionalElectionListUnion? ListUnion { get; set; }

    public bool Winning { get; set; }
}
