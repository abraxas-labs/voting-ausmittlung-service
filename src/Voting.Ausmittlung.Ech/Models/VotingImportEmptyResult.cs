// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImportEmptyResult : VotingImportPoliticalBusinessResult
{
    // An imported eCH vote result may contain ballot results from different VOTING vote results,
    // because VOTING votes of the same domain of influence get grouped into the same eCH vote.
    // The voteId may not be correct for all ballot results!
    public VotingImportEmptyResult(string basisCountingCircleId)
        : base(Guid.Empty, basisCountingCircleId)
    {
        PoliticalBusinessType = PoliticalBusinessType.Unspecified;
    }
}
