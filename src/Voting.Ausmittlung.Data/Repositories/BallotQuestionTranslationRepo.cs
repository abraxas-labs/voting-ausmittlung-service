// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class BallotQuestionTranslationRepo : TranslationRepo<BallotQuestionTranslation>
{
    public BallotQuestionTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.BallotQuestionId);

    public async Task DeleteTranslationsByBallotId(Guid ballotId)
    {
        var idsToDelete = await Query()
            .Where(t => t.BallotQuestion!.BallotId == ballotId)
            .Select(t => t.Id)
            .ToListAsync();

        await DeleteRangeByKey(idsToDelete);
    }
}
