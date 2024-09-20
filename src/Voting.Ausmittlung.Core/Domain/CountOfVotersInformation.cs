// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class CountOfVotersInformation
{
    public int TotalCountOfVoters { get; set; }

    public List<CountOfVotersInformationSubTotal> SubTotalInfo { get; set; } = new();
}
