// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class SecondaryMajorityElectionCandidateTranslationRepo : TranslationRepo<SecondaryMajorityElectionCandidateTranslation>
{
    public SecondaryMajorityElectionCandidateTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.SecondaryMajorityElectionCandidateId);
}
