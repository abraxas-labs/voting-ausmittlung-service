// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class MajorityElectionCandidateTranslationRepo : TranslationRepo<MajorityElectionCandidateTranslation>
{
    public MajorityElectionCandidateTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.MajorityElectionCandidateId);
}
