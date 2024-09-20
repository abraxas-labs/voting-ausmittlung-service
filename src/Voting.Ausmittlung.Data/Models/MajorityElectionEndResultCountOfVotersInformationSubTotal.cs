// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionEndResultCountOfVotersInformationSubTotal : EndResultCountOfVotersInformationSubTotal
{
    public Guid MajorityElectionEndResultId { get; set; }
}
