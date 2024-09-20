// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class TieBreakQuestionTranslationRepo : TranslationRepo<TieBreakQuestionTranslation>
{
    public TieBreakQuestionTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.TieBreakQuestionId);

    public async Task DeleteTranslationsByBallotId(Guid ballotId)
    {
        var idsToDelete = await Query()
            .Where(t => t.TieBreakQuestion!.BallotId == ballotId)
            .Select(t => t.Id)
            .ToListAsync();

        await DeleteRangeByKey(idsToDelete);
    }
}
