// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class SimplePoliticalBusinessTranslationRepo : TranslationRepo<SimplePoliticalBusinessTranslation>
{
    public SimplePoliticalBusinessTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.SimplePoliticalBusinessId);
}
