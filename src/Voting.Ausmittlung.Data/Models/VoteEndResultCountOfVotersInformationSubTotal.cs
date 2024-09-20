// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class VoteEndResultCountOfVotersInformationSubTotal : EndResultCountOfVotersInformationSubTotal
{
    public Guid VoteEndResultId { get; set; }
}
