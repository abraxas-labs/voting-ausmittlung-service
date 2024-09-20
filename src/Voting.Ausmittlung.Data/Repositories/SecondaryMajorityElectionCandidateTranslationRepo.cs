// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class SecondaryMajorityElectionCandidateTranslationRepo : TranslationRepo<SecondaryMajorityElectionCandidateTranslation>
{
    public SecondaryMajorityElectionCandidateTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.SecondaryMajorityElectionCandidateId);

    public async Task DeleteCandidateReferenceTranslations(Guid candidateReferenceId)
    {
        var idsToDelete = await Query()
            .Where(t => t.SecondaryMajorityElectionCandidate!.CandidateReferenceId == candidateReferenceId)
            .Select(t => t.Id)
            .ToListAsync();

        await DeleteRangeByKey(idsToDelete);
    }
}
