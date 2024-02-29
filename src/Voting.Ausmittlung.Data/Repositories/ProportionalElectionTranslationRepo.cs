// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionTranslationRepo : TranslationRepo<ProportionalElectionTranslation>
{
    public ProportionalElectionTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.ProportionalElectionId);
}
