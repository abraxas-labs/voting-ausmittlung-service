// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionCandidateTranslationRepo : TranslationRepo<ProportionalElectionCandidateTranslation>
{
    public ProportionalElectionCandidateTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.ProportionalElectionCandidateId);
}
